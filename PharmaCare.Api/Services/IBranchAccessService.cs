using System.Security.Claims;

namespace PharmaCare.Api.Services;

public interface IBranchAccessService
{
    Task<bool> CanAccessAsync(
        ClaimsPrincipal principal,
        Guid branchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>?> GetAccessibleBranchIdsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
