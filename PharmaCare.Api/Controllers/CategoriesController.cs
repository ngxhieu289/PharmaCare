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
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context) => _context = context;

    [Authorize(Policy = PermissionCodes.ProductsRead)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CategoryResponse>>> GetCategories(
        [FromQuery] string? search,
        [FromQuery] Guid? parentId,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = _context.Categories.AsNoTracking().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }
        if (parentId.HasValue)
        {
            query = query.Where(c => c.ParentId == parentId);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(c => EF.Functions.ILike(c.Name, pattern) ||
                                     EF.Functions.ILike(c.Slug, pattern));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoryResponse(
                c.Id, c.Name, c.Slug, c.ParentId,
                c.Parent == null ? null : c.Parent.Name, c.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(PagedResponse<CategoryResponse>.Create(items, page, pageSize, totalItems));
    }

    [Authorize(Policy = PermissionCodes.ProductsRead)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> GetCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryResponse(
                c.Id, c.Name, c.Slug, c.ParentId,
                c.Parent == null ? null : c.Parent.Name, c.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

        return category is null ? NotFound() : Ok(category);
    }

    [Authorize(Policy = PermissionCodes.ProductsManage)]
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(
        SaveCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await _context.Categories.AnyAsync(c => c.Slug == slug, cancellationToken))
        {
            return Conflict(new { message = "Slug danh mục đã tồn tại." });
        }
        if (request.ParentId.HasValue &&
            !await _context.Categories.AnyAsync(
                c => c.Id == request.ParentId && c.IsActive, cancellationToken))
        {
            return BadRequest(new { message = "Danh mục cha không tồn tại hoặc đã ngừng hoạt động." });
        }

        var category = new Category
        {
            Name = request.Name.Trim(),
            Slug = slug,
            ParentId = request.ParentId
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id },
            new CategoryResponse(category.Id, category.Name, category.Slug,
                category.ParentId, null, category.IsActive));
    }

    [Authorize(Policy = PermissionCodes.ProductsManage)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        SaveCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FindAsync([id], cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await _context.Categories.AnyAsync(c => c.Id != id && c.Slug == slug, cancellationToken))
        {
            return Conflict(new { message = "Slug danh mục đã tồn tại." });
        }
        if (request.ParentId == id || await WouldCreateCycle(id, request.ParentId, cancellationToken))
        {
            return BadRequest(new { message = "Danh mục cha tạo ra quan hệ vòng lặp." });
        }
        if (request.ParentId.HasValue &&
            !await _context.Categories.AnyAsync(
                c => c.Id == request.ParentId && c.IsActive, cancellationToken))
        {
            return BadRequest(new { message = "Danh mục cha không tồn tại hoặc đã ngừng hoạt động." });
        }

        category.Name = request.Name.Trim();
        category.Slug = slug;
        category.ParentId = request.ParentId;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = PermissionCodes.ProductsManage)]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FindAsync([id], cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        category.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = PermissionCodes.ProductsManage)]
    [HttpDelete("{id:guid}")]
    public Task<IActionResult> DisableCategory(Guid id, CancellationToken cancellationToken) =>
        SetStatus(id, new SetActiveRequest { IsActive = false }, cancellationToken);

    private async Task<bool> WouldCreateCycle(
        Guid categoryId,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        var currentId = parentId;
        while (currentId.HasValue)
        {
            if (currentId == categoryId)
            {
                return true;
            }

            currentId = await _context.Categories
                .Where(c => c.Id == currentId)
                .Select(c => c.ParentId)
                .SingleOrDefaultAsync(cancellationToken);
        }
        return false;
    }
}
