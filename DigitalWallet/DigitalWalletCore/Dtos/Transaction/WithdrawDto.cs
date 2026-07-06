using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace DigitalWalletCore.Dtos.Transaction
{
    public class WithdrawDto
    {
        public decimal Amount { get; set; }
        
        [Required]
        [MinLength(4, ErrorMessage = "Pin must be at least 4 digits long.")]
        [MaxLength(4, ErrorMessage = "Pin cannot be more than 4 digits long")]
        public string Pin { get; set; }
    }
}
