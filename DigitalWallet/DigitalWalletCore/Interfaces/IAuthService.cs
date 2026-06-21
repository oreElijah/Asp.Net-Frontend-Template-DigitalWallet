using DigitalWalletCore.Dtos.User;
using DigitalWalletCore.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace DigitalWalletCore.Interfaces
{
    public interface IAuthService
    {
        public Task<string> CreateToken(AppUser user);
        public Task<string> CreateRefreshToken(AppUser user);
        public Task LoginWithGoogleAsync(ClaimsPrincipal claimsPrincipal, HttpContext context);
        public Task Logout(string jti);
        public Task<AppUser> FindUserByWalletNumberAsync(string walletNumber);
        public Task<School> GetSchoolByCodeAsync(string schoolCode);
        public Task<bool> ApproveMerchantAsync(Guid merchantId);
        public Task<bool> RejectMerchantAsync(Guid merchantId);
        public Task<CreateSchoolAdminResponseDto> CreateSchoolAdminAsync(CreateSchoolAdminDto createSchoolAdminDto);
    }
}
