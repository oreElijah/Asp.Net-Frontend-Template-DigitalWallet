using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalWalletCore.Enums;


namespace DigitalWalletCore.Dtos.Transaction
{
    public class TransactionStatementDto
    {
        public Guid Id { get; set; }

        public string Reference { get; set; }

        public string Description { get; set; }

        public decimal Amount { get; set; }

        public TransactionType Type { get; set; }

        public TransactionStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsCredit { get; set; }

        public string SenderName { get; set; }

        public string SenderWallet { get; set; }

        public decimal? BalanceBefore { get; set; }
        
        public decimal? BalanceAfter { get; set; }
    }
}