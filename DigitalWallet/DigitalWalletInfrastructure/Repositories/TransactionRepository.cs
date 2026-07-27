using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Merchant;
using DigitalWalletCore.Dtos.Transaction;
using DigitalWalletCore.Entities;
using DigitalWalletCore.Enums;
using DigitalWalletCore.Interfaces;
using DigitalWalletInfrastructure.Data;
using DigitalWalletInfrastructure.Mapper;
using DigitalWalletInfrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Hangfire;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Data;
using Microsoft.AspNetCore.Identity;

namespace DigitalWalletInfrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IQRCodeService _qrCodeService;
        private readonly IWalletService _walletService;
        private readonly ILogger<TransactionRepository> _logger;
        private readonly IAuditService _auditService;
        private static readonly PasswordHasher<Wallet> PinHasher = new();

        public TransactionRepository(ApplicationDbContext context, IPaymentService paymentService, IWalletService walletService, ILogger<TransactionRepository> logger, IQRCodeService qrCodeService, IAuditService auditService)
        {
            _context = context;
            _paymentService = paymentService;
            _walletService = walletService;
            _logger = logger;
            _qrCodeService = qrCodeService;
            _auditService = auditService;
        }


        public async Task<AppResponse<DepositResponseDto>> DepositAsync(DepositDto depositDto, string userId)
        {
            if (!IsValidAmount(depositDto.Amount)) return InvalidAmountResponse<DepositResponseDto>();
            _logger.LogInformation("Starting deposit process for user {UserId} with amount {Amount}", userId, depositDto.Amount);
            var wallet = await _context.Wallet
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                _logger.LogWarning("Wallet not found for user {UserId}", userId);
                return new AppResponse<DepositResponseDto>
                {
                    Succeeded = false,
                    Message = "Wallet not found."
                };
            }
            _logger.LogInformation("Wallet found for user {UserId}: {WalletNumber}", userId, wallet.WalletNumber);
            if (wallet.IsLocked)
            {
                _logger.LogWarning("Wallet {WalletNumber} is locked for user {UserId}", wallet.WalletNumber, userId);
                return new AppResponse<DepositResponseDto>
                {
                    Succeeded = false,
                    Message = "Wallet is locked."
                };
            }

            _logger.LogInformation("Creating transaction for deposit of amount {Amount} to wallet {WalletNumber} and adding it to the database", depositDto.Amount, wallet.WalletNumber);
            // Create a new transaction
            var transaction = depositDto.ToTransactionFromDeposit(wallet.WalletNumber, TransactionStatus.Pending, "", wallet.Id);
            _context.Transaction.Add(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Transaction created with ID {TransactionId} for deposit of amount {Amount} to wallet {WalletNumber}", transaction.Id, depositDto.Amount, wallet.WalletNumber);
            //Deposit From payment gateway to wallet logic would be here
            _logger.LogInformation("Initializing deposit with payment service for transaction ID {TransactionId} and wallet number {WalletNumber}", transaction.Id, wallet.WalletNumber);
            var paymentResponse = await _paymentService.InitializeDepositAsync(transaction.Id, wallet.WalletNumber);

            if (!paymentResponse.Succeeded)
            {
                _logger.LogWarning("Failed to initialize deposit for transaction ID {TransactionId} and wallet number {WalletNumber}", transaction.Id, wallet.WalletNumber);
                transaction.Status = TransactionStatus.Failed;
                await _context.SaveChangesAsync();
                return new AppResponse<DepositResponseDto>
                {
                    Succeeded = false,
                    Message = paymentResponse.Message
                };
            }

            _logger.LogInformation("Creating deposit response for transaction ID {TransactionId} and wallet number {WalletNumber}", transaction.Id, wallet.WalletNumber);
            var DepositResponse = new DepositResponseDto
            {
                Transaction = transaction.ToTransactionResponseDto(),
                Amount = transaction.Amount,
                PaymentUrl = paymentResponse.Data.AuthorizationUrl,
                PaymentReference = paymentResponse.Data.Reference
            };

            _logger.LogInformation("Deposit process completed successfully for user {UserId} with amount {Amount}", userId, depositDto.Amount);
            return new AppResponse<DepositResponseDto>
            {
                Succeeded = true,
                Message = "Deposit successful.",
                Data = DepositResponse
            };
        }

        public async Task<AppResponse<List<TransactionDto>>> GetTransactionsByWalletIdAsync(Guid walletId, string userId)
        {
            _logger.LogInformation("Fetching transactions for wallet ID {WalletId} and user {UserId}", walletId, userId);
            var wallet = await _walletService.GetWalletDetailsById(walletId, userId);

            if (wallet.Data == null)
            {
                _logger.LogWarning("Wallet not found for user {UserId} and wallet ID {WalletId}", userId, walletId);
                return new AppResponse<List<TransactionDto>>
                {
                    Succeeded = false,
                    Message = "Wallet not found."
                };
            }
            
            _logger.LogInformation("Wallet found for user {UserId}: {WalletNumber}", userId, wallet.Data.WalletNumber);

            _logger.LogInformation("Retrieving sent and received transactions for wallet ID {WalletId}", walletId);
            var sentTransactions = wallet.Data.SentTransactions;
            var receivedTransactions = wallet.Data.ReceivedTransactions;

            _logger.LogInformation("Combining sent and received transactions for wallet ID {WalletId}", walletId);
            var allTransactions = new List<TransactionDto>();
            allTransactions.AddRange(sentTransactions);
            allTransactions.AddRange(receivedTransactions);
            allTransactions = allTransactions
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

            _logger.LogInformation("Returning combined transactions for wallet ID {WalletId} and user {UserId} in a descending order by creation date", walletId, userId);
            return new AppResponse<List<TransactionDto>>
            {
                Succeeded = true,
                Message = "Transactions retrieved successfully.",
                Data = allTransactions
            };
        }

        public async Task<AppResponse<TransactionDto>> ScanToChargeWalletAsync(BarcodeScanDto request, decimal amount, string userId, string pin)
        {
            if (!IsValidAmount(amount)) return InvalidAmountResponse<TransactionDto>();
            using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var barCode = request.BarCode;

                if (barCode == null || barCode.Length == 0)
                {
                    return new AppResponse<TransactionDto>
                    {
                        Succeeded = false,
                        Message = "No barcode image file was uploaded."
                    };
                }

                byte[] barCodebytes;
                using (var memoryStream = new MemoryStream())
                {
                    await barCode.CopyToAsync(memoryStream);
                    barCodebytes = memoryStream.ToArray();
                }

                var extractedData = await _qrCodeService.ScanBarcode(barCodebytes);

                if (extractedData == null)
                {
                    return new AppResponse<TransactionDto>
                    {
                        Succeeded = false,
                        Message = "Failed to extract data from barcode. Ensure the image is clear."
                    };
                }

                var receiverWallet = await _context.Wallet
                    .Include(rw => rw.User)
                    .FirstOrDefaultAsync(rw => rw.UserId == userId);

                var senderWallet = await _context.Wallet
                    .Include(sw => sw.User)
                    .FirstOrDefaultAsync(sw => sw.WalletNumber == extractedData);

                if (senderWallet == null || receiverWallet == null)
                {
                    return new AppResponse<TransactionDto>
                    {
                        Succeeded = false,
                        Message = "Either sender or receiver wallet not found."
                    };
                }

                var transferDto = new TransferDto
                {
                    Amount = amount,
                    Pin = pin,
                    Description = $"Transfer from {senderWallet.WalletNumber} to {receiverWallet.WalletNumber} using scan to charge",
                    ReceiverWalletNumber = receiverWallet.WalletNumber
                };

                var response = await ProcessTransactionAsync(senderWallet, receiverWallet, transferDto, dbTransaction, TransactionType.SantoPay);

                return response;
            }            
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transfer failed");

                if (_context.Database.CurrentTransaction != null)
                {
                    await dbTransaction.RollbackAsync();
                }

                throw;
            }
        }

        private async Task<AppResponse<TransactionDto>> ProcessTransactionAsync(Wallet senderWallet, Wallet receiverWallet,TransferDto transferDto, IDbContextTransaction dbTransaction, TransactionType transactionType = TransactionType.Transfer)
        {

            if (senderWallet.IsLocked || receiverWallet.IsLocked)
            {
                _logger.LogWarning("Either SenderWallet {SenderWalletNumber} or ReceiverWallet {ReceiverWalletNumber} is locked", senderWallet.WalletNumber, receiverWallet.WalletNumber);
                return new AppResponse<TransactionDto>
                {
                    Succeeded = false,
                    Message = "Wallet is locked."
                };
            }

            if (senderWallet.Balance < transferDto.Amount)
            {
                return new AppResponse<TransactionDto>
                {
                    Succeeded = false,
                    Message = "Insufficient balance"
                };
            }

            if (PinHasher.VerifyHashedPassword(senderWallet, senderWallet.PinHash, transferDto.Pin) == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Incorrect pin provided for transfer by user {UserId} and wallet {SenderWalletNumber}", senderWallet.UserId, senderWallet.WalletNumber);
                return new AppResponse<TransactionDto>
                {
                    Succeeded = false,
                    Message = "Incorrect pin."
                };
            }
            var transaction = transferDto.ToTransactionFromTransfer(senderWallet.WalletNumber, TransactionStatus.Successful, senderWallet.Id, receiverWallet.Id);
            transaction.Type = transactionType;

            transaction.SenderBalanceBefore = senderWallet.Balance;
            transaction.SenderBalanceAfter = senderWallet.Balance - transferDto.Amount;
            transaction.ReceiverBalanceBefore = receiverWallet.Balance;
            transaction.ReceiverBalanceAfter = receiverWallet.Balance + transferDto.Amount;

            _logger.LogInformation("SenderWallet and ReceiverWallet were found, Carrying out transferring Logic");
            senderWallet.Balance -= transferDto.Amount;
            receiverWallet.Balance += transferDto.Amount;
            senderWallet.LastUpdatedAt = DateTime.UtcNow;
            receiverWallet.LastUpdatedAt = DateTime.UtcNow;

            await _context.Transaction.AddAsync(transaction);

            _logger.LogInformation("Transfer process completed successfully for user {ReceiverWallet} with amount {Amount}", receiverWallet.WalletNumber, transferDto.Amount);

            senderWallet.SentTransactions.Add(transaction);
            receiverWallet.ReceivedTransactions.Add(transaction);

            await _context.SaveChangesAsync();

            var newTransferDto = transaction.ToTransactionResponseDto2(receiverWallet, senderWallet);

            await dbTransaction.CommitAsync();
            await _auditService.RecordAsync("WalletTransfer", nameof(Transaction), transaction.Id.ToString(), senderWallet.UserId, $"{{\"amount\":{transaction.Amount},\"type\":\"{transaction.Type}\"}}");

            BackgroundJob.Enqueue<IEmailService>(x =>
                x.SendReceiverTransferSuccessfulEmail(
                    receiverWallet.User.FirstName,
                    receiverWallet.User.Email,
                    transaction.Id,
                    transferDto.Amount,
                    newTransferDto.Reference,
                    transferDto.Description,
                    senderWallet.User.FirstName,
                    TransactionStatus.Successful));

            BackgroundJob.Enqueue<IEmailService>(x =>
                x.SendSenderTransferSuccessfulEmail(
                    senderWallet.User.FirstName,
                    senderWallet.User.Email,
                    transaction.Id,
                    transferDto.Amount,
                    newTransferDto.Reference,
                    transferDto.Description,
                    receiverWallet.User.FirstName,
                    TransactionStatus.Successful));

            return new AppResponse<TransactionDto>
            {
                Succeeded = true,
                Data = newTransferDto,
                Message = "Transfer Succeeded"
            };
        }

        public async Task<AppResponse<bool>> TransactionExistsAsync(Guid transactionId)
        {
            var exists = await _context.Transaction.AnyAsync(t => t.Id == transactionId);

            if (exists)
            {
                _logger.LogInformation("Transaction exists for transaction ID {TransactionId}", transactionId);
                return new AppResponse<bool>
                {
                    Succeeded = true,
                    Message = "Transaction exists.",
                    Data = true
                };
            }
            else
            {
                _logger.LogWarning("Transaction does not exist for transaction ID {TransactionId}", transactionId);
                return new AppResponse<bool>
                {
                    Succeeded = true,
                    Message = "Transaction does not exist.",
                    Data = false
                };
            }
        }

        public async Task<AppResponse<TransactionDto>> TransferAsync(TransferDto transferDto, string userId)
        {
            if (!IsValidAmount(transferDto.Amount)) return InvalidAmountResponse<TransactionDto>();
            using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                _logger.LogInformation("Starting Transfer process for user {UserId} with amount {amount} to WalletNumber {WalletNumber}", userId, transferDto.Amount, transferDto.ReceiverWalletNumber);
                var senderWallet = await _context.Wallet
                .Include(sw => sw.User)
                .FirstOrDefaultAsync(sw => sw.UserId == userId);

                var receiverWallet = await _context.Wallet
                .Include(rw => rw.User)
                .FirstOrDefaultAsync(rw => rw.WalletNumber == transferDto.ReceiverWalletNumber);

                if(senderWallet==null || receiverWallet == null)
                {
                    _logger.LogError("SemderWallet or ReceiverWallet is null, so ttransaction is cancelled");
                    return new AppResponse<TransactionDto>
                    {
                        Succeeded = false,
                        Message = "Wallet not found"
                    };
                }

                if (senderWallet.WalletNumber == transferDto.ReceiverWalletNumber || senderWallet.Id == receiverWallet.Id)
                {
                    return new AppResponse<TransactionDto>
                    {
                        Succeeded = false,
                        Message = "You cannot transfer to yourself"
                    };
                }

                if (!string.Equals(senderWallet.User.SchoolCode, receiverWallet.User.SchoolCode, StringComparison.OrdinalIgnoreCase))
                    return new AppResponse<TransactionDto> { Succeeded = false, Message = "Cross-school transfers are not permitted." };

                var response = await ProcessTransactionAsync(senderWallet, receiverWallet, transferDto, dbTransaction);

                return response;               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transfer failed");

                if (_context.Database.CurrentTransaction != null)
                {
                    await dbTransaction.RollbackAsync();
                }

                throw;
            }
        }

        public async Task<AppResponse<TransactionDto>> WithdrawAsync(WithdrawDto withdrawDto, string userId)
        {
            if (!IsValidAmount(withdrawDto.Amount)) return InvalidAmountResponse<TransactionDto>();
            _logger.LogInformation("Starting withdrawal process for user {UserId} with amount {Amount}", userId, withdrawDto.Amount);
            await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var wallet = await _context.Wallet.FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                _logger.LogWarning("Wallet not found for user {UserId}", userId);
                return new AppResponse<TransactionDto>
                {
                    Succeeded = false,
                    Message = "Wallet not found."
                };
            }
            _logger.LogInformation("Wallet found for user {UserId}: {WalletNumber}", userId, wallet.WalletNumber);
            if (wallet.IsLocked)
            {
                _logger.LogWarning("Wallet {WalletNumber} is locked for user {UserId}", wallet.WalletNumber, userId);
                return new AppResponse<TransactionDto>
                {
                    Succeeded = false,
                    Message = "Wallet is locked."
                };
            }

            _logger.LogInformation("Checking if wallet {WalletNumber} has sufficient balance for withdrawal. Available balance: {Balance}, requested amount: {Amount}", wallet.WalletNumber, wallet.Balance, withdrawDto.Amount);
            if (wallet.Balance < withdrawDto.Amount)
            {
                _logger.LogWarning("Insufficient balance in wallet {WalletNumber} for user {UserId}. Available balance: {Balance}, requested amount: {Amount}", wallet.WalletNumber, userId, wallet.Balance, withdrawDto.Amount);
                return new AppResponse<TransactionDto>
                {
                    Succeeded = false,
                    Message = "Insufficient balance."
                };
            }

            _logger.LogInformation("The user has sufficient balance for withdrawal, Creating transaction for Withdrawal of amount {Amount} to wallet {WalletNumber} and adding it to the database", withdrawDto.Amount, wallet.WalletNumber);
            var transaction = withdrawDto.ToTransactionFromWithdrawal(wallet.WalletNumber, TransactionStatus.Pending, "", wallet.Id);

            _logger.LogInformation("Checkin Pin for user {UserId} and wallet {WalletNumber}", userId, wallet.WalletNumber);
            if (PinHasher.VerifyHashedPassword(wallet, wallet.PinHash, withdrawDto.Pin) == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Incorrect pin provided for withdrawal by user {UserId} and wallet {WalletNumber}", userId, wallet.WalletNumber);
                return new AppResponse<TransactionDto>
                {
                    Succeeded = false,
                    Message = "Incorrect pin."
                };
            }

            await _context.AddAsync(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Initializing Withdrawal for Transaction {TransactionId} and Wallet {WalletNumber}", transaction.Id, wallet.WalletNumber);
            //Withdraw from wallet to payment gateway logic would be here
            var paymentResponse = await _paymentService.InitializeWithdrawalAsync(transaction.Id, wallet.WalletNumber);

            if (!paymentResponse.Succeeded)
            {
                transaction.Status = TransactionStatus.Failed;
                await _context.SaveChangesAsync();
                
                return new AppResponse<TransactionDto>
                {
                    Succeeded = false,
                    Message = paymentResponse.Message
                };
            }

            return new AppResponse<TransactionDto>
            {
                Succeeded = true,
                Message = "Withdrawal successful.",
                Data = transaction.ToTransactionResponseDto()
            };
        }

        private static bool IsValidAmount(decimal amount) => amount > 0 && amount <= 1_000_000m && decimal.Round(amount, 2) == amount;

        private static AppResponse<T> InvalidAmountResponse<T>() => new()
        {
            Succeeded = false,
            Message = "Amount must be positive, have at most two decimal places, and not exceed 1,000,000."
        };
    }
}
