using DigitalWalletCore.Dtos.Transaction;
using DigitalWalletCore.Dtos.User;
using DigitalWalletCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.Wallet
{
    public class WalletDto
    {
        public Guid Id { get; set; }

        public string WalletNumber { get; set; } = string.Empty;

        public decimal Balance { get; set; } = decimal.Zero;

        public bool IsLocked { get; set; } = false;

        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastUpdatedAt { get; set; }

    }
}
