using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalWalletCore.Dtos.User
{
    public class CreateSchoolAdminDto
    {
        public string Firstname { get; set; }

        [Required]
        public string Lastname { get; set; }

        [Required]
        public string SchoolCode { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
