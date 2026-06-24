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

        public class SchoolAdminController : ControllerBase
        {
            private readonly UserManager<AppUser> _userManager;
            private readonly IPaymentService _paystackService;
            private readonly IAuthService _authService;
            private readonly IWalletService _walletService;
            private readonly IEmailService _emailService;
            private readonly ISchoolService _schoolService;
            private readonly IWebHostEnvironment _env;
            private readonly ILogger<SchoolAdminController> _logger;
            private readonly ApplicationDbContext _context;

            public SchoolAdminController(UserManager<AppUser> userManager, IAuthService authService, IWalletService walletService, IEmailService emailService, ISchoolService schoolService, IWebHostEnvironment env, ILogger<SchoolAdminController> logger, IPaymentService paystackService, ApplicationDbContext context)
            {
                _userManager = userManager;
                _authService = authService;
                _walletService = walletService;
                _emailService = emailService;
                _context = context;
                _schoolService = schoolService;
                _paystackService = paystackService;
                _logger = logger;
                _env = env;
            }

            private void WriteAuthTokenCookie(string name, string value, TimeSpan lifetime)
            {
                Response.Cookies.Append(name, value, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = _env.IsProduction() ? true : Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.Add(lifetime)
                });
            }

            [ServiceFilter(typeof(LogActionFilter))]
            [HttpPost("login")]
            [AllowAnonymous]
            public async Task<IActionResult> SchoolAdminLogin([FromBody] AdminLoginRequestDto loginDto)
            {
                try
                {
                    _logger.LogInformation("Received login request for email: {Email}", loginDto.Email);
                    if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
                    {
                        _logger.LogWarning("Login request for email {Email} is missing required fields.", loginDto.Email);
                        return BadRequest("Email and password are required.");
                    }

                    _logger.LogInformation("Attempting to find user by email: {Email}", loginDto.Email);
                    var user = await _userManager.FindByEmailAsync(loginDto.Email);

                    if (user == null)
                    {
                        _logger.LogWarning("User not found for email: {Email}", loginDto.Email);
                        return Unauthorized("Invalid Email or username");
                    }

                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        _logger.LogWarning("Email not confirmed for user with email: {Email}", loginDto.Email);
                        return Unauthorized("Please verify your email before logging in.");
                    }

                    _logger.LogInformation("Checking password for user with email: {Email}", loginDto.Email);
                    var passwordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
                    if (!passwordValid)
                    {
                        _logger.LogWarning("Invalid password for user with email: {Email}", loginDto.Email);
                        return Unauthorized("Invalid password");
                    }

                    if (!await _userManager.IsInRoleAsync(user, "Admin") && !await _userManager.IsInRoleAsync(user, "SchoolAdmin"))
                    {
                        _logger.LogWarning("User with email: {Email} does not have the required admin roles.", loginDto.Email);
                        return Unauthorized("You do not have the required admin roles.");
                    }

                    _logger.LogInformation("Generating access and refresh tokens for user with email: {Email}", loginDto.Email);
                    var token = await _authService.CreateToken(user);
                    var refreshToken = await _authService.CreateRefreshToken(user);

                    _logger.LogInformation("Writing access and refresh tokens to cookies for user with email: {Email}", loginDto.Email);
                    WriteAuthTokenCookie("ACCESS_TOKEN", token, TimeSpan.FromHours(1));
                    WriteAuthTokenCookie("REFRESH_TOKEN", refreshToken, TimeSpan.FromDays(7));

                    var responsedto = loginDto.ToAdminLoginResponseDto(user, token);

                    _logger.LogInformation("Login process completed successfully for user with email: {Email}", loginDto.Email);
                    return Ok(responsedto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during login for email: {Email}", loginDto.Email);
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }

            [ServiceFilter(typeof(LogActionFilter))]
            [HttpPost("Create-SchoolAdmin")]
            [Authorize(Roles = "SchoolAdmin")]
            public async Task<IActionResult> CreateSchoolAdmin([FromBody] CreateSchoolAdminDto createSchoolAdminDto)
            {
                _logger.LogInformation("Received request to create school admin for email: {Email}", createSchoolAdminDto.Email);
                var result = await _authService.CreateSchoolAdminAsync(createSchoolAdminDto);
                if (result == null)
                {
                    _logger.LogWarning("Failed to create school admin for email: {Email}", createSchoolAdminDto.Email);
                    return BadRequest("Failed to create school admin.");
                }
                _logger.LogInformation("School admin created successfully for email: {Email}", createSchoolAdminDto.Email);
                return CreatedAtAction(nameof(CreateSchoolAdmin), new { email = result.Email }, result);
            }

            [ServiceFilter(typeof(LogActionFilter))]
            [HttpGet("ApproveMerchant")]
            [Authorize(Roles = "SchoolAdmin")]
            public async Task<IActionResult> ApproveMerchant([FromQuery] string merchantId)
            {
                _logger.LogInformation("Received request to approve merchant with ID: {MerchantId}", merchantId);
                var result = await _authService.ApproveMerchantAsync(Guid.Parse(merchantId));
                if (!result)
                {
                    _logger.LogWarning("Failed to approve merchant with ID: {MerchantId}", merchantId);
                    return BadRequest("Failed to approve merchant.");
                }
                _logger.LogInformation("Merchant with ID: {MerchantId} approved successfully", merchantId);
                return Ok("Merchant approved successfully");
            }

            [ServiceFilter(typeof(LogActionFilter))]
            [HttpGet("profile")]
            [Authorize(Roles = "SchoolAdmin")]
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
            [HttpPost("Lock/Wallet")]
            [Authorize(Roles = "SchoolAdmin")]
            public async Task<IActionResult> LockWallet([FromQuery] string walletNumber)
            {
                _logger.LogInformation("Received request to lock wallet with number: {WalletNumber}", walletNumber);
                var result = await _walletService.LockWalletAsync(walletNumber);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed to lock wallet with number: {WalletNumber}", walletNumber);
                    return BadRequest("Failed to lock wallet.");
                }
                _logger.LogInformation("Wallet with number: {WalletNumber} locked successfully", walletNumber);
                return Ok("Wallet locked successfully");
            }

            [ServiceFilter(typeof(LogActionFilter))]
            [HttpGet("banks")]
            [AllowAnonymous]
            public async Task<IActionResult> GetBanks()
            {
                var banks = await _paystackService.GetBanksAsync();

                return Ok(banks);
            }
        }

    }
}
