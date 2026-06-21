using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {

        }
    }
}
