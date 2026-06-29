using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.Merchant
{
    public class UpdateMerchantProfileDto
    {
        public string Firstname { get; set; } = string.Empty;

        public string Lastname { get; set; } = string.Empty;

        public IFormFile ProfilePicture { get; set; }

        public string ShopLocation { get; set; }

        public string BankCode { get; set; }

        public string AccountNumber { get; set; }

    }
}
