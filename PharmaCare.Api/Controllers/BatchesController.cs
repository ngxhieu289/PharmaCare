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
[Route("api/batches")]
public class BatchesController : ControllerBase
{
    private readonly AppDbContext _context;

    public BatchesController(AppDbContext context) => _context = context;

    [Authorize(Policy = PermissionCodes.InventoryRead)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<BatchResponse>>> GetBatches(
        [FromQuery] string? search,
        [FromQuery] Guid? productId,
        [FromQuery] DateOnly? expiringBefore,
        [FromQuery] bool? expired,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _context.Batches.AsNoTracking().AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(b => b.ProductId == productId);
        }
        if (expiringBefore.HasValue)
        {
            query = query.Where(b => b.ExpiryDate <= expiringBefore);
        }
        if (expired.HasValue)
        {
            query = expired.Value
                ? query.Where(b => b.ExpiryDate < today)
                : query.Where(b => b.ExpiryDate >= today);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(b =>
                EF.Functions.ILike(b.BatchNumber, pattern) ||
                EF.Functions.ILike(b.Product!.Code, pattern) ||
                EF.Functions.ILike(b.Product.Name, pattern));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await Project(query.OrderBy(b => b.ExpiryDate), today)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(PagedResponse<BatchResponse>.Create(items, page, pageSize, totalItems));
    }

    [Authorize(Policy = PermissionCodes.InventoryRead)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BatchResponse>> GetBatch(Guid id, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var batch = await Project(_context.Batches.AsNoTracking().Where(b => b.Id == id), today)
            .SingleOrDefaultAsync(cancellationToken);
        return batch is null ? NotFound() : Ok(batch);
    }

    [Authorize(Policy = PermissionCodes.InventoryAdjust)]
    [HttpPost]
    public async Task<ActionResult<BatchResponse>> CreateBatch(
        SaveBatchRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await Validate(request, null, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var batch = new Batch();
        Map(request, batch);
        _context.Batches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await Project(_context.Batches.AsNoTracking().Where(b => b.Id == batch.Id), today)
            .SingleAsync(cancellationToken);
        return CreatedAtAction(nameof(GetBatch), new { id = batch.Id }, response);
    }

    [Authorize(Policy = PermissionCodes.InventoryAdjust)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateBatch(
        Guid id,
        SaveBatchRequest request,
        CancellationToken cancellationToken)
    {
        var batch = await _context.Batches.FindAsync([id], cancellationToken);
        if (batch is null)
        {
            return NotFound();
        }

        if (batch.ProductId != request.ProductId && await IsReferenced(id, cancellationToken))
        {
            return Conflict(new { message = "Không thể đổi sản phẩm của lô đã phát sinh tồn kho hoặc giao dịch." });
        }

        var validation = await Validate(request, id, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        Map(request, batch);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = PermissionCodes.InventoryAdjust)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBatch(Guid id, CancellationToken cancellationToken)
    {
        var batch = await _context.Batches.FindAsync([id], cancellationToken);
        if (batch is null)
        {
            return NotFound();
        }
        if (await IsReferenced(id, cancellationToken))
        {
            return Conflict(new { message = "Không thể xóa lô đã phát sinh tồn kho hoặc giao dịch." });
        }

        _context.Batches.Remove(batch);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult?> Validate(
        SaveBatchRequest request,
        Guid? currentId,
        CancellationToken cancellationToken)
    {
        if (request.ProductId == Guid.Empty ||
            !await _context.Products.AnyAsync(
                p => p.Id == request.ProductId && p.IsActive, cancellationToken))
        {
            return BadRequest(new { message = "Sản phẩm không tồn tại hoặc đã ngừng hoạt động." });
        }
        if (request.MfgDate == default || request.ExpiryDate == default ||
            request.ExpiryDate < request.MfgDate)
        {
            return BadRequest(new { message = "Ngày sản xuất và hạn dùng không hợp lệ." });
        }

        var batchNumber = request.BatchNumber.Trim().ToUpperInvariant();
        if (await _context.Batches.AnyAsync(
            b => b.Id != currentId && b.ProductId == request.ProductId &&
                 b.BatchNumber == batchNumber, cancellationToken))
        {
            return Conflict(new { message = "Số lô đã tồn tại cho sản phẩm này." });
        }
        return null;
    }

    private async Task<bool> IsReferenced(Guid id, CancellationToken cancellationToken)
    {
        if (await _context.BranchInventories.AnyAsync(i => i.BatchId == id, cancellationToken))
        {
            return true;
        }
        if (await _context.OrderItems.AnyAsync(i => i.BatchId == id, cancellationToken))
        {
            return true;
        }
        return await _context.InventoryTransactions.AnyAsync(i => i.BatchId == id, cancellationToken);
    }

    private static IQueryable<BatchResponse> Project(IQueryable<Batch> query, DateOnly today) =>
        query.Select(b => new BatchResponse(
            b.Id, b.ProductId, b.Product!.Code, b.Product.Name,
            b.BatchNumber, b.MfgDate, b.ExpiryDate, b.CostPrice,
            b.ExpiryDate < today));

    private static void Map(SaveBatchRequest request, Batch batch)
    {
        batch.ProductId = request.ProductId;
        batch.BatchNumber = request.BatchNumber.Trim().ToUpperInvariant();
        batch.MfgDate = request.MfgDate;
        batch.ExpiryDate = request.ExpiryDate;
        batch.CostPrice = request.CostPrice;
    }
}
