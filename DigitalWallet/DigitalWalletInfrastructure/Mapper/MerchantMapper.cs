using DigitalWalletCore.Dtos.Merchant;
using DigitalWalletCore.Dtos.User;
using DigitalWalletCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletInfrastructure.Mapper
{
    public static class MerchantMapper
    {
        public static RegisterMerchantResponseDto ToMerchantRegisterResponseDto(this RegisterMerchantRequestDto registerRequestDto, Guid MerchantId, string WalletNumber, string AccountName, string BankName, string message)
        {
            return new RegisterMerchantResponseDto
            {
                MerchantId = MerchantId,
                Firstname = registerRequestDto.BusinessName,
                Lastname = registerRequestDto.BusinessName,
                Email = registerRequestDto.Email,
                SchoolCode = registerRequestDto.SchoolCode,
                WalletNumber = WalletNumber,
                AccountName = AccountName,
                AccountNumber = registerRequestDto.AccountNumber,
                BusinessName = registerRequestDto.BusinessName,
                BankName = BankName,
                IsApproved = false,
                ShopLocation = registerRequestDto.ShopLocation,
                Message = message
            };
        }

        public static MerchantProfileResponseDto ToMerchantProfileResponseDto(this AppUser user, Guid MerchantId)
        {
            return new MerchantProfileResponseDto
            {
                MerchantId = MerchantId,
                Email = user.Email ?? string.Empty,
                Firstname = user.FirstName ?? string.Empty,
                Lastname = user.LastName ?? string.Empty,
                AccountName = user.Merchant.AccountName ?? string.Empty,
                AccountNumber = user.Merchant.AccountNumber ?? string.Empty,
                BusinessName = user.Merchant.BusinessName ?? string.Empty,
                ShopLocation = user.Merchant.ShopLocation ?? string.Empty,
                BankName = user.Merchant.BankName ?? string.Empty,
                IsApproved = user.Merchant.IsApproved,                
                SchoolCode = user.School.Code,
                WalletNumber = user.Wallet.WalletNumber
            };
        }
    }
}
