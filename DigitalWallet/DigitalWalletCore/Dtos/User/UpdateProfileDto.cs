using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalWalletCore.Dtos.User
{
    public class UpdateProfileDto
    {
        [Required]
        public string Firstname { get; set; }

        public IFormFile ProfilePicture { get; set; }

        [Required]
        public string Lastname { get; set; }

    }
}
