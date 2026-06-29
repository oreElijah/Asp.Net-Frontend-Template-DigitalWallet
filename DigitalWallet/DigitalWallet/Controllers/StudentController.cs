using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWalletApi.Controllers
{
    using DigitalWalletCore.Dtos.Merchant;
    using DigitalWalletCore.Dtos.User;
    using DigitalWalletCore.Entities;
    using DigitalWalletCore.Interfaces;
    using DigitalWalletInfrastructure.Data;
    using DigitalWalletInfrastructure.Mapper;
    using global::DigitalWalletApi.Extensions;
    using global::DigitalWalletApi.Filter;
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

        public class StudentController : ControllerBase
        {
            private readonly UserManager<AppUser> _userManager;
            private readonly IPaymentService _paystackService;
            private readonly IAuthService _authService;
            private readonly IWalletService _walletService;
            private readonly ApplicationDbContext _context;
            private readonly IEmailService _emailService;
            private readonly IFileStorageService _fileStorageService;
            private readonly IWebHostEnvironment _env;
            private readonly ILogger<StudentController> _logger;

            public StudentController(UserManager<AppUser> userManager, IAuthService authService, IWalletService walletService, IEmailService emailService, IFileStorageService fileStorageService, IWebHostEnvironment env, ILogger<StudentController> logger, IPaymentService paystackService, ApplicationDbContext context)
            {
                _userManager = userManager;
                _authService = authService;
                _walletService = walletService;
                _emailService = emailService;
                _context = context;
                _paystackService = paystackService;
                _fileStorageService = fileStorageService;
                _logger = logger;
                _env = env;
            }


            [ServiceFilter(typeof(LogActionFilter))]
            [HttpPost("register")]
            [AllowAnonymous]
            public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerDto)
            {
                _logger.LogInformation("Received registration request for email: {Email}", registerDto.Email);
                if (string.IsNullOrWhiteSpace(registerDto.Email) || string.IsNullOrWhiteSpace(registerDto.Password))
                {
                    _logger.LogWarning("Registration request for email {Email} is missing required fields.", registerDto.Email);
                    return BadRequest("Email and password are required.");
                }

                var profilePictureUrl = "";
                if (registerDto.ProfilePicture != null)
                {
                    _logger.LogInformation("Uploading profile picture for student registration for email: {Email}", registerDto.Email);
                    profilePictureUrl = await _fileStorageService.UploadFileAsync(registerDto.ProfilePicture);
                }

                _logger.LogInformation("Attempting to retrieve school with code: {SchoolCode} for registration.", registerDto.SchoolCode);
                var school = await _authService.GetSchoolByCodeAsync(registerDto.SchoolCode);

                _logger.LogInformation("Creating user object for email: {Email}", registerDto.Email);
                var user = new AppUser
                {
                    MatricNumber = registerDto.MatricNumber,
                    SchoolCode = registerDto.SchoolCode,
                    Email = registerDto.Email,
                    FirstName = registerDto.Firstname,
                    LastName = registerDto.Lastname,
                    UserName = registerDto.Email,
                    ProfilePicture = profilePictureUrl,
                    School = school
                };

                var newUser = await _userManager.CreateAsync(user, registerDto.Password);
                if (!newUser.Succeeded)
                {
                    _logger.LogWarning("Failed to create user for email {Email}. Errors: {Errors}", registerDto.Email, string.Join(", ", newUser.Errors.Select(e => e.Description)));
                    return BadRequest(newUser.Errors);
                }

                _logger.LogInformation("User created successfully for email: {Email}. Assigning role 'Student'.", registerDto.Email);
                var roleResult = await _userManager.AddToRoleAsync(user, "Student");
                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Failed to assign role 'Student' to user with email {Email}. Errors: {Errors}", registerDto.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    return BadRequest(roleResult.Errors);
                }

                _logger.LogInformation("Generating email confirmation token for user with email: {Email}", registerDto.Email);
                var verifyToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                _logger.LogInformation("Creating wallet for user with matric number: {MatricNumber}", user.MatricNumber);
                var wallet = await _walletService.CreateStudentWallet(user.MatricNumber, user.Id);

                user.Wallet = wallet.Data;

                _logger.LogInformation("Enqueuing background job to send verification email to user with email: {Email}", registerDto.Email);
                BackgroundJob.Enqueue<IEmailService>(x =>
                   x.SendVerifyUserEmail(
                       user.FirstName,
                       user.Email,
                         verifyToken,
                       wallet.Data.WalletNumber));

                _logger.LogInformation("Registration process completed successfully for email: {Email}", registerDto.Email);
                var responseDto = registerDto.ToRegisterResponseDto("User registered successfully, Check your mail to activate your account and get your Wallet Number.", user.Id.ToString(), profilePictureUrl);
                return Ok(responseDto);
            }

            [ServiceFilter(typeof(LogActionFilter))]
            [HttpGet("profile")]
            [Authorize(Roles = "Student")]
            public async Task<IActionResult> GetStudentProfile()
            {
                _logger.LogInformation("Received request to get student profile for user with ID: {UserId}", User.GetUserId());
                var userId = User.GetUserId();

                var user = await _context.Users
                    .Include(x => x.Merchant)
                    .Include(x => x.Wallet)
                    .Include(x => x.School)
                    .FirstOrDefaultAsync(x => x.Id == userId);

                _logger.LogInformation("Attempting to retrieve student profile for user with ID: {UserId}", User.GetUserId());
                if (user == null)
                {
                    _logger.LogWarning("User not found for ID: {UserId}", User.GetUserId());
                    return NotFound("User not found");
                }

                _logger.LogInformation("Successfully retrieved student profile for user with ID: {UserId}", User.GetUserId());
                var profileDto = user.ToUserProfileResponseDto();
                return Ok(profileDto);
            }

            [ServiceFilter(typeof(LogActionFilter))]
            [HttpPut("update_profile")]
            [Authorize]
            public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto)
            {
                var userId = User.GetUserId();
                _logger.LogInformation("Received request to update profile for user with ID: {UserId}", userId);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("Update profile request failed: User ID is missing.");
                    return BadRequest("User ID is missing.");
                }

                var profilePictureUrl = "";
                if (updateProfileDto.ProfilePicture != null)
                {
                    _logger.LogInformation("Uploading profile picture for student profile update for user with ID: {UserId}", userId);
                    profilePictureUrl = await _fileStorageService.UploadFileAsync(updateProfileDto.ProfilePicture) ?? null;
                }

                _logger.LogInformation("Attempting to find user by ID: {UserId}", userId);
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ID: {UserId}", userId);
                    return NotFound("User not found");
                }

                _logger.LogInformation("Successfully retrieved user for ID: {UserId}", userId);
                user.FirstName = updateProfileDto.Firstname ?? user.FirstName;
                user.LastName = updateProfileDto.Lastname ?? user.LastName;
                user.ProfilePicture = !string.IsNullOrWhiteSpace(profilePictureUrl) ? profilePictureUrl : user.ProfilePicture;

                _logger.LogInformation("Attempting to update user profile for ID: {UserId}", userId);
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed to update user profile for ID: {UserId}", userId);
                    return BadRequest(result.Errors);
                }

                _logger.LogInformation("User profile updated successfully for ID: {UserId}", userId);
                var profileDto = user.ToUserProfileResponseDto();
                return Ok(profileDto);
            }

        }
    }

}
