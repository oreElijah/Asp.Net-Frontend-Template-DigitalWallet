using DigitalWalletCore.Dtos.User;
using DigitalWalletCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.Merchant
{
    public class MerchantProfileResponseDto
    {
        public Guid MerchantId { get; set; }

        public string Firstname { get; set; } = string.Empty;

        public string Lastname { get; set; } = string.Empty;

        public string SchoolCode { get; set; } = string.Empty;

        public string SchoolName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string WalletNumber { get; set; }

        public string BusinessName { get; set; }

        public string ShopLocation { get; set; }

        public bool IsApproved { get; set; }

        public string AccountNumber { get; set; }

        public string AccountName { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
    }
}
