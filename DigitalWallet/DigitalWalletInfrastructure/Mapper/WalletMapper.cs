using DigitalWalletCore.Dtos.Wallet;
using DigitalWalletCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletInfrastructure.Mapper
{
    public static class WalletMapper
    {
        public static WalletDto ToWalletDto(this Wallet wallet)
        {
            return new WalletDto
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                Balance = wallet.Balance,
                IsLocked = wallet.IsLocked,
                LastUpdatedAt = wallet.LastUpdatedAt,
                WalletNumber = wallet.WalletNumber,
                CreatedAt = wallet.CreatedAt
            };
        }

        public static WalletResponseDto ToWalletResponseDto(this Wallet wallet)
        {
            return new WalletResponseDto
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                Balance = wallet.Balance,
                IsLocked = wallet.IsLocked,
                LastUpdatedAt = wallet.LastUpdatedAt,
                WalletNumber = wallet.WalletNumber,
                CreatedAt = wallet.CreatedAt,
                ReceivedTransactions = wallet.ReceivedTransactions.ToTransactionResponseDtoList(),
                SentTransactions = wallet.SentTransactions.ToTransactionResponseDtoList(),
            };
        }

        public static WalletSearchDto ToWalletSearchDto(this Wallet wallet)
        {
            return new WalletSearchDto
            {
               WalletNumber = wallet.WalletNumber,
               FirstName = wallet.User.FirstName,
               LastName = wallet.User.LastName
            };
        }

        public static Wallet ToWallet(this WalletDto walletDto)
        {
            return new Wallet
            {
                Id = walletDto.Id,
                UserId = walletDto.UserId,
                Balance = walletDto.Balance,
                IsLocked = walletDto.IsLocked,
                LastUpdatedAt = walletDto.LastUpdatedAt,
                WalletNumber = walletDto.WalletNumber,
                CreatedAt = walletDto.CreatedAt
            };
        }
}}
