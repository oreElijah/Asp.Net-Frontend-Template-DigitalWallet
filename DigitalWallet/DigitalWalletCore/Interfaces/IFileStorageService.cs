using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Interfaces
{
    public interface IFileStorageService
    {
        public Task<string> UploadFileAsync(IFormFile file);
    }
}
