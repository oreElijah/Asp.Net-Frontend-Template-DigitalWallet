using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalWalletCore.Dtos.User
{
    public class EmailDto
    {
        [Required]
        public string Email { get; set; }
    }
}
