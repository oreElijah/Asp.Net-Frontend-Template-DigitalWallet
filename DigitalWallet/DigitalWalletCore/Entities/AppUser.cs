using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Entities
{
    public class AppUser : Microsoft.AspNetCore.Identity.IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;    

        public string? MatricNumber { get; set; } = string.Empty;

        public string ProfilePicture { get; set; } = string.Empty;

        public string SchoolCode { get; set; } = string.Empty;

        public School School { get; set; } = new School();

        public Merchant? Merchant { get; set; }

        public Wallet? Wallet { get; set; } = new Wallet();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeactivated { get; set; }

        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
