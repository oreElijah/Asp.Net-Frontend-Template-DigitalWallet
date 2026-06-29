using DigitalWalletCore.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;
using SkiaSharp;


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

            var titleFont = new SKFont(SKTypeface.Default, 42);
            var walletFont = new SKFont(SKTypeface.Default, 32);
            var watermarkFont = new SKFont(SKTypeface.Default, 90);

            using var greenText = new SKPaint
            {
                Color = SKColors.DarkGreen,
                IsAntialias = true
            };

            _logger.LogInformation("Checking if logo file exists.");
            _logger.LogInformation("Current directory: {Directory}", Directory.GetCurrentDirectory());
            _logger.LogInformation("Logo file exists: {Exists}", File.Exists("logo.png"));

            using var logo = SKBitmap.Decode("logo.png");

            _logger.LogInformation("Logo bitmap decoded: {Decoded}", logo != null);
            canvas.DrawBitmap(
                logo,
                new SKRect(330, 40, 570, 120));

            canvas.DrawText(
                businessName,
                180,
                170,
                SKTextAlign.Left,
                titleFont,
                greenText);

            _logger.LogInformation("Decoding QR code from Base64 string.");
            byte[] qrBytes = Convert.FromBase64String(qrCodeBase64);

            _logger.LogInformation("QR code decoded: {Decoded}", qrBytes != null && qrBytes.Length > 0);
            using var qrBitmap = SKBitmap.Decode(qrBytes);

            canvas.DrawBitmap(
                qrBitmap,
                new SKRect(225, 250, 675, 700));

            using var whitePaint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Fill
            };

            canvas.DrawRect(
                new SKRect(150, 760, 750, 880),
                whitePaint);

            using var grayBorder = new SKPaint
            {
                Color = SKColors.LightGray,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            canvas.DrawRect(
                new SKRect(150, 760, 750, 880),
                grayBorder);

            canvas.DrawText(
                walletNumber,
                450,
                835,
                SKTextAlign.Center,
                walletFont,
                greenText);

            using var watermarkPaint = new SKPaint
            {
                Color = SKColors.Green.WithAlpha(20),
                IsAntialias = true
            };

            canvas.DrawText(
                "CampusPay",
                90,
                1030,
                SKTextAlign.Left,
                watermarkFont,
                watermarkPaint);

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
    }
}
