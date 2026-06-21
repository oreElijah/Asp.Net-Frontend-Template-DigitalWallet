using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.User
{
    public class AdminLoginResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Firstname { get; set; } = string.Empty;
        public string Lastname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
