using PharmaCare.Api.Entities;

namespace PharmaCare.Api.Services;

public interface ITokenService
{
    TokenResponseData CreateAccessToken(
        User user,
        IEnumerable<string> roles,
        IEnumerable<string> permissions);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}

public record TokenResponseData(string Token, DateTime ExpiresAt);
