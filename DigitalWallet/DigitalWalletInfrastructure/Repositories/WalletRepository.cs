using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Wallet;
using DigitalWalletCore.Entities;
using DigitalWalletCore.Enums;
using DigitalWalletCore.Interfaces;
using DigitalWalletInfrastructure.Data;
using DigitalWalletInfrastructure.Mapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletInfrastructure.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WalletRepository> _logger;
        private static readonly PasswordHasher<Wallet> PinHasher = new();

        public WalletRepository(ApplicationDbContext context, ILogger<WalletRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AppResponse<Wallet>> CreateMerchantWallet(string userId, string pin)
        {
            _logger.LogInformation("Creating merchant wallet for user with ID: {UserId}", userId);
            var walletNumber = await GenerateMerchantWalletNumberAsync();

            var wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                WalletNumber = walletNumber,
                Balance = 0,
                IsLocked = false,
                PinHash = PinHasher.HashPassword(new Wallet(), pin),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            await _context.Wallet.AddAsync(wallet);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Merchant wallet created with ID: {WalletId} and Wallet Number: {WalletNumber}", wallet.Id, wallet.WalletNumber);
            return new AppResponse<Wallet>
            {
                Data = wallet,
                Succeeded = true,
                Message = "Merchant wallet created successfully"
            };
        }

        public async Task<AppResponse<Wallet>> CreateStudentWallet(string matricNumber, string userId, string pin)
        {
            _logger.LogInformation("Creating student wallet for user with ID: {UserId}", userId);

            var wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                WalletNumber = matricNumber,
                Balance = 0,
                IsLocked = false,
                PinHash = PinHasher.HashPassword(new Wallet(), pin),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            await _context.Wallet.AddAsync(wallet);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Student wallet created with ID: {WalletId} and Wallet Number: {WalletNumber}", wallet.Id, wallet.WalletNumber);
            return new AppResponse<Wallet>
            {
                Data = wallet,
                Succeeded = true,
                Message = "Student wallet created successfully"
            };
        }

        public async Task<AppResponse<bool>> DeleteWalletById(Guid walletId, string userId)
        {
            var wallet = await WalletExists(walletId, userId);
            if (wallet == null)
            {
                _logger.LogWarning("Attempted to delete wallet with ID: {WalletId}, but it was not found", walletId);
                return new AppResponse<bool>
                {
                    Succeeded = false,
                    Message = "Wallet not found"
                };
            }
            
            if(wallet.Succeeded == false)
            {
                _logger.LogWarning("Attempted to delete wallet with ID: {WalletId}, but it does not belong to user with ID: {UserId}", walletId, userId);
                return new AppResponse<bool>
                {
                    Succeeded = false,
                    Message = "Wallet does not belong to the user"
                };
            }

            _context.Wallet.Remove(wallet.Data);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Wallet with ID: {WalletId} deleted successfully", walletId);
            return new AppResponse<bool>
            {
                Data = true,
                Succeeded = true,
                Message = "Wallet deleted successfully"
            };
        }

        public async Task<string> GenerateMerchantWalletNumberAsync()
        {
            string walletNumber;
            do
            {
                walletNumber = "Mer" + Random.Shared.Next(100000, 999999).ToString();
            } while (
            await _context.Merchant.AnyAsync(m => m.User.Wallet.WalletNumber == walletNumber)
            );

            _logger.LogInformation("Generated unique merchant wallet number: {WalletNumber}", walletNumber);
            return walletNumber;
        }

        public async Task<AppResponse<decimal>> GetWalletBalance(Guid walletId, string userId)
        {
            var wallet = await WalletExists(walletId, userId);

            if (wallet == null)
            {
                _logger.LogWarning("Attempted to retrieve balance for wallet with ID: {WalletId}, but it was not found", walletId);
                return new AppResponse<decimal>
                {
                    Succeeded = false,
                    Message = "Wallet not found"
                };
            }

            if (wallet.Succeeded == false)
            {
                _logger.LogWarning("Attempted to retrieve balance for wallet with ID: {WalletId}, but it does not belong to user with ID: {UserId}", walletId, userId);
                return new AppResponse<decimal>
                {
                    Succeeded = false,
                    Message = "Wallet does not belong to the user"
                };
            }

            _logger.LogInformation("Retrieved balance for wallet with ID: {WalletId}. Balance: {Balance}", walletId, wallet.Data.Balance);
            return new AppResponse<decimal>
            {
                Data = wallet.Data.Balance,
                Succeeded = true,
                Message = "Wallet balance retrieved successfully"
            };
        }

        public async Task<AppResponse<WalletDto>> GetWalletById(Guid walletId, string userId)
        {
            var wallet = await WalletExists(walletId, userId);

            if (wallet == null)
            {
                _logger.LogWarning("Attempted to retrieve wallet with ID: {WalletId}, but it was not found", walletId);
                return new AppResponse<WalletDto>
                {
                    Succeeded = false,
                    Message = "Wallet not found"
                };
            }

            if (wallet.Succeeded == false)
            {
                _logger.LogWarning("Attempted to retrieve wallet with ID: {WalletId}, but it does not belong to user with ID: {UserId}", walletId, userId);
                return new AppResponse<WalletDto>
                {
                    Succeeded = false,
                    Message = "Wallet does not belong to the user"
                };
            }

            _logger.LogInformation("Retrieved wallet with ID: {WalletId}", walletId);
            return new AppResponse<WalletDto>
            {
                Data = wallet.Data.ToWalletDto(),
                Succeeded = true,
                Message = "Wallet retrieved successfully"
            };
        }

        public async Task<AppResponse<WalletDto>> GetWalletByUserId(string userId)
        {
            var wallet = await _context.Wallet.FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                _logger.LogWarning("Attempted to retrieve wallet for user with ID: {UserId}, but it was not found", userId);
                return new AppResponse<WalletDto>
                {
                    Succeeded = false,
                    Message = "Wallet not found"
                };
            }

            _logger.LogInformation("Retrieved wallet for user with ID: {UserId}", userId);
            return new AppResponse<WalletDto>
            {
                Data = wallet.ToWalletDto(),
                Succeeded = true,
                Message = "Wallet retrieved successfully"
            };
        }

        public async Task<AppResponse<WalletSearchDto>> GetWalletByWalletNumber(string walletNumber, string userId)
        {
            var wallet = await WalletExistsByWalletNumber(walletNumber);
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (wallet == null)
            {
                _logger.LogWarning("Attempted to retrieve wallet with number: {WalletNumber}, but it was not found", walletNumber);
                return new AppResponse<WalletSearchDto>
                {
                    Succeeded = false,
                    Message = "Wallet not found"
                };
            }

            if (user == null)
            {
                _logger.LogWarning("Attempted to retrieve wallet for user with ID: {UserId}, but the user was not found", userId);
                return new AppResponse<WalletSearchDto>
                {
                    Succeeded = false,
                    Message = "User not found"
                };
            }

            if(wallet.Data.SchoolCode != user.SchoolCode)
            {
                _logger.LogWarning("Attempted to retrieve wallet with number: {WalletNumber}, but it does not belong to the user's school", walletNumber);
                return new AppResponse<WalletSearchDto>
                {
                    Succeeded = false,
                    Message = "Wallet does not belong to the user's school"
                };
            }

            _logger.LogInformation("Retrieved wallet with number: {WalletNumber}", walletNumber);
            return new AppResponse<WalletSearchDto>
            {
                Data = wallet.Data,
                Succeeded = true,
                Message = "Wallet retrieved successfully"
            };
        }

        public async Task<AppResponse<WalletResponseDto>> GetWalletDetailsById(Guid walletId, string userId)
        {
            var wallet = await _context.Wallet
                .Include(w => w.User)
                .Include(w => w.SentTransactions)
                .Include(w => w.ReceivedTransactions)
                .FirstOrDefaultAsync(w => w.Id == walletId);

            if (wallet == null)
            {
                _logger.LogWarning("Attempted to retrieve wallet details for wallet with ID: {WalletId}, but it was not found", walletId);
                return new AppResponse<WalletResponseDto>
                {
                    Succeeded = false,
                    Message = "Wallet not found"
                };
            }

            _logger.LogInformation("Retrieved wallet details for wallet with ID: {WalletId}", walletId);
            return new AppResponse<WalletResponseDto>
            {
                Data = wallet.ToWalletResponseDto(),
                Succeeded = true,
                Message = "Wallet details retrieved successfully"
            };
        }

        public async Task<AppResponse<bool>> LockOrUnlockWalletAsync(string walletNumber)
        {
            _logger.LogInformation("Attempting to lock wallet with number: {WalletNumber}", walletNumber);
            var wallet = await _context.Wallet.FirstOrDefaultAsync(w => w.WalletNumber == walletNumber);
            if (wallet == null)
            {
                _logger.LogWarning("Attempted to lock wallet with number: {WalletNumber}, but it was not found", walletNumber);
                return new AppResponse<bool>
                {
                    Succeeded = false,
                    Message = "Wallet not found"
                };
            }

            if (wallet.IsLocked)
            {
                _logger.LogWarning("Attempting to unlock wallet with number: {WalletNumber}", walletNumber);
                wallet.IsLocked = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Unlocked wallet with number: {WalletNumber}", walletNumber);
                return new AppResponse<bool>
                {
                    Succeeded = true,
                    Message = "Wallet unlocked successfully"
                };

            }

            _logger.LogInformation("Locking wallet with number: {WalletNumber}", walletNumber);
            wallet.IsLocked = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Locked wallet with number: {WalletNumber}", walletNumber);
            return new AppResponse<bool>
            {
                Succeeded = true,
                Message = "Wallet locked successfully"
            };
        }

        public async Task<AppResponse<Wallet>> WalletExists(Guid walletId, string userId)
        {
            var wallet = await _context.Wallet
                .Include(w => w.User)
                .FirstOrDefaultAsync(w => w.Id == walletId);

            _logger.LogInformation("Checked existence of wallet with ID: {WalletId} for user with ID: {UserId}. Exists: {Exists}", walletId, userId, wallet != null);

            if (wallet == null)
            {
                _logger.LogWarning("Wallet with ID: {WalletId} does not exist for user with ID: {UserId}", walletId, userId);
                return new AppResponse<Wallet>
                {
                    Succeeded = false,
                    Message = "Wallet not found"
                };
            }

            if(wallet.UserId != userId)
            {
                _logger.LogWarning("Wallet with ID: {WalletId} does not belong to user with ID: {UserId}", walletId, userId);
                return new AppResponse<Wallet>
                {
                    Succeeded = false,
                    Message = "Wallet does not belong to the user"
                };
            }

            return new AppResponse<Wallet>
            {
                Data = wallet,
                Succeeded = true,
                Message = "Wallet exists"
            };
        }

        public async Task<AppResponse<WalletSearchDto>> WalletExistsByWalletNumber(string walletNumber)
        {
            var wallet = await _context.Wallet
                .Include(w => w.User)
                .FirstOrDefaultAsync(w => w.WalletNumber == walletNumber);

            if (wallet == null)
            {
                _logger.LogWarning("Attempted to check existence of wallet with number: {WalletNumber}, but it was not found", walletNumber);
                return new AppResponse<WalletSearchDto>
                {
                    Succeeded = false,
                    Message = "Wallet not found"
                };
            }

            _logger.LogInformation("Wallet with number: {WalletNumber} exists", walletNumber);
            return new AppResponse<WalletSearchDto>
            {
                Data = wallet.ToWalletSearchDto(),
                Succeeded = true,
                Message = "Wallet exists"
            };
        }

        public async Task<AppResponse<WalletStatementResponseDto>> GetWalletStatementAsync(string userId, WalletStatementRequestDto requestDto)
        {
            requestDto.PageNumber = Math.Max(1, requestDto.PageNumber);
            requestDto.PageSize = Math.Clamp(requestDto.PageSize, 1, 100);

            var wallet = await _context.Wallet
                .AsNoTracking()
                .Include(w => w.User)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                _logger.LogWarning("Attempted to retrieve wallet statement for user with ID: {UserId}, but the wallet was not found", userId);
                return new AppResponse<WalletStatementResponseDto>
                {
                    Succeeded = false,
                    Message = "Wallet not found"
                };
            }

            var transactions = _context.Transaction
                .AsNoTracking()
                .Where(t =>t.SenderWalletNumber == wallet.WalletNumber ||
                    t.ReceiverWalletNumber == wallet.WalletNumber);

            // Apply filters based on requestDto
            if (requestDto.StartDate.HasValue)
            {
                transactions = transactions.Where(t => t.CreatedAt >= requestDto.StartDate.Value);
            }

            if (requestDto.EndDate.HasValue)
            {
                transactions = transactions.Where(t => t.CreatedAt <= requestDto.EndDate.Value);
            }

            if (requestDto.MinAmount.HasValue)
            {
                transactions = transactions.Where(t => t.Amount >= requestDto.MinAmount.Value);
            }

            if (requestDto.MaxAmount.HasValue)
            {
                transactions = transactions.Where(t => t.Amount <= requestDto.MaxAmount.Value);
            }

            if (requestDto.Type.HasValue)
            {
                transactions = transactions.Where(x =>
                    x.Type == requestDto.Type.Value);
            }

            if (requestDto.Status.HasValue)
            {
                transactions = transactions.Where(x =>
                    x.Status == requestDto.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(requestDto.Search))
            {
                transactions = transactions.Where(t =>
                    t.Reference.Contains(requestDto.Search) ||
                    (t.Description != null &&
                    t.Description.Contains(requestDto.Search)));
            }

            var totalTransactions = await transactions.CountAsync();
            var totalCredits = await transactions.Where(t => t.Status == TransactionStatus.Successful && t.ReceiverWalletNumber == wallet.WalletNumber).Select(x => (decimal?)x.Amount).SumAsync()??0;
            var totalDebits = await transactions.Where(t => t.Status == TransactionStatus.Successful && t.SenderWalletNumber == wallet.WalletNumber).Select(x => (decimal?)x.Amount).SumAsync()??0;

            var totalPages = (int)Math.Ceiling(totalTransactions / (double)requestDto.PageSize);

            var pagedTransactions = await transactions
                .OrderByDescending(t => t.CreatedAt)
                .Skip((requestDto.PageNumber - 1) * requestDto.PageSize)
                .Take(requestDto.PageSize)
                .ToListAsync();

            var transactionDtos = pagedTransactions.Select(t => t.ToTransactionStatementDto(wallet)).ToList();

            var response = new WalletStatementResponseDto
            {
                CurrentBalance = wallet.Balance,
                TotalCredits = totalCredits,
                TotalDebits = totalDebits,
                TotalTransactions = totalTransactions,
                TotalPages = totalPages,
                Transactions = transactionDtos
            };

            return new AppResponse<WalletStatementResponseDto>
            {
                Data = response,
                Succeeded = true,
                Message = "Wallet statement retrieved successfully"
            };
        }

    }
}
