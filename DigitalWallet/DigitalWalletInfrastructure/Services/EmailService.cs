using DigitalWalletCore.Interfaces;
using Microsoft.AspNetCore.Hosting;
using DigitalWalletInfrastructure.Data;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using DigitalWalletCore.Entities;
using DigitalWalletCore.Exceptions;
using System.Web;
using System.Text.Json;
using DigitalWalletCore.Enums;

namespace DigitalWalletInfrastructure.Services
{
    public class EmailService : IEmailService
    {

        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly  IWebHostEnvironment _environment;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<EmailService> _logger;

        public EmailService(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment environment, UserManager<AppUser> userManager, ILogger<EmailService> logger)
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task ResetPasswordEmail(string email, string token, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                throw new NotFoundException("User with email not found");
            }

            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, newPassword);
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                throw new UnauthorizedAccessException("Invalid or expired password reset token.");
            }
        }
        public async Task SendForgotPasswordEmail(string userName, string email, string resetToken)
        {
            var BrevoUrl = _configuration["BrevoUrl"];
            var apiKey = _configuration["BREVO_API_KEY"];
            var fromEmail = _configuration["Email:From"];
            var fromName = _configuration["Email:FromName"] ?? "Amala Place";
            var baseUrl = _configuration["App:BaseUrl"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogError("Email settings are missing. Configure BREVO_API_KEY and Email:From.");
                return;
            }

            var templatePath = Path.Combine(_environment.ContentRootPath, "Template", "ForgotPassword.html");
            var html = await File.ReadAllTextAsync(templatePath);
            html = html.Replace("{{name}}", userName);

            var resetLink = $"{baseUrl}api/v1/Account/reset-password?" +
                    $"email={HttpUtility.UrlEncode(email)}&" +
                    $"token={HttpUtility.UrlEncode(resetToken)}";

            html = html.Replace("{{reset_link}}", resetLink);
            var payload = new Dictionary<string, object>
            {
                ["sender"] = new Dictionary<string, string> { ["name"] = fromName, ["email"] = fromEmail },
                ["to"] = new[] { new Dictionary<string, string> { ["email"] = email, ["name"] = userName } },
                ["subject"] = "Reset Your Password",
                ["htmlContent"] = html
            };

            try
            {
                using var http = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, BrevoUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };

                request.Headers.Add("accept", "application/json");
                request.Headers.Add("api-key", apiKey);

                using var response = await http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send email (Brevo {(int)response.StatusCode}): {body}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email due to an exception.");
            }
        }

        public async Task SendVerifyUserEmail(string firstName, string email, string verify_token, string walletNumber)
        {
            var BrevoUrl = _configuration["BrevoUrl"];
            var apiKey = _configuration["BREVO_API_KEY"];
            var fromEmail = _configuration["Email:From"];
            var fromName = _configuration["Email:FromName"] ?? "Amala Place";
            var baseUrl = _configuration["App:BaseUrl"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogError("Email settings are missing. Configure BREVO_API_KEY and Email:From.");
                return;
            }

            var templatePath = Path.Combine(_environment.ContentRootPath, "Template", "VerifyEmail.html");
            var html = await File.ReadAllTextAsync(templatePath);
            html = html.Replace("{{name}}", firstName);

            var verifyLink = $"{baseUrl}/api/v1/Account/verify_email?" +
                    $"email={HttpUtility.UrlEncode(email)}&" +
                    $"token={HttpUtility.UrlEncode(verify_token)}";

            html = html.Replace("{{verification_link}}", verifyLink);
            html = html.Replace("{{wallet_number}}", walletNumber);

            var payload = new Dictionary<string, object>
            {
                ["sender"] = new Dictionary<string, string> { ["name"] = fromName, ["email"] = fromEmail },
                ["to"] = new[] { new Dictionary<string, string> { ["email"] = email, ["name"] = firstName } },
                ["subject"] = "Verify Your Email",
                ["htmlContent"] = html
            };

            try
            {
                using var http = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, BrevoUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };

                request.Headers.Add("accept", "application/json");
                request.Headers.Add("api-key", apiKey);

                using var response = await http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send email (Brevo {(int)response.StatusCode}): {body}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email due to an exception.");
            }
        }

        public async Task VerifyEmail(string email, string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                throw new NotFoundException("User with email not found");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                throw new UnauthorizedAccessException("Invalid or expired verification token.");
            }
        }

        public async Task SendCustomerDepositSuccessfulEmail(string userName, string email, Guid Id, decimal amount, string reference, string WalletNumber, TransactionStatus status)
        {
            var BrevoUrl = _configuration["BrevoUrl"];
            var apiKey = _configuration["BREVO_API_KEY"];
            var fromEmail = _configuration["Email:From"];
            var fromName = _configuration["Email:FromName"] ?? "Campus Pay";

            var templatePath = Path.Combine(_environment.ContentRootPath, "Template", "DepositSuccessful.html");

            var html = await File.ReadAllTextAsync(templatePath);

            html = html.Replace("{{name}}", userName);
            html = html.Replace("{{id}}", Id.ToString());
            html = html.Replace("{{WalletNumber}}", WalletNumber);
            html = html.Replace("{{amount}}", amount.ToString("N0"));
            html = html.Replace("{{reference}}", reference);
            html = html.Replace("{{status}}", status.ToString());

            var payload = new Dictionary<string, object>
            {
                ["sender"] = new Dictionary<string, string>
                {
                    ["name"] = fromName,
                    ["email"] = fromEmail
                },
                ["to"] = new[]
                {
            new Dictionary<string, string>
            {
                ["email"] = email,
                ["name"] = userName
            }
        },
                ["subject"] = "Deposit Successful - Campus Pay",
                ["htmlContent"] = html
            };

            try
            {
                using var http = new HttpClient();

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    BrevoUrl)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json")
                };

                request.Headers.Add("accept", "application/json");
                request.Headers.Add("api-key", apiKey);

                using var response = await http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();

                    _logger.LogError($"Failed to send customer Deposit email (Brevo {(int)response.StatusCode}): {body}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send customer Deposit email.");
            }
        }

        public async Task SendCustomerWithdrawalSuccessfulEmail(string userName, string email, Guid Id, decimal amount, string reference, string AccountName, string AccountNumber, string BankName, string WalletNumber, TransactionStatus status)
        {
            var BrevoUrl = _configuration["BrevoUrl"];
            var apiKey = _configuration["BREVO_API_KEY"];
            var fromEmail = _configuration["Email:From"];
            var fromName = _configuration["Email:FromName"] ?? "Campus Pay";

            var templatePath = Path.Combine(_environment.ContentRootPath, "Template", "WithdrawalSuccessful.html");

            var html = await File.ReadAllTextAsync(templatePath);

            html = html.Replace("{{name}}", userName);
            html = html.Replace("{{AccountName}}", AccountName);
            html = html.Replace("{{AccountNumber}}", AccountNumber);
            html = html.Replace("{{BankName}}", BankName);
            html = html.Replace("{{WalletNumber}}", WalletNumber);
            html = html.Replace("{{id}}", Id.ToString());
            html = html.Replace("{{amount}}", amount.ToString("N0"));
            html = html.Replace("{{reference}}", reference);
            html = html.Replace("{{status}}", status.ToString());

            var payload = new Dictionary<string, object>
            {
                ["sender"] = new Dictionary<string, string>
                {
                    ["name"] = fromName,
                    ["email"] = fromEmail
                },
                ["to"] = new[]
                {
            new Dictionary<string, string>
            {
                ["email"] = email,
                ["name"] = userName
            }
        },
                ["subject"] = "Withdrawal Successful - Campus Pay",
                ["htmlContent"] = html
            };

            try
            {
                using var http = new HttpClient();

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    BrevoUrl)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json")
                };

                request.Headers.Add("accept", "application/json");
                request.Headers.Add("api-key", apiKey);

                using var response = await http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();

                    _logger.LogError($"Failed to send customer Deposit email (Brevo {(int)response.StatusCode}): {body}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send customer Deposit email.");
            }
        }

    }
}
