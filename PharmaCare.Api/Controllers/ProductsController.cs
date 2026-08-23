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

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] string? symptom,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? rxFlag,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string sort = "name",
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
        if (minPrice.HasValue) query = query.Where(p => p.UnitPrice >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => p.UnitPrice <= maxPrice.Value);
        if (!string.IsNullOrWhiteSpace(symptom))
        {
            var symptomPattern = $"%{symptom.Trim()}%";
            query = query.Where(p => p.Indications != null && EF.Functions.ILike(p.Indications, symptomPattern));
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
        var ordered = sort.Trim().ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(p => p.UnitPrice).ThenBy(p => p.Name),
            "price_desc" => query.OrderByDescending(p => p.UnitPrice).ThenBy(p => p.Name),
            _ => query.OrderBy(p => p.Name)
        };
        var items = await Project(ordered)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(PagedResponse<ProductResponse>.Create(items, page, pageSize, totalItems));
    }

    [AllowAnonymous]
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
        var unitError = ValidateUnits(request);
        if (unitError is not null) return BadRequest(new { message = unitError });

        var product = new Product { Code = code };
        Map(request, product);
        SyncUnits(request, product);
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
        var product = await _context.Products.Include(item => item.SaleUnits)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
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
        var unitError = ValidateUnits(request);
        if (unitError is not null) return BadRequest(new { message = unitError });

        product.Code = code;
        Map(request, product);
        SyncUnits(request, product);
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

    [AllowAnonymous]
    [HttpGet("{id:guid}/availability")]
    public async Task<ActionResult<ProductAvailabilityResponse>> GetAvailability(
        Guid id,
        [FromQuery] Guid branchId,
        [FromQuery] Guid? saleUnitId,
        CancellationToken cancellationToken)
    {
        if (!await _context.Products.AnyAsync(p => p.Id == id && p.IsActive, cancellationToken))
        {
            return NotFound(new { message = "Không tìm thấy sản phẩm đang kinh doanh." });
        }
        if (!await _context.Branches.AnyAsync(b => b.Id == branchId && b.IsActive, cancellationToken))
        {
            return NotFound(new { message = "Không tìm thấy chi nhánh đang hoạt động." });
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var stock = await _context.BranchInventories
            .AsNoTracking()
            .Where(i => i.ProductId == id && i.BranchId == branchId && i.Batch!.ExpiryDate >= today)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Available = group.Sum(i => i.QuantityOnHand - i.ReservedQuantity),
                ReorderLevel = group.Sum(i => i.ReorderLevel)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var conversionFactor = 1;
        string? unitName = null;
        if (saleUnitId.HasValue)
        {
            var unit = await _context.ProductSaleUnits.AsNoTracking()
                .Where(u => u.Id == saleUnitId && u.ProductId == id && u.IsActive)
                .Select(u => new { u.ConversionFactor, u.UnitName })
                .SingleOrDefaultAsync(cancellationToken);
            if (unit is null) return BadRequest(new { message = "Đơn vị bán không hợp lệ." });
            conversionFactor = unit.ConversionFactor;
            unitName = unit.UnitName;
        }
        var available = Math.Max(stock?.Available ?? 0, 0) / conversionFactor;
        var reorderLevel = (stock?.ReorderLevel ?? 0) / conversionFactor;
        var status = available == 0
            ? "OUT_OF_STOCK"
            : available <= reorderLevel ? "LOW_STOCK" : "IN_STOCK";

        return Ok(new ProductAvailabilityResponse(id, branchId, available, status, saleUnitId, unitName));
    }

    private Task<bool> IsActiveCategory(Guid categoryId, CancellationToken cancellationToken) =>
        _context.Categories.AnyAsync(c => c.Id == categoryId && c.IsActive, cancellationToken);

    private static IQueryable<ProductResponse> Project(IQueryable<Product> query) =>
        query.Select(p => new ProductResponse(
            p.Id, p.Code, p.Name, p.ActiveIngredient, p.Indications,
            p.Brand, p.RegistrationNumber, p.DosageForm, p.Manufacturer,
            p.CountryOfOrigin, p.ShelfLife, p.Composition, p.UsageInstructions,
            p.Contraindications, p.SideEffects,
            p.CategoryId, p.Category!.Name, p.RxFlag, p.VatRate,
            p.Packaging, p.UnitPrice, p.StorageTemp, p.WarningText, p.ImageUrl, p.IsActive,
            p.SaleUnits.Where(u => u.IsActive).OrderByDescending(u => u.ConversionFactor)
                .Select(u => new ProductSaleUnitResponse(
                    u.Id, u.UnitName, u.ConversionFactor, u.SalePrice, u.IsDefault, u.IsActive))
                .ToArray()));

    private static void Map(SaveProductRequest request, Product product)
    {
        product.Name = request.Name.Trim();
        product.ActiveIngredient = Clean(request.ActiveIngredient);
        product.Indications = Clean(request.Indications);
        product.Brand = Clean(request.Brand);
        product.RegistrationNumber = Clean(request.RegistrationNumber);
        product.DosageForm = Clean(request.DosageForm);
        product.Manufacturer = Clean(request.Manufacturer);
        product.CountryOfOrigin = Clean(request.CountryOfOrigin);
        product.ShelfLife = Clean(request.ShelfLife);
        product.Composition = Clean(request.Composition);
        product.UsageInstructions = Clean(request.UsageInstructions);
        product.Contraindications = Clean(request.Contraindications);
        product.SideEffects = Clean(request.SideEffects);
        product.CategoryId = request.CategoryId;
        product.RxFlag = request.RxFlag;
        product.VatRate = request.VatRate;
        product.Packaging = request.Packaging.Trim();
        product.UnitPrice = request.UnitPrice;
        product.StorageTemp = Clean(request.StorageTemp);
        product.WarningText = Clean(request.WarningText);
        product.ImageUrl = Clean(request.ImageUrl);
    }

    private static string? ValidateUnits(SaveProductRequest request)
    {
        if (request.SaleUnits.Count == 0) return "Sản phẩm phải có ít nhất một đơn vị bán.";
        if (request.SaleUnits.Count(unit => unit.IsDefault) != 1) return "Phải chọn đúng một đơn vị bán mặc định.";
        if (request.SaleUnits.GroupBy(unit => unit.UnitName.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1)) return "Tên đơn vị bán không được trùng.";
        if (request.SaleUnits.GroupBy(unit => unit.ConversionFactor).Any(group => group.Count() > 1)) return "Hệ số quy đổi của các đơn vị không được trùng.";
        return null;
    }

    private static void SyncUnits(SaveProductRequest request, Product product)
    {
        var requestedIds = request.SaleUnits.Where(unit => unit.Id.HasValue).Select(unit => unit.Id!.Value).ToHashSet();
        foreach (var existing in product.SaleUnits.Where(unit => !requestedIds.Contains(unit.Id))) existing.IsActive = false;
        foreach (var input in request.SaleUnits)
        {
            var unit = input.Id.HasValue ? product.SaleUnits.SingleOrDefault(item => item.Id == input.Id.Value) : null;
            if (unit is null) { unit = new ProductSaleUnit(); product.SaleUnits.Add(unit); }
            unit.UnitName = input.UnitName.Trim(); unit.ConversionFactor = input.ConversionFactor;
            unit.SalePrice = input.SalePrice; unit.IsDefault = input.IsDefault; unit.IsActive = true;
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
