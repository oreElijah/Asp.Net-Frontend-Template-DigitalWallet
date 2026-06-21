using DigitalWalletCore.Dtos.Wallet;
using DigitalWalletCore.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.Transaction
{
    public class TransactionDto
    {
        public Guid Id { get; set; }

        public string Reference { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string? Description { get; set; } = string.Empty;

        public TransactionType Type { get; set; }

        public TransactionStatus Status { get; set; }

        public string? SenderWalletNumber { get; set; }

        public string ? SenderWalletName { get; set; }

        public string? ReceiverWalletName { get; set; }

        public string? ReceiverWalletNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
