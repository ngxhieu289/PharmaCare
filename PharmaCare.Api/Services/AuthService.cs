using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PharmaCare.Api.Data;
using PharmaCare.Api.DTOs.Auth;
using PharmaCare.Api.Entities;

namespace PharmaCare.Api.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUserSessionService _userSessionService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        AppDbContext context,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        IUserSessionService userSessionService,
        IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _userSessionService = userSessionService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<TokenResponse?> LoginAsync(LoginRequest request, string ipAddress)
    {
        var identifier = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .SingleOrDefaultAsync(u => u.Email == identifier || u.Username == identifier);

        if (user is null || !user.IsActive || user.IsGuest)
        {
            return null;
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        }

        var roles = GetRoles(user);
        var permissions = GetPermissions(user);
        return await IssueTokensAsync(user, roles, permissions, ipAddress);
    }

    public async Task<TokenResponse?> RegisterAsync(RegisterRequest request, string ipAddress)
    {
        var email = NormalizeEmail(request.Email);
        var username = request.Username.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(u => u.Email == email || u.Username == username))
        {
            return null;
        }

        var customerRole = await _context.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .SingleOrDefaultAsync(r => r.Name == "Customer");
        if (customerRole is null)
        {
            throw new InvalidOperationException("Customer role has not been initialized.");
        }

        var user = new User
        {
            Email = email,
            Username = username,
            DisplayName = request.DisplayName.Trim(),
            Phone = request.Phone?.Trim()
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        user.UserRoles.Add(new UserRole { User = user, Role = customerRole });

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var permissions = customerRole.RolePermissions.Select(rp => rp.Permission.Code).ToArray();
        return await IssueTokensAsync(user, [customerRole.Name], permissions, ipAddress);
    }

    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        var tokenHash = _tokenService.HashRefreshToken(refreshToken);
        var storedToken = await _context.RefreshTokens
            .AsSplitQuery()
            .Include(t => t.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (storedToken is null || !storedToken.User.IsActive)
        {
            return null;
        }

        // Một refresh token đã được rotate mà xuất hiện lần nữa là dấu hiệu bị phát lại.
        // Thu hồi toàn bộ phiên của người dùng để chặn cả token thay thế đã bị đánh cắp.
        if (!storedToken.IsActive)
        {
            if (storedToken.RevokedAt.HasValue &&
                !string.IsNullOrWhiteSpace(storedToken.ReplacedByTokenHash))
            {
                await _userSessionService.InvalidateUserAsync(storedToken.User, ipAddress);
                await _context.SaveChangesAsync();
            }
            return null;
        }

        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var replacementHash = _tokenService.HashRefreshToken(newRefreshToken);
        var now = DateTimeOffset.UtcNow;
        var rotated = await _context.RefreshTokens
            .Where(token => token.Id == storedToken.Id &&
                            token.RevokedAt == null &&
                            token.ExpiresAt > now)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(token => token.RevokedAt, now)
                .SetProperty(token => token.RevokedByIp, ipAddress)
                .SetProperty(token => token.ReplacedByTokenHash, replacementHash));

        // Chỉ một request được phép rotate một refresh token. Request đồng thời
        // hoặc phát lại sẽ làm mất hiệu lực toàn bộ phiên của tài khoản.
        if (rotated != 1)
        {
            await _userSessionService.InvalidateUserAsync(storedToken.User, ipAddress);
            await _context.SaveChangesAsync();
            return null;
        }

        var roles = GetRoles(storedToken.User);
        var permissions = GetPermissions(storedToken.User);
        return await IssueTokensAsync(storedToken.User, roles, permissions, ipAddress, newRefreshToken);
    }

    public async Task RevokeTokenAsync(string refreshToken, string ipAddress)
    {
        var tokenHash = _tokenService.HashRefreshToken(refreshToken);
        var storedToken = await _context.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash);
        if (storedToken is null || !storedToken.IsActive)
        {
            return;
        }

        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        storedToken.RevokedByIp = ipAddress;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null || !user.IsActive)
        {
            return false;
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            return false;
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        await _userSessionService.InvalidateUserAsync(user);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<TokenResponse> IssueTokensAsync(
        User user,
        string[] roles,
        string[] permissions,
        string ipAddress,
        string? refreshToken = null)
    {
        var accessToken = _tokenService.CreateAccessToken(user, roles, permissions);
        refreshToken ??= _tokenService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashRefreshToken(refreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
            CreatedByIp = ipAddress
        });
        await _context.SaveChangesAsync();

        return new TokenResponse(accessToken.Token, refreshToken, accessToken.ExpiresAt, roles, permissions);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string[] GetRoles(User user) =>
        user.UserRoles.Select(ur => ur.Role.Name).Distinct().Order().ToArray();

    private static string[] GetPermissions(User user) =>
        user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .Order()
            .ToArray();
}
