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
        private readonly IAmazonS3 _amazonS3;
        private readonly IConfiguration _configuration;

        public BackBlazeStorageService(IAmazonS3 amazonS3, IConfiguration configuration)
        {
            _amazonS3 = amazonS3;
            _configuration = configuration;
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";

            using var stream = file.OpenReadStream();

            var request = new PutObjectRequest
            {
                BucketName = _configuration["AWS:BucketName"],
                Key = fileName,
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _amazonS3.PutObjectAsync(request);
            
            var ReturnUrl = _configuration["Backblaze:ReturnUrl"];

            var fileUrl = $"{ReturnUrl}/{fileName}";

            return fileUrl;
        }
    }
}
