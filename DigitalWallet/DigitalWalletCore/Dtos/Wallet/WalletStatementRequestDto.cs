using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalWalletCore.Enums;

namespace DigitalWalletCore.Dtos.Wallet
{
    public class WalletStatementRequestDto
    {
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public TransactionType? Type { get; set; }

        public TransactionStatus? Status { get; set; }

        public decimal? MinAmount { get; set; }

        public decimal? MaxAmount { get; set; }

        public string Search { get; set; } = string.Empty; 
    }
}