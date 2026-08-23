using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Api.Data;

namespace PharmaCare.Api.Services;

public sealed class BranchAccessService : IBranchAccessService
{
    private readonly AppDbContext _context;

    public BranchAccessService(AppDbContext context) => _context = context;

    public async Task<bool> CanAccessAsync(
        ClaimsPrincipal principal,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (principal.IsInRole("Admin"))
        {
            return true;
        }

        var userId = GetUserId(principal);
        return userId.HasValue && await _context.UserBranches
            .AnyAsync(ub => ub.UserId == userId && ub.BranchId == branchId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>?> GetAccessibleBranchIdsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (principal.IsInRole("Admin"))
        {
            return null;
        }

        var userId = GetUserId(principal);
        if (!userId.HasValue)
        {
            return Array.Empty<Guid>();
        }

        return await _context.UserBranches
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.BranchId)
            .ToListAsync(cancellationToken);
    }

    private static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
}
