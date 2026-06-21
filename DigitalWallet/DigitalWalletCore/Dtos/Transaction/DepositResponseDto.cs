using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.Transaction
{
    public class DepositResponseDto
    {
        public TransactionDto Transaction { get; set; }
        public decimal Amount { get; set; }
        public string PaymentUrl { get; set; } = string.Empty;
        public string PaymentReference { get; set; } = string.Empty;
    }
}
