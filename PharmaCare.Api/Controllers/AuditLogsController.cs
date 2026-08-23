using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Api.Authorization;
using PharmaCare.Api.Data;
using PharmaCare.Api.Dtos;
using PharmaCare.Api.Dtos.Common;

namespace PharmaCare.Api.Controllers;

[ApiController]
[Authorize(Policy = PermissionCodes.AuditRead)]
[Route("api/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly AppDbContext _context;
    public AuditLogsController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AuditLogResponse>>> GetAll(
        [FromQuery] Guid? userId, [FromQuery] string? action,
        [FromQuery] string? entityName, [FromQuery] string? entityId,
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        if (from.HasValue && to.HasValue && to < from)
            return BadRequest(new { message = "Thời gian kết thúc phải sau thời gian bắt đầu." });
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();
        if (userId.HasValue) query = query.Where(a => a.UserId == userId);
        if (!string.IsNullOrWhiteSpace(action))
        {
            var normalized = action.Trim().ToUpperInvariant();
            query = query.Where(a => a.Action == normalized);
        }
        if (!string.IsNullOrWhiteSpace(entityName))
        {
            var pattern = $"%{entityName.Trim()}%";
            query = query.Where(a => EF.Functions.ILike(a.EntityName, pattern));
        }
        if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(a => a.EntityId == entityId.Trim());
        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(a => a.CreatedAt <= to.Value.ToUniversalTime());
        var count = await query.CountAsync(cancellationToken);
        var items = await Project(query.OrderByDescending(a => a.CreatedAt))
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(PagedResponse<AuditLogResponse>.Create(items, page, pageSize, count));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AuditLogResponse>> Get(long id, CancellationToken cancellationToken)
    {
        var item = await Project(_context.AuditLogs.AsNoTracking().Where(a => a.Id == id))
            .SingleOrDefaultAsync(cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    private static IQueryable<AuditLogResponse> Project(IQueryable<PharmaCare.Api.Entities.AuditLog> query) =>
        query.Select(a => new AuditLogResponse(
            a.Id, a.UserId, a.User!.DisplayName, a.Action, a.EntityName,
            a.EntityId, a.OldValues, a.NewValues, a.IpAddress, a.CreatedAt));
}
