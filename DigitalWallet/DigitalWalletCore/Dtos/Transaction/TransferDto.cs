using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace DigitalWalletCore.Dtos.Transaction
{
    public class TransferDto
    {
        public string ReceiverWalletNumber { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        
        [Required]
        [MinLength(4, ErrorMessage = "Pin must be at least 4 digits long.")]
        [MaxLength(4, ErrorMessage = "Pin cannot be more than 4 digits long")]
        public string Pin { get; set; }
    }
}
