using DigitalWalletApi.Extensions;
using DigitalWalletApi.Filter;
using DigitalWalletCore.Dtos.Merchant;
using DigitalWalletCore.Dtos.User;
using DigitalWalletCore.Entities;
using DigitalWalletCore.Interfaces;
using DigitalWalletInfrastructure.Mapper;
using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWalletApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]/")]

    public class MerchantController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IPaymentService _paystackService;
        private readonly IAuthService _authService;
        private readonly IWalletService _walletService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<MerchantController> _logger;

        public MerchantController(UserManager<AppUser> userManager, IAuthService authService, IWalletService walletService, IEmailService emailService, IWebHostEnvironment env, ILogger<MerchantController> logger, IPaymentService paystackService)
        {
            _userManager = userManager;
            _authService = authService;
            _walletService = walletService;
            _emailService = emailService;
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
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
        {
            try
            {
                _logger.LogInformation("Received login request for wallet number: {WalletNumber}", loginDto.WalletNumber);
                if (string.IsNullOrWhiteSpace(loginDto.WalletNumber) || string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    _logger.LogWarning("Login request for wallet number {WalletNumber} is missing required fields.", loginDto.WalletNumber);
                    return BadRequest("WalletNumber and password are required.");
                }

                _logger.LogInformation("Attempting to find user by wallet number: {WalletNumber}", loginDto.WalletNumber);
                var user = await _authService.FindUserByWalletNumberAsync(loginDto.WalletNumber);

                if (user == null)
                {
                    _logger.LogWarning("User not found for wallet number: {WalletNumber}", loginDto.WalletNumber);
                    return Unauthorized("Invalid Email or username");
                }

                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    _logger.LogWarning("Email not confirmed for user with wallet number: {WalletNumber}", loginDto.WalletNumber);
                    return Unauthorized("Please verify your email before logging in.");
                }

                _logger.LogInformation("Checking password for user with wallet number: {WalletNumber}", loginDto.WalletNumber);
                var passwordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
                if (!passwordValid)
                {
                    _logger.LogWarning("Invalid password for user with wallet number: {WalletNumber}", loginDto.WalletNumber);
                    return Unauthorized("Invalid password");
                }

                if (user.Merchant != null && !user.Merchant.IsApproved)
                {
                    _logger.LogWarning("Merchant account not approved for user with wallet number: {WalletNumber}", loginDto.WalletNumber);
                    return Unauthorized("Your merchant account is pending approval. Please wait for confirmation.");
                }

                _logger.LogInformation("Generating access and refresh tokens for user with wallet number: {WalletNumber}", loginDto.WalletNumber);
                var token = await _authService.CreateToken(user);
                var refreshToken = await _authService.CreateRefreshToken(user);

                _logger.LogInformation("Writing access and refresh tokens to cookies for user with wallet number: {WalletNumber}", loginDto.WalletNumber);
                WriteAuthTokenCookie("ACCESS_TOKEN", token, TimeSpan.FromHours(1));
                WriteAuthTokenCookie("REFRESH_TOKEN", refreshToken, TimeSpan.FromDays(7));

                var responsedto = loginDto.ToLoginResponseDto(user, token);

                _logger.LogInformation("Login process completed successfully for user with wallet number: {WalletNumber}", loginDto.WalletNumber);
                return Ok(responsedto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login for wallet number: {WalletNumber}", loginDto.WalletNumber);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [ServiceFilter(typeof(LogActionFilter))]
        [HttpGet("verify_email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromQuery] string email, [FromQuery] string token)
        {
            _logger.LogInformation("Received email verification request for email: {Email}", email);
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Email verification request for email {Email} is missing required fields.", email);
                return BadRequest("Email and token are required.");
            }

            // ASP.NET Core sometimes decodes '+' as a space (' ') in query parameters.
            // Identity tokens often include '+' characters, so we map them back here.
            token = token.Replace(" ", "+");

            try
            {
                _logger.LogInformation("Attempting to verify email for user with email: {Email}", email);
                await _emailService.VerifyEmail(email, token);
                _logger.LogInformation("Email verified successfully for user with email: {Email}", email);
                return Ok("Email verified successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email verification failed for user with email: {Email}", email);
                return BadRequest($"Email verification failed: {ex.Message}");
            }
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("Received logout request for user with ID: {UserId}", User.GetUserId());
            var jti = User.GetJti();
            if (string.IsNullOrWhiteSpace(jti))
            {
                _logger.LogWarning("Logout request failed: Token jti is missing for user with ID: {UserId}", User.GetUserId());
                return BadRequest("Token jti is missing.");
            }

            await _authService.Logout(jti);

            _logger.LogInformation("Clearing authentication cookies for user with ID: {UserId}", User.GetUserId());
            return Ok("Logged out successfully");
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPost("change_password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            _logger.LogInformation("Received change password request for user with ID: {UserId}", User.GetUserId());
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Change password request failed: User ID is missing.");
                return BadRequest("User ID is missing.");
            }

            _logger.LogInformation("Attempting to find user by ID: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", userId);
                return NotFound("User not found");
            }

            _logger.LogInformation("Checking current password for user with ID: {UserId}", userId);
            var passwordValid = await _userManager.CheckPasswordAsync(user, changePasswordDto.CurrentPassword);
            if (!passwordValid)
            {
                _logger.LogWarning("Invalid current password for user with ID: {UserId}", userId);
                return Unauthorized("Invalid current password");
            }

            _logger.LogInformation("Attempting to change password for user with ID: {UserId}", userId);
            var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to change password for user with ID: {UserId}", userId);
                return BadRequest(result.Errors);
            }

            _logger.LogInformation("Password changed successfully for user with ID: {UserId}", userId);
            return Ok("Password changed successfully");
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpDelete("delete_account")]
        [Authorize]
        public async Task<IActionResult> DeleteProfile()
        {
            var userId = User.GetUserId();
            _logger.LogInformation("Received request to delete profile for user with ID: {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", userId);
                return NotFound("User not found");
            }

            _logger.LogInformation("Attempting to delete profile for user with ID: {UserId}", userId);
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to delete profile for user with ID: {UserId}. Errors: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors);
            }

            _logger.LogInformation("Profile deleted successfully for user with ID: {UserId}", userId);
            return Ok("User deleted successfully");
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPost("forgot_password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailDto emailDto)
        {
            _logger.LogInformation("Received forgot password request for email: {Email}", emailDto.Email);
            var user = await _userManager.FindByEmailAsync(emailDto.Email);
            if (user == null)
            {
                _logger.LogWarning("User not found for email: {Email}", emailDto.Email);
                return NotFound("User not found");
            }

            _logger.LogInformation("Generating password reset token for user with email: {Email}", emailDto.Email);
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            _logger.LogInformation("Generated password reset token for user with email: {Email}.", emailDto.Email);
            BackgroundJob.Enqueue<IEmailService>(x =>
            x.SendForgotPasswordEmail(
                user.UserName,
                user.Email,
                resetToken));

            _logger.LogInformation("Enqueued forgot password email for user with email: {Email}", emailDto.Email);
            return Ok($"Email : {user.Email}\t" +
                $"Token : {resetToken}");
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPost("reset_password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromQuery] string email, [FromQuery] string token, [FromBody] PasswordDto newPassword)
        {
            _logger.LogInformation("Received reset password request for email: {Email}", email);
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword.Password))
            {
                _logger.LogWarning("Reset password request for email {Email} is missing required fields.", email);
                return BadRequest("Email, token, and new password are required.");
            }

            token = token.Replace(" ", "+");

            try
            {
                _logger.LogInformation("Attempting to find user by email: {Email}", email);
                await _emailService.ResetPasswordEmail(email, token, newPassword.Password);
                _logger.LogInformation("Password reset successfully for user with email: {Email}", email);
                return Ok("Password reset successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset failed for user with email: {Email}", email);
                return BadRequest($"Password reset failed: {ex.Message}");
            }
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpGet("merchant/profile")]
        [Authorize(Roles = "Merchant")]
        public async Task<IActionResult> GetMerchantProfile()
        {
            _logger.LogInformation("Received request to get merchant profile for user with ID: {UserId}", User.GetUserId());
            var user = await _userManager.GetUserAsync(User);

            _logger.LogInformation("Attempting to retrieve merchant profile for user with ID: {UserId}", User.GetUserId());
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", User.GetUserId());
                return NotFound("User not found");
            }

            _logger.LogInformation("Successfully retrieved merchant profile for user with ID: {UserId}", User.GetUserId());
            var profileDto = user.ToMerchantProfileResponseDto(user.Merchant.Id);
            return Ok(profileDto);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPost("update_merchant_profile")]
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

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpGet("login/google/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLoginCallback([FromQuery] string returnUrl)
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                return Unauthorized();
            }

            await _authService.LoginWithGoogleAsync(result.Principal, HttpContext);

            // Prevent open-redirect vulnerabilities: only redirect to local URLs supplied by the user.
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Fallback to a safe default when returnUrl is missing or not local.
            return Redirect("/");
        }
    }
}
