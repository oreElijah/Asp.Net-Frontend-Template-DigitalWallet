using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Options
{
    public class ConnectionStringOptions
    {
        public const string SectionName = "ConnectionStrings";

        public string DefaultConnection { get; set; } = string.Empty;
    }
}
