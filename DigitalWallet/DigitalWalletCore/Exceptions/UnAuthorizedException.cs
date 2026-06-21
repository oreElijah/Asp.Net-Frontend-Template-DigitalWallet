using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Exceptions
{
    public class UnAuthorizedException : Exception
    {
        public UnAuthorizedException(string message) : base(message)
        {

        }
    }
}
