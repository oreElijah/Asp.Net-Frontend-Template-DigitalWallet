using DigitalWalletCore.Dtos.Transaction;
using DigitalWalletCore.Dtos.User;
using DigitalWalletCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.Wallet
{
    public class WalletResponseDto
    {
        public Guid Id { get; set; }

        public string WalletNumber { get; set; } = string.Empty;

        public decimal Balance { get; set; } = decimal.Zero;

        public bool IsLocked { get; set; } = false;

        public string UserId { get; set; } = string.Empty;

        public AppUserDto User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime LastUpdatedAt { get; set; }

        public List<TransactionDto> SentTransactions { get; set; } = new List<TransactionDto>();

        public List<TransactionDto> ReceivedTransactions { get; set; } = new List<TransactionDto>();
    }
}
