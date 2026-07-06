using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalWalletCore.Dtos.User
{
    public class RegisterRequestDto
    {
        [Required]
        public string Firstname { get; set; }

        [Required]
        public string Lastname { get; set; }

        public IFormFile ProfilePicture { get; set; }


        [Required]
        public string MatricNumber { get; set; }

        [Required]
        public string SchoolCode { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(4, ErrorMessage = "Pin must be at least 4 digits long.")]
        [MaxLength(4, ErrorMessage = "Pin cannot be more than 4 digits long")] 
        public string Pin { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
