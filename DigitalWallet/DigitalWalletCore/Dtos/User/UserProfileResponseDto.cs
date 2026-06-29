using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.User
{
    public class UserProfileResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string ProfilePicture { get; set; } = string.Empty;
        public string Firstname { get; set; } = string.Empty;
        public string Lastname { get; set; } = string.Empty;
        public string MatricNumber { get; set; } = string.Empty;
        public string SchoolCode { get; set; } = string.Empty;
        public string SchoolName {get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string WalletNumber { get; set; }
    }
}
