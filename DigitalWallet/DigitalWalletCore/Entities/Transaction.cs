
using DigitalWalletCore.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Entities
{
    public class Transaction
    {
        public Guid Id { get; set; }

        public string Reference { get; set; } = string.Empty;   

        public decimal Amount { get; set; }

        public string Description { get; set; } = string.Empty;

        public TransactionType Type { get; set; }

        public TransactionStatus Status { get; set; }

        public string SenderWalletNumber { get; set; } = string.Empty;

        public Guid? SenderWalletId { get; set; }

        public Wallet? SenderWallet { get; set; }

        public string ReceiverWalletNumber { get; set; } = string.Empty;

        public Guid? ReceiverWalletId { get; set; }

        public Wallet? ReceiverWallet { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
