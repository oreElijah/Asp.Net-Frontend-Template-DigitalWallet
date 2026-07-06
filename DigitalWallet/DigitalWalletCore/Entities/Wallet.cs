using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Entities
{
    public class Wallet
    {
        public Guid Id { get; set; }

        public string WalletNumber { get; set; } = string.Empty;

        public decimal Balance { get; set; } = decimal.Zero;

        public decimal LockedBalance { get; set; } = decimal.Zero;

        public bool IsLocked { get; set; } = false;

        public string? Pin { get; set; } = "0000";

        public string UserId { get; set; } = string.Empty;  

        public AppUser User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastUpdatedAt { get; set; }

        public List<Transaction> SentTransactions { get; set; } = new List<Transaction>();

        public List<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
    }
}
