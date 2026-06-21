using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.Transaction
{
    public class TransferDto
    {
        public string ReceiverWalletNumber { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}
