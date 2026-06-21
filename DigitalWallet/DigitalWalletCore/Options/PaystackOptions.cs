using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Options
{
    public class PaystackOptions
    {
        public const string SectionName = "PayStack";

        public string SecretKey { get; set; } = string.Empty;
    }
}
