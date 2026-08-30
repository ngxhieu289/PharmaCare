using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Api.Authorization;
using PharmaCare.Api.Data;
using PharmaCare.Api.Dtos;
using PharmaCare.Api.Dtos.Common;
using PharmaCare.Api.Entities;
using PharmaCare.Api.Services;

namespace PharmaCare.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly IBranchAccessService _branchAccess;

    public InventoryController(
        AppDbContext context,
        IInventoryService inventoryService,
        IBranchAccessService branchAccess)
    {
        _context = context;
        _inventoryService = inventoryService;
        _branchAccess = branchAccess;
    }

    [Authorize(Policy = PermissionCodes.InventoryRead)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<InventoryResponse>>> GetInventory(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? productId,
        [FromQuery] string? search,
        [FromQuery] bool lowStockOnly = false,
        [FromQuery] DateOnly? expiringBefore = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var accessibleBranches = await _branchAccess
            .GetAccessibleBranchIdsAsync(User, cancellationToken);
        var query = _context.BranchInventories.AsNoTracking().AsQueryable();

        if (accessibleBranches is not null)
        {
            query = query.Where(i => accessibleBranches.Contains(i.BranchId));
        }
        if (branchId.HasValue)
        {
            if (!await _branchAccess.CanAccessAsync(User, branchId.Value, cancellationToken))
            {
                return Forbid();
            }
            query = query.Where(i => i.BranchId == branchId);
        }
        if (productId.HasValue)
        {
            query = query.Where(i => i.ProductId == productId);
        }
        if (lowStockOnly)
        {
            query = query.Where(i => i.QuantityOnHand - i.ReservedQuantity <= i.ReorderLevel);
        }
        if (expiringBefore.HasValue)
        {
            query = query.Where(i => i.Batch!.ExpiryDate <= expiringBefore);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(i =>
                EF.Functions.ILike(i.Product!.Code, pattern) ||
                EF.Functions.ILike(i.Product.Name, pattern) ||
                EF.Functions.ILike(i.Batch!.BatchNumber, pattern));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var items = await query
            .OrderBy(i => i.Branch!.Code)
            .ThenBy(i => i.Product!.Name)
            .ThenBy(i => i.Batch!.ExpiryDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InventoryResponse(
                i.BranchId, i.Branch!.Code,
                i.ProductId, i.Product!.Code, i.Product.Name,
                i.BatchId, i.Batch!.BatchNumber, i.Batch.ExpiryDate,
                i.QuantityOnHand, i.ReservedQuantity,
                i.QuantityOnHand - i.ReservedQuantity,
                i.ReorderLevel,
                i.QuantityOnHand - i.ReservedQuantity <= i.ReorderLevel,
                i.Batch.ExpiryDate < today,
                i.Version))
            .ToListAsync(cancellationToken);

        return Ok(PagedResponse<InventoryResponse>.Create(items, page, pageSize, totalItems));
    }

    [Authorize(Policy = PermissionCodes.InventoryRead)]
    [HttpGet("transactions")]
    public async Task<ActionResult<PagedResponse<InventoryTransactionResponse>>> GetTransactions(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? productId,
        [FromQuery] Guid? batchId,
        [FromQuery] string? transactionType,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var accessibleBranches = await _branchAccess
            .GetAccessibleBranchIdsAsync(User, cancellationToken);
        var query = _context.InventoryTransactions.AsNoTracking().AsQueryable();

        if (accessibleBranches is not null)
        {
            query = query.Where(t => accessibleBranches.Contains(t.BranchId));
        }
        if (branchId.HasValue)
        {
            if (!await _branchAccess.CanAccessAsync(User, branchId.Value, cancellationToken))
            {
                return Forbid();
            }
            query = query.Where(t => t.BranchId == branchId);
        }
        if (productId.HasValue) query = query.Where(t => t.ProductId == productId);
        if (batchId.HasValue) query = query.Where(t => t.BatchId == batchId);
        if (!string.IsNullOrWhiteSpace(transactionType))
        {
            var normalizedType = transactionType.Trim().ToUpperInvariant();
            query = query.Where(t => t.TransactionType == normalizedType);
        }
        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from);
        if (to.HasValue) query = query.Where(t => t.CreatedAt <= to);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new InventoryTransactionResponse(
                t.Id, t.BranchId, t.Branch.Code,
                t.ProductId, t.Product.Code,
                t.BatchId, t.Batch.BatchNumber,
                t.TransactionType, t.Quantity, t.BalanceAfter,
                t.ReferenceType, t.ReferenceId, t.Note,
                t.CreatedBy, t.CreatedByUser.DisplayName, t.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(PagedResponse<InventoryTransactionResponse>.Create(
            items, page, pageSize, totalItems));
    }

    [Authorize(Policy = PermissionCodes.InventoryAdjust)]
    [HttpPost("receive")]
    public async Task<IActionResult> Receive(
        ReceiveInventoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _branchAccess.CanAccessAsync(User, request.BranchId, cancellationToken))
        {
            return Forbid();
        }
        return await Execute(
            actorId => _inventoryService.ReceiveAsync(request, actorId, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.InventoryAdjust)]
    [HttpPost("adjust")]
    public async Task<IActionResult> Adjust(
        AdjustInventoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _branchAccess.CanAccessAsync(User, request.BranchId, cancellationToken))
        {
            return Forbid();
        }
        return await Execute(
            actorId => _inventoryService.AdjustAsync(request, actorId, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.InventoryAdjust)]
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(
        TransferInventoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _branchAccess.CanAccessAsync(User, request.FromBranchId, cancellationToken) ||
            !await _branchAccess.CanAccessAsync(User, request.ToBranchId, cancellationToken))
        {
            return Forbid();
        }
        return await Execute(
            actorId => _inventoryService.TransferAsync(request, actorId, cancellationToken));
    }

    private async Task<IActionResult> Execute(Func<Guid, Task> operation)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
        {
            return Unauthorized();
        }

        try
        {
            await operation(actorId);
            return NoContent();
        }
        catch (InventoryOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }
}
