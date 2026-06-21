using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace DigitalWalletCore.Entities
{
    public class AppUser : Microsoft.AspNetCore.Identity.IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;    

        public string? MatricNumber { get; set; } = string.Empty;

        public string SchoolCode { get; set; } = string.Empty;

        public School School { get; set; } = new School();

        public Merchant? Merchant { get; set; }

        public Wallet? Wallet { get; set; } = new Wallet();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
