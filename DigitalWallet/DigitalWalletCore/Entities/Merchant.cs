using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Entities
{
    public class Merchant
    {
        public Guid Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public AppUser User { get; set; } = null!;

        public string BusinessName { get; set; } = string.Empty;

        public string ShopLocation { get; set; } = string.Empty;

        public bool IsApproved { get; set; } = false;

        public string AccountNumber { get; set; } = string.Empty;   

        public string AccountName { get; set; } = string.Empty;

        public string BankCode { get; set; } = string.Empty;
        
        public string BankName { get; set; } = string.Empty;

        public string TransferRecipientCode { get; set; } = string.Empty;
        
        public string QRCodeString { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
