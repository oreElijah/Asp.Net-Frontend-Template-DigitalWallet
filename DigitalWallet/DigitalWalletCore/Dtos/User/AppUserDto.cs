using DigitalWalletCore.Dtos.Merchant;
using DigitalWalletCore.Dtos.School;
using DigitalWalletCore.Dtos.Wallet;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.User
{
    public class AppUserDto
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? MatricNumber { get; set; } = string.Empty;

        public string SchoolCode { get; set; }

        public SchoolDto School { get; set; } = new SchoolDto();

        public MerchantDto? Merchant { get; set; }

        public WalletDto Wallet { get; set; } = new WalletDto();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
