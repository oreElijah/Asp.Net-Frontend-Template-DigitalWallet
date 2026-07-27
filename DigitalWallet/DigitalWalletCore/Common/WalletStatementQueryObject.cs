using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalWalletCore.Enums;


namespace DigitalWalletCore.Common
{
    public class WalletStatementQueryObject
    {
        public DateTime StartDate { get; set; } = DateTime.MinValue;
        public DateTime EndDate { get; set; } = DateTime.UtcNow;
        public TransactionType type { get; set; } = TransactionType.Transfer;
        public TransactionStatus status { get; set; } = TransactionStatus.Successful;
        public decimal MinAmount { get; set; } = 0;
        public decimal MaxAmount { get; set; } = decimal.MaxValue;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}