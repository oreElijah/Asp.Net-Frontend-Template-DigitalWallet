using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Wallet;
using DigitalWalletCore.Entities;
using DigitalWalletCore.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletInfrastructure.Services
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepo;
        private readonly ILogger<WalletService> _logger;

        public WalletService(IWalletRepository walletRepo, ILogger<WalletService> logger)
        {
            _walletRepo = walletRepo;
            _logger = logger;
        }

        public async Task<AppResponse<Wallet>> CreateMerchantWallet(string userId)
        {
            var wallet = await _walletRepo.CreateMerchantWallet(userId);

            if(wallet == null)
            {
                _logger.LogError("Failed to create merchant wallet for user {UserId}", userId);
                return new AppResponse<Wallet>()
                {
                    Succeeded = false,
                    Message = "Failed to create merchant wallet"
                };
            }

            _logger.LogInformation("Successfully created merchant wallet for user {UserId}", userId);
            return wallet;
        }

        public async Task<AppResponse<Wallet>> CreateStudentWallet(string matricNumber, string userId)
        {
            var wallet = await _walletRepo.CreateStudentWallet(matricNumber, userId);

            if(wallet == null)
            {
                _logger.LogError("Failed to create student wallet for user {UserId} with matric number {MatricNumber}", userId, matricNumber);
                return new AppResponse<Wallet>()
                {
                    Succeeded = false,
                    Message = "Failed to create student wallet"
                };
            }

            _logger.LogInformation("Successfully created student wallet for user {UserId} with matric number {MatricNumber}", userId, matricNumber);
            return wallet;
        }

        public async Task<AppResponse<bool>> DeleteWalletById(Guid walletId, string userId)
        {
            var response = await _walletRepo.DeleteWalletById(walletId, userId);

            if (response == null)
            {
                _logger.LogError("Failed to delete wallet with ID {WalletId} for user {UserId}", walletId, userId);
                return new AppResponse<bool>()
                {
                    Succeeded = false,
                    Message = "Failed to delete wallet"
                };
            }

            _logger.LogInformation("Successfully deleted wallet with ID {WalletId} for user {UserId}", walletId, userId);
            return response;
        }

        public async Task<AppResponse<decimal>> GetWalletBalance(Guid walletId, string userId)
        {
            var response = await _walletRepo.GetWalletBalance(walletId, userId);

            if (response == null)
            {
                _logger.LogError("Failed to retrieve balance for wallet with ID {WalletId} for user {UserId}", walletId, userId);
                return new AppResponse<decimal>()
                {
                    Succeeded = false,
                    Message = "Failed to retrieve wallet balance"
                };
            }

            _logger.LogInformation("Successfully retrieved balance for wallet with ID {WalletId} for user {UserId}", walletId, userId);
            return response;
        }

        public async Task<AppResponse<WalletDto>> GetWalletById(Guid walletId, string userId)
        {
            var response = await _walletRepo.GetWalletById(walletId, userId);

            if (response == null)
            {
                _logger.LogError("Failed to retrieve wallet with ID {WalletId} for user {UserId}", walletId, userId);
                return new AppResponse<WalletDto>()
                {
                    Succeeded = false,
                    Message = "Failed to retrieve wallet"
                };
            }

            _logger.LogInformation("Successfully retrieved wallet with ID {WalletId} for user {UserId}", walletId, userId);
            return response;
        }

        public async Task<AppResponse<WalletDto>> GetWalletByUserId(string userId)
        {
            var response = await _walletRepo.GetWalletByUserId(userId);

            if (response == null)
            {
                _logger.LogError("Failed to retrieve wallet for user {UserId}", userId);
                return new AppResponse<WalletDto>()
                {
                    Succeeded = false,
                    Message = "Failed to retrieve wallet"
                };
            }

            _logger.LogInformation("Successfully retrieved wallet for user {UserId}", userId);
            return response;
        }

        public async Task<AppResponse<WalletSearchDto>> GetWalletByWalletNumber(string walletNumber)
        {
            var response = await _walletRepo.GetWalletByWalletNumber(walletNumber);

            if (response == null)
            {
                _logger.LogError("Failed to retrieve wallet with number {WalletNumber}", walletNumber);
                return new AppResponse<WalletSearchDto>()
                {
                    Succeeded = false,
                    Message = "Failed to retrieve wallet"
                };
            }

            _logger.LogInformation("Successfully retrieved wallet with number {WalletNumber}", walletNumber);
            return response;
        }

        public async Task<AppResponse<WalletResponseDto>> GetWalletDetailsById(Guid walletId, string userId)
        {
            var response = await _walletRepo.GetWalletDetailsById(walletId, userId);

            if (response == null)
            {
                _logger.LogError("Failed to retrieve wallet details for wallet with ID {WalletId} for user {UserId}", walletId, userId);
                return new AppResponse<WalletResponseDto>()
                {
                    Succeeded = false,
                    Message = "Failed to retrieve wallet details"
                };
            }

            _logger.LogInformation("Successfully retrieved wallet details for wallet with ID {WalletId} for user {UserId}", walletId, userId);
            return response;
        }

        public async Task<AppResponse<bool>> LockWalletAsync(string walletNumber)
        {
            var response = await _walletRepo.LockWalletAsync(walletNumber);

            if (response == null)
            {
                _logger.LogError("Failed to lock wallet with number {WalletNumber}", walletNumber);
                return new AppResponse<bool>()
                {
                    Succeeded = false,
                    Message = "Failed to lock wallet"
                };
            }

            _logger.LogInformation("Successfully locked wallet with number {WalletNumber}", walletNumber);
            return response;
        }
    }
}
