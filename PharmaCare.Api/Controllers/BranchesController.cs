using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Api.Authorization;
using PharmaCare.Api.Data;
using PharmaCare.Api.Dtos;
using PharmaCare.Api.Dtos.Common;
using PharmaCare.Api.Entities;

namespace PharmaCare.Api.Controllers;

[ApiController]
[Route("api/branches")]
public class BranchesController : ControllerBase
{
    private readonly AppDbContext _context;

    public BranchesController(AppDbContext context) => _context = context;

    [Authorize(Policy = PermissionCodes.BranchesRead)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<BranchResponse>>> GetBranches(
        [FromQuery] string? search,
        [FromQuery] string? province,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = _context.Branches.AsNoTracking().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(b => b.IsActive);
        }
        if (!string.IsNullOrWhiteSpace(province))
        {
            query = query.Where(b => b.Province != null &&
                                     EF.Functions.ILike(b.Province, province.Trim()));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(b =>
                EF.Functions.ILike(b.Code, pattern) ||
                EF.Functions.ILike(b.Name, pattern) ||
                EF.Functions.ILike(b.Address, pattern));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await Project(query.OrderBy(b => b.Code))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(PagedResponse<BranchResponse>.Create(items, page, pageSize, totalItems));
    }

    [Authorize(Policy = PermissionCodes.BranchesRead)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BranchResponse>> GetBranch(
        Guid id,
        CancellationToken cancellationToken)
    {
        var branch = await Project(_context.Branches.AsNoTracking().Where(b => b.Id == id))
            .SingleOrDefaultAsync(cancellationToken);
        return branch is null ? NotFound() : Ok(branch);
    }

    [Authorize(Policy = PermissionCodes.BranchesManage)]
    [HttpPost]
    public async Task<ActionResult<BranchResponse>> CreateBranch(
        SaveBranchRequest request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _context.Branches.AnyAsync(b => b.Code == code, cancellationToken))
        {
            return Conflict(new { message = "Mã chi nhánh đã tồn tại." });
        }

        var branch = new Branch { Code = code };
        Map(request, branch);
        _context.Branches.Add(branch);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetBranch), new { id = branch.Id }, ToResponse(branch));
    }

    [Authorize(Policy = PermissionCodes.BranchesManage)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateBranch(
        Guid id,
        SaveBranchRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await _context.Branches.FindAsync([id], cancellationToken);
        if (branch is null)
        {
            return NotFound();
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _context.Branches.AnyAsync(b => b.Id != id && b.Code == code, cancellationToken))
        {
            return Conflict(new { message = "Mã chi nhánh đã tồn tại." });
        }

        branch.Code = code;
        Map(request, branch);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = PermissionCodes.BranchesManage)]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await _context.Branches.FindAsync([id], cancellationToken);
        if (branch is null)
        {
            return NotFound();
        }

        branch.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = PermissionCodes.BranchesManage)]
    [HttpDelete("{id:guid}")]
    public Task<IActionResult> DisableBranch(Guid id, CancellationToken cancellationToken) =>
        SetStatus(id, new SetActiveRequest { IsActive = false }, cancellationToken);

    private static IQueryable<BranchResponse> Project(IQueryable<Branch> query) =>
        query.Select(b => new BranchResponse(
            b.Id, b.Code, b.Name, b.Address, b.Phone,
            b.Province, b.District, b.Ward, b.IsActive));

    private static BranchResponse ToResponse(Branch branch) =>
        new(branch.Id, branch.Code, branch.Name, branch.Address, branch.Phone,
            branch.Province, branch.District, branch.Ward, branch.IsActive);

    private static void Map(SaveBranchRequest request, Branch branch)
    {
        branch.Name = request.Name.Trim();
        branch.Address = request.Address.Trim();
        branch.Phone = Clean(request.Phone);
        branch.Province = Clean(request.Province);
        branch.District = Clean(request.District);
        branch.Ward = Clean(request.Ward);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
