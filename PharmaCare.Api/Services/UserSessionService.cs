using Microsoft.EntityFrameworkCore;
using PharmaCare.Api.Data;
using PharmaCare.Api.Entities;

namespace PharmaCare.Api.Services;

public sealed class UserSessionService : IUserSessionService
{
    private readonly AppDbContext _context;

    public UserSessionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task InvalidateUserAsync(
        User user,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default)
    {
        user.TokenVersion++;
        await RevokeActiveRefreshTokensAsync([user.Id], revokedByIp, cancellationToken);
    }

    public async Task InvalidateRoleMembersAsync(
        Guid roleId,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .Where(user => user.UserRoles.Any(userRole => userRole.RoleId == roleId))
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            user.TokenVersion++;
        }

        await RevokeActiveRefreshTokensAsync(
            users.Select(user => user.Id).ToArray(),
            revokedByIp,
            cancellationToken);
    }

    private async Task RevokeActiveRefreshTokensAsync(
        IReadOnlyCollection<Guid> userIds,
        string? revokedByIp,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var tokens = await _context.RefreshTokens
            .Where(token => userIds.Contains(token.UserId) &&
                            token.RevokedAt == null &&
                            token.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = now;
            token.RevokedByIp = revokedByIp;
        }
    }
}
