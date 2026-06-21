using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalWalletCore.Dtos.User
{
    public class CreateSchoolAdminResponseDto
    {
        public string Firstname { get; set; }

        public string Lastname { get; set; }

        public string SchoolCode { get; set; }

        public string Email { get; set; }

        public DateTime CreatedAt { get; set; }

    }
}
