using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalWalletCore.Dtos.Merchant
{
    public class RegisterMerchantRequestDto
    {

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string BusinessName { get; set; }

        [Required]
        public string ShopLocation { get; set; }

        [Required]
        public string SchoolCode { get; set; }

        [Required]
        public string BankCode { get; set; }

        [Required]
        public string AccountNumber { get; set; }

    }
}
