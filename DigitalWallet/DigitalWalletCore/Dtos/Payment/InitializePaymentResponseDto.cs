using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.Payment
{
    public class InitializePaymentResponseDto
    {
        public string AuthorizationUrl { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }
}
