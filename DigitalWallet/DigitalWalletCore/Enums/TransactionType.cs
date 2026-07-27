using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Enums
{
    public enum TransactionType
    {
        Deposit = 0,
        Transfer = 1,
        Withdrawal = 2,
        QRPayment = 3,
        ScantoPay = 4
    }
}
