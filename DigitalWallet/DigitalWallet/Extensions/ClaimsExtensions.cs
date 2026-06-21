using DigitalWalletCore.Exceptions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DigitalWalletApi.Extensions
{
    public static class ClaimsExtension
    {
        public static string GetUsername(this ClaimsPrincipal user)
        {
            var username = user.FindFirst(JwtRegisteredClaimNames.Name)?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst(ClaimTypes.GivenName)?.Value;

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new UnAuthorizedException("Username claim is missing from token.");
            }

            return username;
        }

        public static string GetJti(this ClaimsPrincipal user)
        {
            var jti = user.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (string.IsNullOrWhiteSpace(jti))
            {
                throw new UnAuthorizedException("JTI claim is missing from token.");
            }

            return jti;
        }

        public static string GetUserId(this ClaimsPrincipal user)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnAuthorizedException("UserId claim is missing from token.");
            }

            return userId;
        }

        public static string GetEmail(this ClaimsPrincipal user)
        {
            var email = user.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new UnAuthorizedException("Email claim is missing from token.");
            }

            return email;
        }
    }
}
