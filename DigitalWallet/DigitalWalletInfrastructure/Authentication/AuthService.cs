using DigitalWalletCore.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Hangfire;
using DigitalWalletCore.Interfaces;
using DigitalWalletInfrastructure.Data;
using DigitalWalletCore.Exceptions;
using DigitalWalletCore.Dtos.User;
using Microsoft.EntityFrameworkCore;

namespace DigitalWalletInfrastructure.Authentication
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;
        private readonly UserManager<AppUser> _userManager;
        private readonly SymmetricSecurityKey _key;
        private readonly IDistributedCache _cache;
        private readonly ILogger<AuthService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public AuthService(IConfiguration config, UserManager<AppUser> userManager, IDistributedCache cache, ILogger<AuthService> logger, IWebHostEnvironment env, ApplicationDbContext context, IAuditService auditService)
        {
            _config = config;
            _userManager = userManager;
            _cache = cache;
            _env = env;
            _context = context;
            _auditService = auditService;
            var signingKeyString = _config["JWT:SigningKey"]; // S6781: Key is loaded from secure source (User Secrets/Environment Variables, not from appsettings.json)
            if (string.IsNullOrWhiteSpace(signingKeyString))
            {
                throw new InvalidOperationException("JWT:SigningKey is not configured. Configure it via User Secrets (development) or environment variables (production).");
            }
            _key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKeyString)); // NOSONAR - S6781
            _logger = logger;
        }

        public async Task<string> CreateToken(AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Name, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            _logger.LogInformation("Creating JWT token for user {Email} with roles: {Roles}", user.Email, string.Join(", ", roles));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = creds,
                Issuer = _config["JWT:Issuer"],
                Audience = _config["JWT:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            var finalToken = tokenHandler.WriteToken(token);
            _logger.LogInformation("JWT token created successfully for user {Email}", user.Email);
            return finalToken;
        }

        public async Task<string> CreateRefreshToken(AppUser user)
        {
            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            _context.RefreshToken.Add(new RefreshToken
            {
                Id = Guid.NewGuid(), UserId = user.Id, TokenHash = HashToken(rawToken),
                CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await _context.SaveChangesAsync();
            return rawToken;
        }

        public async Task<TokenResponseDto?> RefreshTokenAsync(string refreshToken)
        {
            var tokenHash = HashToken(refreshToken);
            var storedToken = await _context.RefreshToken.Include(t => t.User)
                .SingleOrDefaultAsync(t => t.TokenHash == tokenHash);
            if (storedToken is null || !storedToken.IsActive || storedToken.User.IsDeactivated)
                return null;

            storedToken.RevokedAt = DateTime.UtcNow;
            var nextRefreshToken = await CreateRefreshToken(storedToken.User);
            storedToken.ReplacedByTokenHash = HashToken(nextRefreshToken);
            await _context.SaveChangesAsync();

            return new TokenResponseDto { AccessToken = await CreateToken(storedToken.User), RefreshToken = nextRefreshToken };
        }

        public async Task RevokeUserRefreshTokensAsync(string userId)
        {
            var activeTokens = await _context.RefreshToken.Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow).ToListAsync();
            foreach (var token in activeTokens) token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        public async Task LoginWithGoogleAsync(ClaimsPrincipal claimsPrincipal, HttpContext context)
        {
            var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Google login failed: email claim is missing.");
                throw new InvalidOperationException("Google login failed: email claim is missing.");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogInformation("No existing user found for Google login with email {Email}. Creating new user.", email);
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = claimsPrincipal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
                    LastName = claimsPrincipal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user);

                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to create user for Google login with email {Email}. Errors: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    throw new InvalidOperationException($"Failed to create user for Google login: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                var roleResult = await _userManager.AddToRoleAsync(user, "User");
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Failed to assign 'User' role to new user created for Google login with email {Email}. Errors: {Errors}", email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    throw new InvalidOperationException($"Failed to assign 'User' role for Google login: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }

                _logger.LogInformation("New user created successfully for Google login with email {Email}", email);

            }

            var info = new UserLoginInfo("Google",
            claimsPrincipal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
             "Google");

            var loginResult = await _userManager.AddLoginAsync(user, info);

            if (!loginResult.Succeeded)
            {
                _logger.LogError("Failed to add Google login info for user with email {Email}. Errors: {Errors}", email, string.Join(", ", loginResult.Errors.Select(e => e.Description)));
                throw new InvalidOperationException($"Failed to add Google login info: {string.Join(", ", loginResult.Errors.Select(e => e.Description))}");
            }

            var jwtToken = await CreateToken(user);
            var refreshToken = await CreateRefreshToken(user);

            context.Response.Cookies.Append("ACCESS_TOKEN", jwtToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = _env.IsProduction() ? true : context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

            context.Response.Cookies.Append("REFRESH_TOKEN", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = _env.IsProduction() ? true : context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            _logger.LogInformation("Google login tokens issued successfully for user {Email}", email);
        }

        public async Task Logout(string jti)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            };

            await _cache.SetStringAsync(jti, "revoked", options);
            _logger.LogInformation("User logged out successfully with JTI: {JTI}", jti);
        }

        public async Task<AppUser> FindUserByWalletNumberAsync(string walletNumber)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Wallet.WalletNumber == walletNumber);
            if (user == null)
            {
                _logger.LogWarning("No user found with wallet number {WalletNumber}", walletNumber);
                throw new NotFoundException($"No user found with wallet number {walletNumber}");
            }
            else
            {
                _logger.LogInformation("User found with wallet number {WalletNumber}: {Email}", walletNumber, user.Email);
                return user;
            }
        }

        public async Task<School> GetSchoolByCodeAsync(string schoolCode)
        {
            var school = await _context.School.FirstOrDefaultAsync(s => s.Code == schoolCode);
            if (school == null)
            {
                _logger.LogWarning("No school found with code {SchoolCode}", schoolCode);
                throw new NotFoundException($"No school found with code {schoolCode}");
            }
            else
            {
                _logger.LogInformation("School found with code {SchoolCode}: {Name}", schoolCode, school.Name);
                return school;
            }
        }

        public async Task<bool> ApproveMerchantAsync(Guid merchantId, string schoolCode, string actorUserId)
        {
            var merchant = await _context.Merchant
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == merchantId && m.User.SchoolCode == schoolCode);
            if (merchant == null)
            {
                _logger.LogWarning("No merchant found with ID {MerchantId}", merchantId);
                return false;
            }

            merchant.IsApproved = true;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("MerchantApproved", nameof(Merchant), merchantId.ToString(), actorUserId, "{\"approved\":true}");

            _logger.LogInformation("Enqueing Email");

            BackgroundJob.Enqueue<IEmailService>(emailService =>
                 emailService.SendMerchantApprovalResponseEmail(
                    true,
                    merchant.BusinessName,
                    merchant.User.Email,
                    DateTime.UtcNow));

            _logger.LogInformation("Merchant with ID {MerchantId} approved successfully", merchantId);
            return true;    
        }

        public async Task<bool> RejectMerchantAsync(Guid merchantId, string schoolCode, string actorUserId)
        {
            var merchant = await _context.Merchant
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == merchantId && m.User.SchoolCode == schoolCode);
            if (merchant == null)
            {
                _logger.LogWarning("No merchant found with ID {MerchantId}", merchantId);
                return false;
            }

            merchant.IsApproved = false;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("MerchantRejected", nameof(Merchant), merchantId.ToString(), actorUserId, "{\"approved\":false}");

            _logger.LogInformation("Enqueing Email");

            BackgroundJob.Enqueue<IEmailService>(emailService =>
                 emailService.SendMerchantApprovalResponseEmail(
                    false,
                    merchant.BusinessName,
                    merchant.User.Email,
                    DateTime.UtcNow));

            _logger.LogInformation("Merchant with ID {MerchantId} rejected successfully", merchantId);
            return true;
        }

        public async Task<CreateSchoolAdminResponseDto> CreateSchoolAdminAsync(CreateSchoolAdminDto createSchoolAdminDto)
        {
            var school = await GetSchoolByCodeAsync(createSchoolAdminDto.SchoolCode);
            var schoolAdmins = await GetSchoolAdminByCodeAsync(createSchoolAdminDto.SchoolCode);

            var user = new AppUser
            {
                FirstName = createSchoolAdminDto.Firstname,
                LastName = createSchoolAdminDto.Lastname,
                Email = createSchoolAdminDto.Email,
                UserName = createSchoolAdminDto.Email,
                EmailConfirmed = true,
                SchoolCode = createSchoolAdminDto.SchoolCode,
                Wallet = new Wallet
                {
                    Balance = 0,
                    WalletNumber = $"SchAdmin00{schoolAdmins.Count + 1}"
                },
                School = school
            };

            var result = await _userManager.CreateAsync(user, createSchoolAdminDto.Password);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to create school admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                throw new Exception("Failed to create school admin");
            }

            _logger.LogInformation("School admin created successfully: {Email}", user.Email);

            _logger.LogInformation("Assigning 'SchoolAdmin' role to user {Email}", user.Email);
            var roleResult = await _userManager.AddToRoleAsync(user, "SchoolAdmin");

            return new CreateSchoolAdminResponseDto
            {
                Firstname = user.FirstName,
                Lastname = user.LastName,
                SchoolCode = user.School.Code,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };
        }
        
        public async Task<List<string>> GetSchoolAdminByCodeAsync(string schoolCode)
        {
            _logger.LogInformation("Searching for school admin with school code {SchoolCode}", schoolCode);

                var schoolAdmins = await (
                    from user in _context.Users
                    join userRole in _context.UserRoles on user.Id equals userRole.UserId
                    join role in _context.Roles on userRole.RoleId equals role.Id
                    where role.Name == "SchoolAdmin"
                        && user.SchoolCode == schoolCode
                    select user
                ).ToListAsync();

            if (schoolAdmins == null )
            {
                _logger.LogWarning("No school admin found for school code {SchoolCode}", schoolCode);
                throw new NotFoundException($"No school admin found for school code {schoolCode}");
            }

            if(schoolAdmins.Count == 0)
            {
                _logger.LogWarning("No school admins found for school code {SchoolCode}", schoolCode);
                return new List<string>();
            }

            _logger.LogInformation("School admins found for school code {SchoolCode}", schoolCode);

            List<string> schoolAdminDetails = new List<string>();
            
                foreach (AppUser admin in schoolAdmins)
                {
                    schoolAdminDetails.Add(admin.Email);
                }
            
            
            return schoolAdminDetails;
        }
    }
}
