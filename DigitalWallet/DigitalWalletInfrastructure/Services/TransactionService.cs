using Amazon.Runtime.Internal.Util;
using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Merchant;
using DigitalWalletCore.Dtos.Transaction;
using DigitalWalletCore.Entities;
using DigitalWalletCore.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletInfrastructure.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepo;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(ITransactionRepository transactionRepo, ILogger<TransactionService> logger)
        {
            _transactionRepo = transactionRepo;
            _logger = logger;
        }

        public async Task<AppResponse<DepositResponseDto>> DepositAsync(DepositDto depositDto, string userId)
        {
            _logger.LogInformation("Initiating deposit for user {userId} with amount {amount}", userId, depositDto.Amount);

            var response = await _transactionRepo.DepositAsync(depositDto, userId);

            if (response == null)
            {
                _logger.LogWarning("No response found for user {userId}", userId);
                return new AppResponse<DepositResponseDto>
                {
                    Succeeded = false,
                    Message = "No response from deposit operation",
                };
            }
            else
            {   
                _logger.LogInformation("Checking deposit response for user {userId}: Succeeded={succeeded}, Message={message}", userId, response.Succeeded, response.Message);
            if(response.Succeeded == false)
            {
                _logger.LogWarning("Deposit failed for user {userId}: {message}", userId, response.Message);
                return response;
            }
            return response;
        }}

        public async Task<AppResponse<List<TransactionDto>>> GetTransactionsByWalletIdAsync(Guid walletId, string userId)
        {
            _logger.LogInformation("Fetching transactions for wallet {walletId} and user {userId}", walletId, userId);

            var response = await _transactionRepo.GetTransactionsByWalletIdAsync(walletId, userId);
           
            if(response == null)
            {
                _logger.LogWarning("No transactions found for wallet {walletId} and user {userId}", walletId, userId);
                return new AppResponse<List<TransactionDto>>
                {
                    Succeeded = false,
                    Message = "No transactions found",
                    Data = null
                };
            }

            _logger.LogInformation("Received transactions response for wallet {walletId} and user {userId}: Succeeded={succeeded}, Message={message}", walletId, userId, response.Succeeded, response.Message);
            if(response.Succeeded == false)
            {
                _logger.LogWarning("Failed to fetch transactions for wallet {walletId} and user {userId}: {message}", walletId, userId, response.Message);
                return response;
            }

            return response;
        }

        public async Task<AppResponse<TransactionDto>> ScanToChargeWallet(BarcodeScanDto request, decimal amount, string userId, string pin)
        {
            _logger.LogInformation("Initiating scan to charge wallet for user {userId} with amount {amount}", userId, amount);

            var response = await _transactionRepo.ScanToChargeWalletAsync(request, amount, userId, pin);
            if (response == null)
            {
                _logger.LogWarning("No response found for user {userId}", userId);
                return new AppResponse<TransactionDto>
                {
                    Succeeded = false,
                    Message = "No response from scan to charge operation",
                };
            }
            else
            {
                _logger.LogInformation("Checking scan to charge response for user {userId}: Succeeded={succeeded}, Message={message}", userId, response.Succeeded, response.Message);
                if (response.Succeeded == false)
                {
                    _logger.LogWarning("Scan to charge failed for user {userId}: {message}", userId, response.Message);
                    return response;
                }
                return response;
            }
        }

        public async Task<AppResponse<TransactionDto>> TransferAsync(TransferDto transferDto, string userId)
        {
            _logger.LogInformation("Initiating transfer for user {userId} with amount {amount} to wallet {toWalletNumber}", userId, transferDto.Amount, transferDto.ReceiverWalletNumber);
            
            var response = await _transactionRepo.TransferAsync(transferDto, userId);
            if (response == null)
            {
                _logger.LogWarning("No response found for user {userId}", userId);
                return new AppResponse<TransactionDto>
                {
                    Succeeded = false,
                    Message = "No response from transfer operation",
                };
            }
            else
            {
                _logger.LogInformation("Checking transfer response for user {userId}: Succeeded={succeeded}, Message={message}", userId, response.Succeeded, response.Message);
                if (response.Succeeded == false)
                {
                    _logger.LogWarning("Transfer failed for user {userId}: {message}", userId, response.Message);
                    return response;
                }
                return response;
            }
        }

        public async Task<AppResponse<TransactionDto>> WithdrawAsync(WithdrawDto withdrawDto, string userId)
        {
            _logger.LogInformation("Initiating withdrawal for user {userId} with amount {amount}", userId, withdrawDto.Amount);
            var response = await _transactionRepo.WithdrawAsync(withdrawDto, userId);
           
            if (response == null)
            {
                _logger.LogWarning("No response found for user {userId}", userId);
                return new AppResponse<TransactionDto>
                {
                    Succeeded = false,
                    Message = "No response from withdrawal operation",
                };
            }
           
            else
            {
                _logger.LogInformation("Checking withdrawal response for user {userId}: Succeeded={succeeded}, Message={message}", userId, response.Succeeded, response.Message);
                if (response.Succeeded == false)
                {
                    _logger.LogWarning("Withdrawal failed for user {userId}: {message}", userId, response.Message);
                    return response;
                }
                return response;
            }
        }
    }
}
