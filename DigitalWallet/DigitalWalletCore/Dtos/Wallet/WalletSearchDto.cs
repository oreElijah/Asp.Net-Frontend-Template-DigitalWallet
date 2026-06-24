using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.Wallet
{
    public class WalletSearchDto
    {
        public string WalletNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string SchoolCode { get; set; } = string.Empty;
    }
}
