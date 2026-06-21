using DigitalWalletCore.Dtos.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.School
{
    public class SchoolUserResponseDto
    {
        public string SchoolName { get; set; } = string.Empty;
        public string SchoolCode { get; set; } = string.Empty;
        public List<AppUserDto> Users { get; set; } = new List<AppUserDto>();
    }
}
