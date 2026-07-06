using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace DigitalWalletCore.Dtos.Merchant
{
    public class RegisterMerchantRequestDto
    {

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public IFormFile ProfilePicture { get; set; }

        [Required]
        public string BusinessName { get; set; }

        [Required]
        public string ShopLocation { get; set; }

        [Required]
        public string SchoolCode { get; set; }

        [Required]
        [MinLength(4, ErrorMessage = "Pin must be at least 4 digits long.")]
        [MaxLength(4, ErrorMessage = "Pin cannot be more than 4 digits long")]
        public string Pin { get; set; }

        [Required]
        public string BankCode { get; set; }

        [Required]
        public string AccountNumber { get; set; }

    }
}
