using DigitalWalletCore.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;
using SkiaSharp;
using System.Collections.Generic;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace DigitalWalletInfrastructure.Services
{
    public class QRCodeService : IQRCodeService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<QRCodeService> _logger;

        public QRCodeService(IConfiguration config, ILogger<QRCodeService> logger)
        {
            _config = config;
            _logger = logger;
        }

        [Obsolete]
        public async Task<byte[]> DownloadQRCodeAsync(string businessName, string qrCodeBase64, string walletNumber)
        {
            _logger.LogInformation("Starting QR code download process for business: {BusinessName}", businessName);
            const int width = 900;
            const int height = 1300;

            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);

            canvas.Clear(SKColors.White);

            using var borderPaint = new SKPaint
            {
                Color = SKColors.Green,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 5,
                IsAntialias = true
            };

            canvas.DrawRoundRect(
                new SKRoundRect(
                    new SKRect(20, 20, 880, 1280),
                    30,
                    30),
                borderPaint);

            var titleFont = new SKFont(SKTypeface.FromFamilyName( "Arial", SKFontStyle.Bold), 70);
            var walletFont = new SKFont(SKTypeface.Default, 48);
            var watermarkFont = new SKFont(SKTypeface.Default, 110);

            using var greenText = new SKPaint
            {
                Color = SKColors.DarkGreen,
                IsAntialias = true
            };

            _logger.LogInformation("Checking if logo file exists.");
            _logger.LogInformation("Current directory: {Directory}", Directory.GetCurrentDirectory());
            _logger.LogInformation("Logo file exists: {Exists}", File.Exists("logo.png"));

            using var logo = SKBitmap.Decode("Image/logo.png");

            _logger.LogInformation("Logo bitmap decoded: {Decoded}", logo != null);
            canvas.DrawBitmap(
            logo,
            new SKRect(250, 40, 650, 145));

            canvas.DrawText(
            businessName,
            450,
            210,
            SKTextAlign.Center,
            titleFont,
            greenText);

            var subtitleFont = new SKFont(SKTypeface.Default, 32);

            using var grayPaint = new SKPaint
            {
                Color = SKColors.Gray,
                IsAntialias = true
            };

            canvas.DrawText(
                "Scan to Pay",
                450,
                260,
                SKTextAlign.Center,
                subtitleFont,
                grayPaint);

            _logger.LogInformation("Decoding QR code from Base64 string.");
            byte[] qrBytes = Convert.FromBase64String(qrCodeBase64);

            _logger.LogInformation("QR code decoded: {Decoded}", qrBytes != null && qrBytes.Length > 0);
            using var shadowPaint = new SKPaint
            {
                IsAntialias = true,
                ImageFilter = SKImageFilter.CreateDropShadow(
                dx: 0,
                dy: 8,
                sigmaX: 12,
                sigmaY: 12,
                color: SKColors.Black.WithAlpha(6)
            )
            };

            using var qrBitmap = SKBitmap.Decode(qrBytes);

            using var qrBackground = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var qrBorder = new SKPaint
            {
                Color = SKColors.Green,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 4,
                IsAntialias = true
            };

            var qrRect = new SKRoundRect(
                new SKRect(180, 320, 720, 860),
                25,
                25);

            canvas.DrawRoundRect(qrRect, shadowPaint);            
            canvas.DrawRoundRect(qrRect, qrBackground);
            canvas.DrawRoundRect(qrRect, qrBorder);
            canvas.DrawBitmap(qrBitmap, new SKRect(220, 360, 680, 820));

            using var whitePaint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Fill
            };

            canvas.DrawRect(
                new SKRect(160, 900, 740, 1030),
                whitePaint);

            using var grayBorder = new SKPaint
            {
                Color = SKColors.Green,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 5
            };

            var walletRect = new SKRoundRect(
                new SKRect(160, 900, 740, 1030),
                25,
                25);

            using var walletShadow = new SKPaint
            {
                IsAntialias = true,
                ImageFilter = SKImageFilter.CreateDropShadow(
                    0,
                    6,
                    10,
                    10,
                    SKColors.Black.WithAlpha(10))
            };
            canvas.DrawRoundRect(walletRect, walletShadow);
            canvas.DrawRoundRect(walletRect, whitePaint);
            canvas.DrawRoundRect(walletRect, grayBorder);

            

            var labelFont = new SKFont(SKTypeface.Default, 24);

            canvas.DrawText(
                "Wallet Number",
                450,
                940,
                SKTextAlign.Center,
                labelFont,
                grayPaint);

            canvas.DrawText(
                walletNumber,
                450,
                995,
                SKTextAlign.Center,
                walletFont,
                greenText);

            using var watermarkPaint = new SKPaint
            {
                Color = SKColors.Green.WithAlpha(12),
                IsAntialias = true
            };

            canvas.DrawText(
                "CampusPay",
                80,
                1120,
                SKTextAlign.Left,
                watermarkFont,
                watermarkPaint);

            using var footerPaint = new SKPaint
            {
                Color = SKColor.Parse("#18A54A"),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            canvas.DrawRoundRect(
                new SKRoundRect(
                    new SKRect(25, 1150, 875, 1275),
                    0,
                    0),
                footerPaint);
            canvas.DrawText(
                "Open CampusPay and scan to pay",
                450,
                1080,
                SKTextAlign.Center,
                subtitleFont,
                grayPaint);


            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            return data.ToArray();
        }

        public async Task<string> GenerateQRCodeAsync(string data)
        {
            var base_url = _config["QRCode:BaseUrl"];
            // Create a Url payload
            var Payload = new PayloadGenerator.Url($"{base_url}?data={data}");

            // Generate the QR code data from the payload
            using var qrCodeData = QRCodeGenerator.GenerateQrCode(Payload);

            // Or override the ECC level
            using var qrCodeData2 = QRCodeGenerator.GenerateQrCode(Payload, QRCodeGenerator.ECCLevel.H);

            // Render the QR code
            using var pngRenderer = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = pngRenderer.GetGraphic(20);

            // Convert the byte array to a Base64 string
            return Convert.ToBase64String(qrCodeImage);
        }

        public async Task<string?> ScanBarcode(byte[] imageBytes)
        {            
           try
            {
                using var bitmap = SKBitmap.Decode(imageBytes);
                if (bitmap == null)
                {
                    _logger.LogWarning("Failed to decode barcode image bytes into a bitmap.");
                    return null;
                }

                var reader = new BarcodeReader()
                {
                    Options = new DecodingOptions
                    {
                        PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.CODE_128 },
                        TryHarder = true
                    }
                };

                // 3. Decode the barcode
                var result = reader.Decode(bitmap);
                return result?.Text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while scanning the barcode.");
                return null;
            }
        }
    }
}
