using DigitalWalletCore.Dtos.User;
using DigitalWalletCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletInfrastructure.Mapper
{
    public static class AccountMapper
    {
        public static RegisterResponseDto ToRegisterResponseDto(this RegisterRequestDto registerRequestDto, string message, string userId, string profilePicture)
        {
            return new RegisterResponseDto
            {
                UserId = userId ?? string.Empty,
                ProfilePicture = profilePicture,
                Firstname = registerRequestDto.Firstname,
                Lastname = registerRequestDto.Lastname,
                MatricNumber = registerRequestDto.MatricNumber,
                Email = registerRequestDto.Email,
                SchoolCode = registerRequestDto.SchoolCode,
                WalletNumber = registerRequestDto.MatricNumber,
                Message = message
            };
        }

        public static LoginResponseDto ToLoginResponseDto(this LoginRequestDto loginRequestDto, AppUser user, string token)
        {
            return new LoginResponseDto
            {
                Email =user.Email ?? string.Empty,
                ProfilePicture = user.ProfilePicture ?? string.Empty,
                Firstname = user.FirstName ?? string.Empty,
                Lastname = user.LastName ?? string.Empty,
                WalletNumber = loginRequestDto.WalletNumber,
                SchoolCode = user.School.Code,
                UserId = user.Id,
                Token = token
            };
        }

        public static AdminLoginResponseDto ToAdminLoginResponseDto(this AdminLoginRequestDto loginRequestDto, AppUser user, string token)
        {
            return new AdminLoginResponseDto
            {
                Email = user.Email ?? string.Empty,
                Firstname = user.FirstName ?? string.Empty,
                Lastname = user.LastName ?? string.Empty,
                UserId = user.Id,
                Token = token
            };
        }

        public static UserProfileResponseDto ToUserProfileResponseDto(this AppUser user)
        {
            return new UserProfileResponseDto
            {
                UserId = user.Id ?? string.Empty,
                ProfilePicture = user.ProfilePicture ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Firstname = user.FirstName ?? string.Empty,
                Lastname = user.LastName ?? string.Empty,
                MatricNumber = user.MatricNumber,
                SchoolCode = user.School.Code,
                SchoolName = user.School.Name,
                WalletNumber = user.Wallet.WalletNumber
            };
        }

        public static AppUserDto ToAppUserDto(this AppUser user)
        {
            return new AppUserDto
            {
                ProfilePicture = user.ProfilePicture ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                MatricNumber = user.MatricNumber,
                SchoolCode = user.School.Code,
                Merchant = user.Merchant != null ? new DigitalWalletCore.Dtos.Merchant.MerchantDto
                {
                    MerchantId = user.Merchant.Id,
                    Email = user.Email ?? string.Empty,
                    Firstname = user.FirstName ?? string.Empty,
                    Lastname = user.LastName ?? string.Empty,
                    AccountName = user.Merchant.AccountName ?? string.Empty,
                    AccountNumber = user.Merchant.AccountNumber ?? string.Empty,
                    BusinessName = user.Merchant.BusinessName ?? string.Empty,
                    ShopLocation = user.Merchant.ShopLocation ?? string.Empty,
                    IsApproved = user.Merchant.IsApproved,
                    SchoolCode = user.School.Code,
                    WalletNumber = user.Wallet.WalletNumber
                } : null,
            };
        }
}}
