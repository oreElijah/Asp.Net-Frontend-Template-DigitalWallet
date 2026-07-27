using Amazon.Runtime.Internal.Util;
using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Payment;
using DigitalWalletCore.Enums;
using DigitalWalletCore.Exceptions;
using DigitalWalletCore.Interfaces;
using DigitalWalletCore.Options;
using DigitalWalletInfrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DigitalWalletInfrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly IDistributedCache _cache;
        private readonly string _secretKey;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(HttpClient httpClient, ApplicationDbContext context, IOptions<PaystackOptions> settings, IEmailService emailService, IConfiguration config, ILogger<PaymentService> logger, IDistributedCache cache)
        {
            _httpClient = httpClient;
            _context = context;
            _secretKey = settings.Value.SecretKey;
            _emailService = emailService;
            _logger = logger;
            _config = config;
            _cache = cache;
        }

        public async Task<AppResponse<InitializePaymentResponseDto>> InitializeDepositAsync(Guid TransactionId, string walletNumber)
        {
            _logger.LogInformation("Initializing deposit for TransactionId: {TransactionId} and walletNumber: {walletNumber}", TransactionId, walletNumber);
            var transaction = await _context.Transaction
            .Include(o => o.ReceiverWallet)
            .ThenInclude(w => w.User)
            .FirstOrDefaultAsync(
                o => o.Id == TransactionId &&
                o.ReceiverWalletNumber == walletNumber);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found for TransactionId: {TransactionId} and walletNumber: {walletNumber}", TransactionId, walletNumber);
                return new AppResponse<InitializePaymentResponseDto>
                {
                    Succeeded = false,
                    Message = "Transaction not found."
                };
            }

            if (transaction.Status == TransactionStatus.Successful)
            {
                return new AppResponse<InitializePaymentResponseDto>
                {
                    Succeeded = false,
                    Message = "Transaction already processed."
                };
            }

            _logger.LogInformation("Transaction found: {TransactionId} with amount: {Amount}", transaction.Id, transaction.Amount);
            var reference_ = $"Deposit_{transaction.Id}_{Guid.NewGuid():N}";

            _logger.LogInformation("Generated reference: {Reference} for TransactionId: {TransactionId}", reference_, transaction.Id);
            transaction.Reference = reference_;
            transaction.Status = TransactionStatus.Pending;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Transaction updated with reference and status set to pending for TransactionId: {TransactionId}", transaction.Id);

            var amountInKobo = (int)(transaction.Amount * 100);

            _logger.LogInformation("Amount in Kobo: {AmountInKobo} for TransactionId: {TransactionId}", amountInKobo, transaction.Id);
            var payload = new
            {
                email = transaction.ReceiverWallet.User.Email,
                amount = amountInKobo,
                reference = reference_
            };

            var json = JsonSerializer.Serialize(payload);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            _logger.LogInformation("Payload prepared for Paystack API for TransactionId: {TransactionId}", transaction.Id);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);

            var response = await _httpClient.PostAsync(_config["Paystack:Transaction"], content);

            _logger.LogInformation("Received response from Paystack API for TransactionId: {TransactionId} with status code: {StatusCode}", transaction.Id, response.StatusCode);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Response body from Paystack API for TransactionId: {TransactionId}: {ResponseBody}", transaction.Id, responseBody);
            var initializePaymentResponse = new InitializePaymentResponseDto
            {
                AuthorizationUrl = JsonDocument.Parse(responseBody).RootElement.GetProperty("data").GetProperty("authorization_url").GetString(),
                Reference = reference_
            };
            return new AppResponse<InitializePaymentResponseDto>
            {
                Succeeded = true,
                Message = "Payment initialized successfully.",
                Data = initializePaymentResponse
            };
        }

        public async Task<AppResponse<string>> VerifyDepositAsync(string reference)
        {
            _logger.LogInformation("Verifying deposit for reference: {Reference}", reference);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);

            _logger.LogInformation("Set authorization header for Paystack API with secret key for reference: {Reference}", reference);
            var response = await _httpClient.GetAsync($"{_config["Paystack:Verify"]}{reference}");

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Received response from Paystack API for reference: {Reference} with status code: {StatusCode}", reference, response.StatusCode);
            var root = JsonDocument.Parse(responseBody).RootElement;

            var paymentStatus = root
                .GetProperty("data")
                .GetProperty("status")
                .GetString();

            _logger.LogInformation("Payment status from Paystack API for reference: {Reference} is {PaymentStatus}", reference, paymentStatus);
            var transaction = await _context.Transaction
                .Include(o => o.ReceiverWallet)
                .ThenInclude(w => w.User)
                .FirstOrDefaultAsync(o => o.Reference == reference);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found for reference: {Reference}", reference);
                return new AppResponse<string>
                {
                    Succeeded = false,
                    Message = "Transaction not found."
                };
            }

            if (transaction.Status == TransactionStatus.Successful)
            {
                return new AppResponse<string>
                {
                    Succeeded = true,
                    Message = "Already processed"
                };
            }

            if (paymentStatus != "success")
            {
                _logger.LogWarning("Payment not successful for reference: {Reference}. Payment status: {PaymentStatus}", reference, paymentStatus);
                transaction.Status = TransactionStatus.Failed;

                await _context.SaveChangesAsync();
                return new AppResponse<string>
                {
                    Succeeded = false,
                    Message = "Payment verification failed."
                };
            }


            if (transaction.Status == TransactionStatus.Pending)
            {
                transaction.Status = TransactionStatus.Successful;
                transaction.ReceiverBalanceBefore = transaction.ReceiverWallet.Balance;
                transaction.ReceiverBalanceAfter = transaction.ReceiverWallet.Balance + transaction.Amount;
                transaction.ReceiverWallet.Balance += transaction.Amount;
                transaction.ReceiverWallet.LastUpdatedAt = DateTime.UtcNow;
                transaction.ReceiverWallet.ReceivedTransactions.Add(transaction);

                await _context.SaveChangesAsync();

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == transaction.ReceiverWallet.UserId);

                var customerName = $"{user?.FirstName} {user?.LastName}";
                BackgroundJob.Enqueue<IEmailService>(x =>
                x.SendCustomerDepositSuccessfulEmail(
                    customerName,
                    user.Email,
                    transaction.Id,
                    transaction.Amount,
                    transaction.Reference,
                    transaction.ReceiverWalletNumber,
                    transaction.Status));
            }

            return new AppResponse<string>
            {
                Succeeded = true,
                Message = "Deposit verified successfully.",
                Data = reference
            };
        }

        public async Task<AppResponse<InitializePaymentResponseDto>> InitializeWithdrawalAsync(Guid TransactionId, string walletNumber)
        {
            _logger.LogInformation("Initializing withdrawal for TransactionId: {TransactionId} and walletNumber: {walletNumber}", TransactionId, walletNumber);
            var transaction = await _context.Transaction
            .Include(x => x.SenderWallet)
            .ThenInclude(x => x.User)
            .ThenInclude(x => x.Merchant)
            .FirstOrDefaultAsync(x =>
            x.Id == TransactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found for TransactionId: {TransactionId} and walletNumber: {walletNumber}", TransactionId, walletNumber);
                return new AppResponse<InitializePaymentResponseDto>
                {
                    Succeeded = false,
                    Message = "Transaction not found"
                };
            }

            if (transaction.Status == TransactionStatus.Successful)
            {
                return new AppResponse<InitializePaymentResponseDto>
                {
                    Succeeded = false,
                    Message = "Transaction already processed."
                };
            }

            _logger.LogInformation("Transaction found: {TransactionId} with amount: {Amount}", transaction.Id, transaction.Amount);

            _logger.LogInformation("Checking if sender wallet has sufficient balance for TransactionId: {TransactionId}. Current balance: {Balance}, Withdrawal amount: {Amount}", transaction.Id, transaction.SenderWallet.Balance, transaction.Amount);
            var recipientCode = transaction.SenderWallet.User.Merchant?.TransferRecipientCode;

            if (string.IsNullOrWhiteSpace(recipientCode))
            {
                _logger.LogWarning("Merchant bank account not configured for TransactionId: {TransactionId}. Cannot proceed with withdrawal.", transaction.Id);
                return new AppResponse<InitializePaymentResponseDto>
                {
                    Succeeded = false,
                    Message = "Merchant bank account not configured."
                };
            }

            if (transaction.SenderWallet.Balance < transaction.Amount)
            {
                _logger.LogWarning("Insufficient balance in sender wallet for TransactionId: {TransactionId}. Current balance: {Balance}, Withdrawal amount: {Amount}", transaction.Id, transaction.SenderWallet.Balance, transaction.Amount);
                return new AppResponse<InitializePaymentResponseDto>
                {
                    Succeeded = false,
                    Message = "Insufficient balance"
                };
            }

            _logger.LogInformation("Sufficient balance in sender wallet for TransactionId: {TransactionId}. Proceeding with withdrawal.", transaction.Id);
            _logger.LogInformation("Removing the amount from the user wallet now to avoid multiple withdrawal");
            transaction.SenderWallet.Balance -= transaction.Amount;
            transaction.SenderWallet.LockedBalance += transaction.Amount;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Preparing payload for Paystack transfer API for TransactionId: {TransactionId}. Amount: {Amount}, RecipientCode: {RecipientCode}, Description: {Description}", transaction.Id, transaction.Amount, recipientCode, transaction.Description);
            var payload = new
            {
                source = "balance",
                amount = (int)(transaction.Amount * 100),
                recipient = recipientCode,
                reason = transaction.Description
            };

            var json = JsonSerializer.Serialize(payload);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            _logger.LogInformation("Payload prepared for Paystack transfer API for TransactionId: {TransactionId}. Payload: {Payload}", transaction.Id, json);

            _logger.LogInformation("Setting authorization header for Paystack API with secret key for TransactionId: {TransactionId}", transaction.Id);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _secretKey);

            try
            {
                var response = await _httpClient.PostAsync(
                    _config["Paystack:Transfer"],
                    content);
                _logger.LogInformation("Received response from Paystack transfer API for TransactionId: {TransactionId} with status code: {StatusCode}", transaction.Id, response.StatusCode);
                var body = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Paystack Response: {Body}", body);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Paystack Error: {Body}", body);

                    return new AppResponse<InitializePaymentResponseDto>
                    {
                        Succeeded = false,
                        Message = body
                    };
                }
                var transferReference =
                    JsonDocument.Parse(body)
                    .RootElement
                    .GetProperty("data")
                    .GetProperty("reference")
                    .GetString();

                _logger.LogInformation("Received response from Paystack transfer API for TransactionId: {TransactionId} with status code: {StatusCode}. Transfer reference: {TransferReference}", transaction.Id, response.StatusCode, transferReference);
                transaction.Reference = transferReference;
                transaction.Status = TransactionStatus.Pending;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Transaction updated with transfer reference and status set to pending for TransactionId: {TransactionId}", transaction.Id);

                _logger.LogInformation("Preparing InitializePaymentResponseDto for TransactionId: {TransactionId} with transfer reference: {TransferReference}", transaction.Id, transferReference);
                var initializePaymentResponse = new InitializePaymentResponseDto
                {
                    AuthorizationUrl = "",
                    Reference = transferReference
                };

                _logger.LogInformation("Returning successful response for withdrawal initialization for TransactionId: {TransactionId} with transfer reference: {TransferReference}", transaction.Id, transferReference);
                return new AppResponse<InitializePaymentResponseDto>
                {
                    Succeeded = true,
                    Data = initializePaymentResponse,
                    Message = "Withdrawal initialized"
                };
            }
            catch
            {
                transaction.SenderWallet.Balance += transaction.Amount;
                transaction.SenderWallet.LockedBalance -= transaction.Amount;

                await _context.SaveChangesAsync();

                throw;
            }
        }

        public async Task<AppResponse<string>> VerifyWithdrawalAsync(string reference)
        {
            _logger.LogInformation("Verifying withdrawal for reference: {Reference}", reference);

            if (reference.StartsWith("TRF_mock_"))
            {
                return new AppResponse<string> { Succeeded = true, Data = "success", Message = "Mock transfer successful" };
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);

            _logger.LogInformation("Set authorization header for Paystack API with secret key for reference: {Reference}", reference);
            var response = await _httpClient.GetAsync($"{_config["Paystack:Verify_Transfer"]}{reference}");

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();

            var root = JsonDocument.Parse(body).RootElement;

            var status = root.GetProperty("data")
                    .GetProperty("status")
                    .GetString();

            _logger.LogInformation("Payment status from Paystack API for reference: {Reference} is {PaymentStatus}", reference, status);

            _logger.LogInformation("Retrieving transaction from database for reference: {Reference}", reference);
            var transaction = await _context.Transaction
                    .Include(x => x.SenderWallet)
                    .ThenInclude(sw => sw.User)
                    .ThenInclude(u => u.Merchant)
                    .FirstOrDefaultAsync(x =>
                        x.Reference == reference);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found for reference: {Reference}", reference);
                return new AppResponse<string>
                {
                    Succeeded = false,
                    Message = "Transaction not found"
                };
            }

            if (transaction.Status == TransactionStatus.Successful)
            {
                return new AppResponse<string>
                {
                    Succeeded = true,
                    Message = "Already processed"
                };
            }

            _logger.LogInformation("Transaction found for reference: {Reference}. TransactionId: {TransactionId}, Amount: {Amount}, SenderWalletNumber: {SenderWalletNumber}", reference, transaction.Id, transaction.Amount, transaction.SenderWalletNumber);
            if (status != "success")
            {
                _logger.LogWarning("Withdrawal not successful for reference: {Reference}. Payment status: {PaymentStatus}", reference, status);
                transaction.Status = TransactionStatus.Failed;
                transaction.SenderWallet.Balance += transaction.Amount;
                transaction.SenderWallet.LockedBalance =
                    Math.Max(
                        0,
                        transaction.SenderWallet.LockedBalance - transaction.Amount);

                await _context.SaveChangesAsync();
                return new AppResponse<string>
                {
                    Succeeded = false,
                    Message = "Withdrawal verification failed."
                };
            }

            _logger.LogInformation("Withdrawal successful for reference: {Reference}. Updating transaction status to successful and updating sender wallet balance.", reference);
            transaction.Status = TransactionStatus.Successful;
            transaction.SenderBalanceBefore = transaction.SenderWallet.Balance + transaction.SenderWallet.LockedBalance;
            transaction.SenderBalanceAfter = transaction.SenderWallet.Balance;
            transaction.SenderWallet.LockedBalance =
                Math.Max(
                    0,
                    transaction.SenderWallet.LockedBalance - transaction.Amount);
            transaction.SenderWallet.LastUpdatedAt = DateTime.UtcNow;
            transaction.SenderWallet.SentTransactions.Add(transaction);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Transaction updated with successful status and sender wallet balance updated for reference: {Reference}. TransactionId: {TransactionId}", reference, transaction.Id);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == transaction.SenderWallet.UserId);

            var customerName = $"{user?.FirstName} {user?.LastName}";

            _logger.LogInformation("Enqueuing email job for successful withdrawal for reference: {Reference}. CustomerName: {CustomerName}, Email: {Email}", reference, customerName, user.Email);
            BackgroundJob.Enqueue<IEmailService>(x =>
            x.SendCustomerWithdrawalSuccessfulEmail(
                customerName,
                user.Email,
                transaction.Id,
                transaction.Amount,
                transaction.Reference,
                transaction.SenderWallet.User.Merchant.AccountName,
                transaction.SenderWallet.User.Merchant.AccountNumber,
                transaction.SenderWallet.User.Merchant.BankName,
                transaction.SenderWalletNumber,
                transaction.Status));

            _logger.LogInformation("Returning successful response for withdrawal verification for reference: {Reference}", reference);
            return new AppResponse<string>
            {
                Succeeded = true,
                Message = "Withdrawal verified successfully.",
                Data = reference
            };
        }
        
       

        public async Task HandleWebhookForWithdrawalAsync(string body)
        {
            var root = JsonDocument.Parse(body).RootElement;

            var eventName = root.GetProperty("event").GetString();

            if (eventName != "transfer.success")
                return;

            var reference = root.GetProperty("data")
                    .GetProperty("reference")
                    .GetString();

            _logger.LogInformation("Payment status from Paystack API for reference: {Reference} is {PaymentStatus}", reference, eventName);
            var transaction = await _context.Transaction
                    .Include(x => x.SenderWallet)
                    .ThenInclude(o => o.SentTransactions)
                    .FirstOrDefaultAsync(x =>
                        x.Reference == reference);

            if (transaction != null)
            {
                _logger.LogInformation("Transaction found for reference: {Reference}. TransactionId: {TransactionId}", reference, transaction.Id);
                if (eventName != "charge.success")
                {
                    _logger.LogWarning("Withdrawal not successful for reference: {Reference}. Payment status: {PaymentStatus}", reference, eventName);

                    transaction.Status = TransactionStatus.Failed;
                    transaction.SenderWallet.Balance += transaction.Amount;
                    transaction.SenderWallet.LockedBalance =
                        Math.Max(
                            0,
                            transaction.SenderWallet.LockedBalance - transaction.Amount);
                    await _context.SaveChangesAsync();
                    return;
                }

                _logger.LogInformation("Withdrawal successful for reference: {Reference}. Updating transaction status to successful.", reference);

                transaction.Status = TransactionStatus.Successful;
                transaction.SenderWallet.LockedBalance =
                    Math.Max(
                        0,
                        transaction.SenderWallet.LockedBalance - transaction.Amount);
                transaction.SenderWallet.LastUpdatedAt = DateTime.UtcNow;
                transaction.SenderWallet.SentTransactions.Add(transaction);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Transaction updated with successful status and wallet balance updated for reference: {Reference}. TransactionId: {TransactionId}", reference, transaction.Id);
                var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == transaction.ReceiverWallet.UserId);

                _logger.LogInformation("User found for transaction reference: {Reference}. UserId: {UserId}, Email: {Email}", reference, user.Id, user.Email);
                var customerName = $"{user?.FirstName} {user?.LastName}";

                _logger.LogInformation("Enqueuing email job for successful deposit for reference: {Reference}. CustomerName: {CustomerName}, Email: {Email}", reference, customerName, user.Email);
                BackgroundJob.Enqueue<IEmailService>(x =>
            BackgroundJob.Enqueue<IEmailService>(x =>
            x.SendCustomerWithdrawalSuccessfulEmail(
                customerName,
                user.Email,
                transaction.Id,
                transaction.Amount,
                transaction.Reference,
                transaction.SenderWallet.User.Merchant.AccountName,
                transaction.SenderWallet.User.Merchant.AccountNumber,
                transaction.SenderWallet.User.Merchant.BankName,
                transaction.SenderWalletNumber,
                transaction.Status)));
            }
            _logger.LogWarning("Transaction not found for reference: {Reference}", reference);
        }

        public async Task HandleWebhookForDepositAsync(string body)
        {
            var root = JsonDocument.Parse(body).RootElement;

            var eventName = root
                .GetProperty("event")
                .GetString();

            var reference = root
                .GetProperty("data")
                .GetProperty("reference")
                .GetString();

            _logger.LogInformation("Deposit status from Paystack API for reference: {Reference} is {PaymentStatus}", reference, eventName);
            var transaction = await _context.Transaction
                .Include(o => o.ReceiverWallet)
                .ThenInclude(o => o.ReceivedTransactions)
                .FirstOrDefaultAsync(o => o.Reference == reference);

            if (transaction != null)
            {
                _logger.LogInformation("Transaction found for reference: {Reference}. TransactionId: {TransactionId}", reference, transaction.Id);

                if (transaction.Status == TransactionStatus.Successful)
                {
                    return;
                }
                if (eventName != "charge.success")
                {
                    _logger.LogWarning("Deposit not successful for reference: {Reference}. Payment status: {PaymentStatus}", reference, eventName);

                    transaction.Status = TransactionStatus.Failed;
                    await _context.SaveChangesAsync();
                    return;
                }

                _logger.LogInformation("Deposit successful for reference: {Reference}. Updating transaction status to successful.", reference);
                transaction.Status = TransactionStatus.Successful;
                transaction.ReceiverWallet.Balance += transaction.Amount;
                transaction.ReceiverWallet.LastUpdatedAt = DateTime.UtcNow;
                transaction.ReceiverWallet.ReceivedTransactions.Add(transaction);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Transaction updated with successful status and wallet balance updated for reference: {Reference}. TransactionId: {TransactionId}", reference, transaction.Id);
                var user = await _context.Users
             .FirstOrDefaultAsync(u => u.Id == transaction.ReceiverWallet.UserId);

                _logger.LogInformation("User found for transaction reference: {Reference}. UserId: {UserId}, Email: {Email}", reference, user.Id, user.Email);
                var customerName = $"{user?.FirstName} {user?.LastName}";

                _logger.LogInformation("Enqueuing email job for successful deposit for reference: {Reference}. CustomerName: {CustomerName}, Email: {Email}", reference, customerName, user.Email);
                BackgroundJob.Enqueue<IEmailService>(x =>
                x.SendCustomerDepositSuccessfulEmail(
                    customerName,
                    user.Email,
                    transaction.Id,
                    transaction.Amount,
                    transaction.Reference,
                    transaction.ReceiverWalletNumber,
                    transaction.Status));
            }
            _logger.LogWarning("Transaction not found for reference: {Reference}", reference);
        }

        public async Task<string> ResolveAccountAsync(string accountNumber, string bankCode)
        {
            var cacheKey = $"resolve_{accountNumber}_{bankCode}";
            var cached = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogInformation("Account name found in cache");
                var SerializedAccountName = JsonSerializer.Deserialize<string>(cached);

                if (SerializedAccountName == null)
                {
                    _logger.LogWarning("Cached account name is null for account number: {AccountNumber} and bank code: {BankCode}", accountNumber, bankCode);
                    throw new ValidationException("Cached account name is null. Please try again.");
                }
                
                return SerializedAccountName;
            }
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _secretKey);

            var url = $"{_config["Paystack:Resolve_Account"]}{accountNumber}&bank_code={bankCode}";

            _logger.LogInformation("Resolve URL: {Url}", url);
            _logger.LogInformation("Resolve Account Config Value: {Value}", _config["Paystack:Resolve_Account"]);
            var response = await _httpClient.GetAsync(
                $"{_config["Paystack:Resolve_Account"]}{accountNumber}&bank_code={bankCode}");

            var body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                string resetSeconds = "Unknown";

                foreach (var header in response.Headers)
                {
                    _logger.LogInformation("Found Header: {Key} = {Value}", header.Key, string.Join(", ", header.Value));
                }

                var targetHeader = response.Headers
                .FirstOrDefault(h => h.Key.Equals("x-ratelimit-reset", StringComparison.OrdinalIgnoreCase));

                if (targetHeader.Value != null)
                {
                    resetSeconds = targetHeader.Value.FirstOrDefault() ?? "Unknown";
                }
                // 2. Secondary fallback check just in case it leaks into Content headers
                else if (response.Content?.Headers != null)
                {
                    var contentHeader = response.Content.Headers
                        .FirstOrDefault(h => h.Key.Equals("x-ratelimit-reset", StringComparison.OrdinalIgnoreCase));

                    if (contentHeader.Value != null)
                    {
                        resetSeconds = contentHeader.Value.FirstOrDefault() ?? "Unknown";
                    }
                }

                _logger.LogWarning(
                    "Paystack rate limit hit for {AccountNumber}. Cooldown period: {ResetSeconds} seconds.",
                    accountNumber, resetSeconds);

                throw new ValidationException(
                    $"Paystack rate limit exceeded. Please retry in {resetSeconds} seconds.");
            }

            response.EnsureSuccessStatusCode();

            var root = JsonDocument.Parse(body).RootElement;

            if (!root.GetProperty("status").GetBoolean() || root.GetProperty("data").ValueKind == JsonValueKind.Null)
            {
                _logger.LogWarning("Account resolution failed for account number: {AccountNumber} and bank code: {BankCode}. Response message: {ResponseMessage}", accountNumber, bankCode, root.GetProperty("message").GetString());
                throw new ValidationException("Account resolution failed. Please check the account number and bank code and try again.");
            }

            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
            };

            var accountName = root.GetProperty("data").GetProperty("account_name").GetString();
            
            var serialized = JsonSerializer.Serialize(accountName);

            await _cache.SetStringAsync(cacheKey, serialized, options);
            _logger.LogInformation("Account name for {AccountNumber} retrieved.", accountNumber);

            return accountName;
        }

        public async Task<string> CreateTransferRecipientAsync(string accountName, string accountNumber, string bankCode)
        {
             var cacheKey = $"TransferRecipient_{accountName}_{accountNumber}_{bankCode}";
            var cached = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogInformation("Account name found in cache for creating transfer recipient");
                var SerializedTrfRecipient = JsonSerializer.Deserialize<string>(cached);

                if (SerializedTrfRecipient == null)
                {
                    _logger.LogWarning("Cached transfer recipient is null for account number: {AccountNumber} and bank code: {BankCode} when creating transfer recipient", accountNumber, bankCode);
                    throw new ValidationException("Cached transfer recipient is null. Please try again.");
                }

                return SerializedTrfRecipient;
            }
            
            _logger.LogInformation("Creating transfer recipient with account name: {AccountName}, account number: {AccountNumber}, bank code: {BankCode}", accountName, accountNumber, bankCode);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _secretKey);

            _logger.LogInformation("Preparing payload for creating transfer recipient with account name: {AccountName}, account number: {AccountNumber}, bank code: {BankCode}", accountName, accountNumber, bankCode);
            var payload = new
            {
                type = "nuban",
                name = accountName,
                account_number = accountNumber,
                bank_code = bankCode,
                currency = "NGN"
            };

            _logger.LogInformation("Payload prepared for creating transfer recipient: {Payload}", JsonSerializer.Serialize(payload));
            var json = JsonSerializer.Serialize(payload);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_config["Paystack:TransferRecipient"], content);

            var body = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "Paystack Response: {Body}",
                body);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                string resetSeconds = "unknown";

                if (response.Headers.TryGetValues("x-ratelimit-reset", out var values))
                {
                    resetSeconds = values.FirstOrDefault() ?? "unknown";
                }

                _logger.LogWarning(
                    "Paystack rate limit hit for {AccountName}. Cooldown period: {ResetSeconds} seconds.",
                    accountName, resetSeconds);

                throw new ValidationException(
                    $"Paystack rate limit exceeded. Please retry in {resetSeconds} seconds.");
            }

            response.EnsureSuccessStatusCode();

            var root = JsonDocument.Parse(body).RootElement;

            if (!root.GetProperty("status").GetBoolean() || root.GetProperty("data").ValueKind == JsonValueKind.Null)
            {
                _logger.LogWarning("Transfer recipient creation failed for account name: {AccountName}, account number: {AccountNumber}, bank code: {BankCode}. Response message: {ResponseMessage}", accountName, accountNumber, bankCode, root.GetProperty("message").GetString());
                throw new ValidationException("Transfer recipient creation failed. Please check the account details and try again.");
            }

            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
            };

            var trfRecipient = root.GetProperty("data").GetProperty("recipient_code").GetString();

            var serialized = JsonSerializer.Serialize(trfRecipient);

            await _cache.SetStringAsync(cacheKey, serialized, options);
            _logger.LogInformation("Transfer recipient code for {AccountNumber} retrieved.", accountNumber);

            return trfRecipient;
        }

        public async Task<string> GetBanksAsync()
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _secretKey);
            _logger.LogInformation("Getting banks list from Paystack");
            var response =
                await _httpClient.GetAsync(_config["Paystack:Get_Banks"]);
            _logger.LogInformation("Banks response received");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string?> GetBankNameByCodeAsync(string bankCode)
        {
            var cacheKey = $"Bank_{bankCode}";
            var cached = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogInformation("Bank name found in cache for bank code: {BankCode}", bankCode);
                var SerializedTrfRecipient = JsonSerializer.Deserialize<string>(cached);

                if (SerializedTrfRecipient == null)
                {
                    _logger.LogWarning("Cached bank name is null for bank code: {BankCode} when creating transfer recipient", bankCode);
                    throw new ValidationException("Cached bank name is null. Please try again.");
                }

                return SerializedTrfRecipient;
            }

            var response = await GetBanksAsync();

            var root = JsonDocument.Parse(response);

            var banks = root
                .RootElement
                .GetProperty("data");

            foreach (var bank in banks.EnumerateArray())
            {
                if (bank.GetProperty("code").GetString() == bankCode)
                {
                    var options = new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
                    };

                    var name = bank.GetProperty("name").GetString();
                    _logger.LogInformation("Found bank. Code: {Code}, Name: {Name}", bankCode, name);

                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(name), options);
                    return name;
                }
            }

            return null;
        }

    }
}
