using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Wallet;
using DigitalWalletCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Interfaces
{
    public interface IWalletRepository
    {
        public Task<AppResponse<Wallet>> CreateStudentWallet(string matricNumber, string userId);

        public Task<AppResponse<Wallet>> CreateMerchantWallet(string userId);

        public Task<AppResponse<WalletDto>> GetWalletById(Guid walletId, string userId);

        public Task<AppResponse<WalletSearchDto>> GetWalletByWalletNumber(string walletNumber);

        public Task<AppResponse<bool>> DeleteWalletById(Guid walletId, string userId);

        public Task<AppResponse<Decimal>> GetWalletBalance(Guid walletId, string userId);
        //public Task<AppResponse<bool>> UpdateWalletBalance(Guid walletId);

        public Task<AppResponse<WalletDto>> GetWalletByUserId(string userId);

        public Task<AppResponse<Wallet>> WalletExists(Guid walletId, string userId);

        public Task<AppResponse<WalletSearchDto>> WalletExistsByWalletNumber(string walletNumber);

        public Task<string> GenerateMerchantWalletNumberAsync();

        public Task<AppResponse<bool>> LockWalletAsync(string walletNumber);

        public Task<AppResponse<WalletResponseDto>> GetWalletDetailsById(Guid walletId, string userId);
    }
}
