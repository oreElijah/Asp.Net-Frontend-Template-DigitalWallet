using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Entities
{
    public class School
    {
        public Guid Id { get; set; } 

        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public List<AppUser> Users { get; set; } = new List<AppUser>();
    }
}
