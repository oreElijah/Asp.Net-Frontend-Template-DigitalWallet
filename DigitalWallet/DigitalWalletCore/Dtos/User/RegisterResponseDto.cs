using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.User
{
    public class RegisterResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string ProfilePicture { get; set; } = string.Empty;
        public string Firstname { get; set; } = string.Empty;
        public string Lastname { get; set; } = string.Empty;
        public string MatricNumber { get; set; } = string.Empty;
        public string SchoolCode { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string WalletNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
