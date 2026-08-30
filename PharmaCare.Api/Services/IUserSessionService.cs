using PharmaCare.Api.Entities;

namespace PharmaCare.Api.Services;

public interface IUserSessionService
{
    Task InvalidateUserAsync(
        User user,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default);

    Task InvalidateRoleMembersAsync(
        Guid roleId,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default);
}
