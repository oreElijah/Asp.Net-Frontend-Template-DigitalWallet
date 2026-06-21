using Amazon.S3;
using Amazon.S3.Model;
using DigitalWalletCore.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletInfrastructure.Services
{
    public class BackBlazeStorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3;
        private readonly IConfiguration _config;

        public BackBlazeStorageService(IAmazonS3 s3, IConfiguration config)
        {
            _s3 = s3;
            _config = config;
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            using var stream = file.OpenReadStream();

            var request = new PutObjectRequest
            {
                BucketName = _config["Backblaze:BucketName"],
                Key = fileName,
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _s3.PutObjectAsync(request);

            var ReturnUrl = _config["Backblaze:ReturnUrl"];

            return $"{ReturnUrl}/{fileName}";
        }
    }
}
