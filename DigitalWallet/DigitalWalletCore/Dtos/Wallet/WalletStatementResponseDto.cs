using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalWalletCore.Dtos.Transaction;

namespace DigitalWalletCore.Dtos.Wallet
{
    public class WalletStatementResponseDto
    {
        public decimal CurrentBalance { get; set; }

        public decimal TotalCredits { get; set; }

        public decimal TotalDebits { get; set; }

        public int TotalTransactions { get; set; }
        
        public int TotalPages { get; set; }

        public IEnumerable<TransactionStatementDto> Transactions { get; set; }
    }
}