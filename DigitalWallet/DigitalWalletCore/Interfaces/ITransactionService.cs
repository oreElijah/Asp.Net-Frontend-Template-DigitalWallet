using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Transaction;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Interfaces
{
    public interface ITransactionService
    {
        public Task<AppResponse<DepositResponseDto>> DepositAsync(DepositDto depositDto, string userId);

        public Task<AppResponse<TransactionDto>> WithdrawAsync(WithdrawDto withdrawDto, string userId);

        public Task<AppResponse<TransactionDto>> TransferAsync(TransferDto transferDto, string userId);

        public Task<AppResponse<List<TransactionDto>>> GetTransactionsByWalletIdAsync(Guid walletId, string userId);
    }
}
