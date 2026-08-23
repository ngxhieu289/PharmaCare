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
[Authorize]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IOrderService _orderService;
    private readonly IBranchAccessService _branchAccess;

    public OrdersController(
        AppDbContext context,
        IOrderService orderService,
        IBranchAccessService branchAccess)
    {
        _context = context;
        _orderService = orderService;
        _branchAccess = branchAccess;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<OrderResponse>>> GetOrders(
        [FromQuery] string? status,
        [FromQuery] Guid? branchId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!HasPermission(PermissionCodes.OrdersRead) || !TryGetUserId(out var userId))
        {
            return Forbid();
        }

        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = _context.Orders.AsNoTracking().AsQueryable();
        if (!User.IsInRole("Admin"))
        {
            var branches = await _branchAccess.GetAccessibleBranchIdsAsync(User, cancellationToken)
                           ?? Array.Empty<Guid>();
            query = query.Where(o => o.CustomerId == userId || branches.Contains(o.BranchId));
        }
        if (branchId.HasValue)
        {
            query = query.Where(o => o.BranchId == branchId);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            if (!IsValidStatus(normalizedStatus))
            {
                return BadRequest(new { message = "Trạng thái đơn hàng không hợp lệ." });
            }
            query = query.Where(o => o.Status == normalizedStatus);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(o =>
                EF.Functions.ILike(o.Code, pattern) ||
                EF.Functions.ILike(o.Customer!.DisplayName, pattern) ||
                EF.Functions.ILike(o.Customer.Email, pattern));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await Project(query.OrderByDescending(o => o.CreatedAt))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return Ok(PagedResponse<OrderResponse>.Create(items, page, pageSize, totalItems));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(PermissionCodes.OrdersRead) ||
            !await CanAccessOrder(id, cancellationToken))
        {
            return NotFound();
        }

        var response = await Project(_context.Orders.AsNoTracking().Where(o => o.Id == id))
            .SingleAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actorId))
        {
            return Unauthorized();
        }

        var canCreate = HasPermission(PermissionCodes.OrdersCreate);
        var canManage = HasPermission(PermissionCodes.OrdersManage);
        if (!canCreate && !canManage)
        {
            return Forbid();
        }

        var orderType = request.OrderType.Trim().ToUpperInvariant();
        if (orderType == OrderTypes.Pos &&
            (!canManage || !await _branchAccess.CanAccessAsync(User, request.BranchId, cancellationToken)))
        {
            return Forbid();
        }

        try
        {
            var id = await _orderService.CreateAsync(
                request, actorId, canManage, cancellationToken);
            var response = await Project(_context.Orders.AsNoTracking().Where(o => o.Id == id))
                .SingleAsync(cancellationToken);
            return CreatedAtAction(nameof(GetOrder), new { id }, response);
        }
        catch (OrderOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [Authorize(Policy = PermissionCodes.OrdersManage)]
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(
        Guid id,
        ChangeOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanManageOrder(id, cancellationToken)) return Forbid();
        return await Execute(actor =>
            _orderService.ConfirmAsync(id, actor, request.Note, cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        ChangeOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var order = await _context.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new { o.CustomerId, o.BranchId })
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null) return NotFound();

        var canManage = HasPermission(PermissionCodes.OrdersManage) &&
                        await _branchAccess.CanAccessAsync(User, order.BranchId, cancellationToken);
        if (order.CustomerId != userId && !canManage) return Forbid();

        return await Execute(actor =>
            _orderService.CancelAsync(id, actor, request.Note, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.OrdersManage)]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(
        Guid id,
        ChangeOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanManageOrder(id, cancellationToken)) return Forbid();
        return await Execute(actor =>
            _orderService.CompleteAsync(id, actor, request.Note, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.OrdersManage)]
    [HttpPost("{id:guid}/payments/confirm")]
    public async Task<IActionResult> ConfirmPayment(
        Guid id, ConfirmPaymentRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManageOrder(id, cancellationToken)) return Forbid();
        return await Execute(actor =>
            _orderService.ConfirmPaymentAsync(id, actor, request, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.OrdersManage)]
    [HttpPost("{id:guid}/payments/refund")]
    public async Task<IActionResult> RefundPayment(
        Guid id, RefundPaymentRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManageOrder(id, cancellationToken)) return Forbid();
        return await Execute(actor =>
            _orderService.RefundPaymentAsync(id, actor, request.Reason, cancellationToken));
    }

    private async Task<IActionResult> Execute(Func<Guid, Task> action)
    {
        if (!TryGetUserId(out var actorId)) return Unauthorized();
        try
        {
            await action(actorId);
            return NoContent();
        }
        catch (OrderOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    private async Task<bool> CanAccessOrder(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return false;
        var order = await _context.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new { o.CustomerId, o.BranchId })
            .SingleOrDefaultAsync(cancellationToken);
        return order is not null &&
               (order.CustomerId == userId || User.IsInRole("Admin") ||
                await _branchAccess.CanAccessAsync(User, order.BranchId, cancellationToken));
    }

    private async Task<bool> CanManageOrder(Guid id, CancellationToken cancellationToken)
    {
        var branchId = await _context.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => (Guid?)o.BranchId)
            .SingleOrDefaultAsync(cancellationToken);
        return branchId.HasValue &&
               await _branchAccess.CanAccessAsync(User, branchId.Value, cancellationToken);
    }

    private bool HasPermission(string permission) =>
        User.HasClaim(PermissionCodes.ClaimType, permission);

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static bool IsValidStatus(string status) =>
        status is OrderStatuses.Pending or OrderStatuses.Confirmed or
            OrderStatuses.Completed or OrderStatuses.Cancelled;

    private static IQueryable<OrderResponse> Project(IQueryable<Order> query) =>
        query.Select(o => new OrderResponse(
            o.Id, o.Code, o.CustomerId, o.Customer!.DisplayName,
            o.BranchId, o.Branch!.Code, o.PrescriptionId,
            o.OrderType, o.PickupType, o.Status,
            o.SubtotalBeforeVat, o.TotalVatAmount, o.ShippingFee,
            o.DiscountAmount, o.TotalAmount, o.VoucherCode,
            o.PaymentMethod, o.PaymentStatus,
            o.RecipientName, o.RecipientPhone, o.ShippingAddress,
            o.CreatedAt, o.UpdatedAt,
            o.OrderItems.OrderBy(i => i.Product!.Name).ThenBy(i => i.Batch!.ExpiryDate)
                .Select(i => new OrderItemResponse(
                    i.Id, i.ProductId, i.Product!.Code, i.Product.Name,
                    i.BatchId, i.Batch!.BatchNumber, i.Batch.ExpiryDate,
                    i.Quantity, i.UnitPrice, i.VatRate, i.VatAmount, i.LineTotal))
                .ToArray(),
            o.StatusHistory.OrderBy(h => h.ChangedAt)
                .Select(h => new OrderStatusHistoryResponse(
                    h.FromStatus, h.ToStatus, h.Note,
                    h.ChangedBy, h.ChangedByUser.DisplayName, h.ChangedAt))
                .ToArray(),
            o.Payments.OrderBy(p => p.CreatedAt)
                .Select(p => new PaymentTransactionResponse(
                    p.Id, p.TransactionType, p.Method, p.Amount, p.Status,
                    p.ExternalReference, p.Note, p.CreatedBy,
                    p.CreatedByUser.DisplayName, p.CreatedAt))
                .ToArray()));
}
