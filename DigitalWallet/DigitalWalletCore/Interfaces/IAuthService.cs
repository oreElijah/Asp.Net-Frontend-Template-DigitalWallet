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
        public Task<TokenResponseDto?> RefreshTokenAsync(string refreshToken);
        public Task RevokeUserRefreshTokensAsync(string userId);
        public Task LoginWithGoogleAsync(ClaimsPrincipal claimsPrincipal, HttpContext context);
        public Task Logout(string jti);
        public Task<AppUser> FindUserByWalletNumberAsync(string walletNumber);
        public Task<School> GetSchoolByCodeAsync(string schoolCode);
        public Task<bool> ApproveMerchantAsync(Guid merchantId, string schoolCode, string actorUserId);
        public Task<bool> RejectMerchantAsync(Guid merchantId, string schoolCode, string actorUserId);
        public Task<CreateSchoolAdminResponseDto> CreateSchoolAdminAsync(CreateSchoolAdminDto createSchoolAdminDto);
        public Task<List<string>> GetSchoolAdminByCodeAsync(string schoolCode);
    }
}
