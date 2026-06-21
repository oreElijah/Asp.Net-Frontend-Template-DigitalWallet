using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Exceptions
{
    public class ExternalProviderException : Exception
    {
        public ExternalProviderException(string provider, string message)
        : base($"External login provider: {provider} error occured: {message}")
        {

        }
    }
}
