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
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context) => _context = context;

    [Authorize(Policy = PermissionCodes.ProductsRead)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? rxFlag,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = _context.Products.AsNoTracking().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }
        if (rxFlag.HasValue)
        {
            query = query.Where(p => p.RxFlag == rxFlag);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Code, pattern) ||
                EF.Functions.ILike(p.Name, pattern) ||
                (p.ActiveIngredient != null && EF.Functions.ILike(p.ActiveIngredient, pattern)) ||
                (p.Indications != null && EF.Functions.ILike(p.Indications, pattern)));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await Project(query.OrderBy(p => p.Name))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(PagedResponse<ProductResponse>.Create(items, page, pageSize, totalItems));
    }

    [Authorize(Policy = PermissionCodes.ProductsRead)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetProduct(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await Project(_context.Products.AsNoTracking().Where(p => p.Id == id))
            .SingleOrDefaultAsync(cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [Authorize(Policy = PermissionCodes.ProductsManage)]
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> CreateProduct(
        SaveProductRequest request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _context.Products.AnyAsync(p => p.Code == code, cancellationToken))
        {
            return Conflict(new { message = "Mã sản phẩm đã tồn tại." });
        }
        if (!await IsActiveCategory(request.CategoryId, cancellationToken))
        {
            return BadRequest(new { message = "Danh mục không tồn tại hoặc đã ngừng hoạt động." });
        }

        var product = new Product { Code = code };
        Map(request, product);
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        var response = await Project(_context.Products.AsNoTracking().Where(p => p.Id == product.Id))
            .SingleAsync(cancellationToken);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, response);
    }

    [Authorize(Policy = PermissionCodes.ProductsManage)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        SaveProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync([id], cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _context.Products.AnyAsync(p => p.Id != id && p.Code == code, cancellationToken))
        {
            return Conflict(new { message = "Mã sản phẩm đã tồn tại." });
        }
        if (!await IsActiveCategory(request.CategoryId, cancellationToken))
        {
            return BadRequest(new { message = "Danh mục không tồn tại hoặc đã ngừng hoạt động." });
        }

        product.Code = code;
        Map(request, product);
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
        var product = await _context.Products.FindAsync([id], cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        product.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = PermissionCodes.ProductsManage)]
    [HttpDelete("{id:guid}")]
    public Task<IActionResult> DisableProduct(Guid id, CancellationToken cancellationToken) =>
        SetStatus(id, new SetActiveRequest { IsActive = false }, cancellationToken);

    private Task<bool> IsActiveCategory(Guid categoryId, CancellationToken cancellationToken) =>
        _context.Categories.AnyAsync(c => c.Id == categoryId && c.IsActive, cancellationToken);

    private static IQueryable<ProductResponse> Project(IQueryable<Product> query) =>
        query.Select(p => new ProductResponse(
            p.Id, p.Code, p.Name, p.ActiveIngredient, p.Indications,
            p.CategoryId, p.Category!.Name, p.RxFlag, p.VatRate,
            p.Packaging, p.UnitPrice, p.StorageTemp, p.WarningText, p.IsActive));

    private static void Map(SaveProductRequest request, Product product)
    {
        product.Name = request.Name.Trim();
        product.ActiveIngredient = Clean(request.ActiveIngredient);
        product.Indications = Clean(request.Indications);
        product.CategoryId = request.CategoryId;
        product.RxFlag = request.RxFlag;
        product.VatRate = request.VatRate;
        product.Packaging = request.Packaging.Trim();
        product.UnitPrice = request.UnitPrice;
        product.StorageTemp = Clean(request.StorageTemp);
        product.WarningText = Clean(request.WarningText);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
