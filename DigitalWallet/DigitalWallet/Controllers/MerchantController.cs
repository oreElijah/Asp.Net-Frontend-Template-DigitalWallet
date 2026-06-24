using DigitalWalletApi.Extensions;
using DigitalWalletApi.Filter;
using DigitalWalletCore.Dtos.Merchant;
using DigitalWalletCore.Dtos.User;
using DigitalWalletCore.Entities;
using DigitalWalletCore.Interfaces;
using DigitalWalletInfrastructure.Data;
using DigitalWalletInfrastructure.Mapper;
using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalWalletApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]/")]

    public class MerchantController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paystackService;
        private readonly IAuthService _authService;
        private readonly IWalletService _walletService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<MerchantController> _logger;

        public MerchantController(UserManager<AppUser> userManager, ApplicationDbContext context, IAuthService authService, IWalletService walletService, IEmailService emailService, IWebHostEnvironment env, ILogger<MerchantController> logger, IPaymentService paystackService)
        {
            _userManager = userManager;
            _context = context; 
            _authService = authService;
            _walletService = walletService;
            _emailService = emailService;
            _paystackService = paystackService;
            _logger = logger;
            _env = env;
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterMerchant([FromBody] RegisterMerchantRequestDto registerRequestDto)
        {
            _logger.LogInformation("Received merchant registration request for email: {Email}", registerRequestDto.Email);
            if (string.IsNullOrWhiteSpace(registerRequestDto.Email) || string.IsNullOrWhiteSpace(registerRequestDto.Password))
            {
                _logger.LogWarning("Merchant registration request for email {Email} is missing required fields.", registerRequestDto.Email);
                return BadRequest("Email and password are required.");
            }

            _logger.LogInformation("Attempting to retrieve school with code: {SchoolCode} for merchant registration.", registerRequestDto.SchoolCode);
            var school = await _authService.GetSchoolByCodeAsync(registerRequestDto.SchoolCode);

            _logger.LogInformation("Resolving bank account for account number: {AccountNumber} and bank code: {BankCode} for merchant registration.", registerRequestDto.AccountNumber, registerRequestDto.BankCode);
            var accountName = await _paystackService.ResolveAccountAsync(registerRequestDto.AccountNumber, registerRequestDto.BankCode);

            if (accountName == null)
            {
                _logger.LogWarning("Failed to resolve bank account for account number: {AccountNumber} and bank code: {BankCode} for user ", registerRequestDto.AccountNumber, registerRequestDto.BankCode);
                return BadRequest("Invalid bank account details");
            }

            _logger.LogInformation("Creating transfer recipient for account name: {AccountName}, account number: {AccountNumber}, and bank code: {BankCode} for merchant registration.", accountName, registerRequestDto.AccountNumber, registerRequestDto.BankCode);
            var recipientCode = await _paystackService.CreateTransferRecipientAsync(accountName, registerRequestDto.AccountNumber, registerRequestDto.BankCode);

            _logger.LogInformation("Retrieving bank name for bank code: {BankCode} for merchant registration.", registerRequestDto.BankCode);
            var bankName = await _paystackService.GetBankNameByCodeAsync(registerRequestDto.BankCode);

            _logger.LogInformation("Creating merchant user object for email: {Email}", registerRequestDto.Email);
            var user = new AppUser
            {
                FirstName = registerRequestDto.BusinessName,
                LastName = registerRequestDto.BusinessName,
                Email = registerRequestDto.Email,
                SchoolCode = registerRequestDto.SchoolCode,
                School = school,
                UserName = $"{registerRequestDto.Email}",
                Merchant = new Merchant
                {
                    UserId = Guid.NewGuid().ToString(), // This will be set correctly after the user is created
                    AccountName = accountName,
                    AccountNumber = registerRequestDto.AccountNumber,
                    BankCode = registerRequestDto.BankCode,
                    BusinessName = registerRequestDto.BusinessName,
                    BankName = bankName,
                    IsApproved = false, // Assuming new merchants are not approved by default
                    ShopLocation = registerRequestDto.ShopLocation,
                    TransferRecipientCode = recipientCode
                }
            };

            _logger.LogInformation("Creating merchant user in the database for email: {Email}", registerRequestDto.Email);
            var newUser = await _userManager.CreateAsync(user, registerRequestDto.Password);
            if (!newUser.Succeeded)
            {
                _logger.LogWarning("Failed to create merchant user in the database for email: {Email}. Errors: {Errors}", registerRequestDto.Email, string.Join(", ", newUser.Errors.Select(e => e.Description)));
                return BadRequest(newUser.Errors);
            }

            _logger.LogInformation("Merchant user created successfully for email: {Email}. Assigning role 'Merchant'.", registerRequestDto.Email);
            var roleResult = await _userManager.AddToRoleAsync(user, "Merchant");
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("Failed to assign role 'Merchant' to user with email: {Email}. Errors: {Errors}", registerRequestDto.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                return BadRequest(roleResult.Errors);
            }

            _logger.LogInformation("Setting the UserId of the Merchant to the newly created user's Id for email: {Email}", registerRequestDto.Email);
            user.Merchant.UserId = user.Id; // Setting the UserId of the Merchant to the newly created user's Id

            _logger.LogInformation("Creating wallet for merchant user with email: {Email}", registerRequestDto.Email);
            var wallet = await _walletService.CreateMerchantWallet(user.Id);

            _logger.LogInformation("Assigning wallet to merchant user with email: {Email}", registerRequestDto.Email);
            user.Wallet = wallet.Data;

            _logger.LogInformation("Generating email confirmation token for merchant user with email: {Email}", registerRequestDto.Email);
            var verifyToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            _logger.LogInformation("Enqueuing background job to send verification email to merchant user with email: {Email}", registerRequestDto.Email);
            BackgroundJob.Enqueue<IEmailService>(x =>
               x.SendVerifyUserEmail(
                   user.FirstName,
                   user.Email,
                     verifyToken,
                   wallet.Data.WalletNumber));

            _logger.LogInformation("Merchant registration process completed successfully for email: {Email}", registerRequestDto.Email);
            var responseDto = registerRequestDto.ToMerchantRegisterResponseDto(user.Merchant.Id, wallet.Data.WalletNumber, accountName, bankName, "User registered successfully, Check your mail to activate your account and get your Wallet Number.");
            return Ok(responseDto);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpGet("merchant/profile")]
        [Authorize(Roles = "Merchant")]
        public async Task<IActionResult> GetMerchantProfile()
        {
            _logger.LogInformation("Received request to get merchant profile for user with ID: {UserId}", User.GetUserId());
            var userId = User.GetUserId();

            var user = await _context.Users
                .Include(x => x.Merchant)
                .Include(x => x.Wallet)
                .Include(x => x.School)
                .FirstOrDefaultAsync(x => x.Id == userId);

            _logger.LogInformation("Attempting to retrieve merchant profile for user with ID: {UserId}", User.GetUserId());
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", User.GetUserId());
                return NotFound("User not found");
            }

            _logger.LogInformation("Successfully retrieved merchant profile for user with ID: {UserId}", User.GetUserId());
            var profileDto = user.ToMerchantProfileResponseDto(user.Merchant.Id);
            _logger.LogInformation("Returning merchant profile for user: {User}", profileDto);
            return Ok(profileDto);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPut("update_merchant_profile")]
        [Authorize(Roles = "Merchant")]
        public async Task<IActionResult> UpdateMerchantProfile([FromBody] UpdateMerchantProfileDto updateMerchantProfileDto)
        {
            var userId = User.GetUserId();
            _logger.LogInformation("Received request to update merchant profile for user with ID: {UserId}", userId);

            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Update merchant profile request failed: User ID is missing.");
                return BadRequest("User ID is missing.");
            }

            _logger.LogInformation("Attempting to find user by ID: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return NotFound("User not found");
            }

            _logger.LogInformation("Successfully retrieved user for ID: {UserId}", userId);

            var accountName = await _paystackService.ResolveAccountAsync(updateMerchantProfileDto.AccountNumber, updateMerchantProfileDto.BankCode);
            if (accountName == null)
            {
                _logger.LogWarning("Failed to resolve bank account for account number: {AccountNumber} and bank code: {BankCode} for user with ID: {UserId}", updateMerchantProfileDto.AccountNumber, updateMerchantProfileDto.BankCode, userId);
                return BadRequest("Invalid bank account details");
            }

            _logger.LogInformation("Updating merchant profile for user with ID: {UserId}", userId);
            user.FirstName = updateMerchantProfileDto.Firstname ?? user.FirstName;
            user.LastName = updateMerchantProfileDto.Lastname ?? user.LastName;
            user.Merchant.AccountNumber = updateMerchantProfileDto.AccountNumber ?? user.Merchant.AccountNumber;
            user.Merchant.BankCode = updateMerchantProfileDto.BankCode ?? user.Merchant.BankCode;
            user.Merchant.ShopLocation = updateMerchantProfileDto.ShopLocation ?? user.Merchant.ShopLocation;

            _logger.LogInformation("Attempting to update merchant profile for user with ID: {UserId}", userId);
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to update merchant profile for user with ID: {UserId}", userId);
                return BadRequest(result.Errors);
            }

            _logger.LogInformation("Merchant profile updated successfully for user with ID: {UserId}", userId);
            var profileDto = user.ToMerchantProfileResponseDto(user.Merchant.Id);
            return Ok(profileDto);
        }
    }
}
