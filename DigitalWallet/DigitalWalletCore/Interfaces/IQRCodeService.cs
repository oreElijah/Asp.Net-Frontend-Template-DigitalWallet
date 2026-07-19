using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Interfaces
{
    public interface IQRCodeService
    {
        public Task<string> GenerateQRCodeAsync(string data);

        public Task<byte[]> DownloadQRCodeAsync(string BusinessName, string Qrcode, string walletNumber);

        public Task<string> ScanBarcode(byte[] imageBytes);
    }
}
