using System.ComponentModel.DataAnnotations;

namespace DigitalWalletCore.Dtos.User;

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
