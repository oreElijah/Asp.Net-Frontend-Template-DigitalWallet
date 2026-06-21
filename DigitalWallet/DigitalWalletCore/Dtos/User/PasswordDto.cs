using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalWalletCore.Dtos.User
{
    public class PasswordDto
    {
        [Required]
        public string Password { get; set; }
    }
}
