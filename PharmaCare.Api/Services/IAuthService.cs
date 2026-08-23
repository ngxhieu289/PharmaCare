using PharmaCare.Api.DTOs.Auth;

namespace PharmaCare.Api.Services;

public interface IAuthService
{
    Task<TokenResponse?> LoginAsync(LoginRequest request, string ipAddress);
    Task<TokenResponse?> RegisterAsync(RegisterRequest request, string ipAddress);
    Task<TokenResponse?> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task RevokeTokenAsync(string refreshToken, string ipAddress);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
}
