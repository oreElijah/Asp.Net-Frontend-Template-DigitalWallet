using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Exceptions
{
    public class AlreadyExistsException : Exception
    {
        public AlreadyExistsException(string message) : base(message)
        {

        }
    }
}
