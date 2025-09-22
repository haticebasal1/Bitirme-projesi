using System;

namespace PhoneCase.Shared.Dtos.AuthDtos;

public class TokenDto
{
    public string? AccessToken { get; set; }
    public DateTime AccessTokenExpretionDate { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiration { get; set; }
    public DateTimeOffset? AccessTokenExpirationDate { get; set; }
}
