using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Api.Authorization;
using PharmaCare.Api.Data;
using PharmaCare.Api.Dtos;
using PharmaCare.Api.Entities;
using PharmaCare.Api.Services;

namespace PharmaCare.Api.Controllers;

[ApiController]
[Authorize(Policy = PermissionCodes.ReportsRead)]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IBranchAccessService _branchAccess;
    private IReadOnlyCollection<Guid>? _accessibleBranches;

    public ReportsController(AppDbContext context, IBranchAccessService branchAccess)
    {
        _context = context;
        _branchAccess = branchAccess;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResponse>> Dashboard(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] Guid? branchId, [FromQuery] int expiringWithinDays = 30,
        CancellationToken cancellationToken = default)
    {
        var range = NormalizeRange(from, to);
        if (range.Error is not null) return BadRequest(new { message = range.Error });
        if (expiringWithinDays is < 1 or > 365)
            return BadRequest(new { message = "expiringWithinDays phải từ 1 đến 365." });
        var branchError = await ValidateBranch(branchId, cancellationToken);
        if (branchError is not null) return branchError;

        var orders = ScopedOrders(branchId).Where(o =>
            o.CreatedAt >= range.StartUtc && o.CreatedAt < range.EndExclusiveUtc);
        var statusRows = await orders.GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .OrderBy(x => x.Status).ToListAsync(cancellationToken);
        var statusCounts = statusRows
            .Select(x => new OrderStatusCountResponse(x.Status, x.Count)).ToArray();
        var completed = orders.Where(o => o.Status == OrderStatuses.Completed);
        var financial = await completed.GroupBy(_ => 1).Select(g => new
        {
            Count = g.Count(),
            Gross = g.Sum(o => o.TotalAmount),
            Refunded = g.Sum(o => o.PaymentStatus == PaymentStatuses.Refunded ? o.TotalAmount : 0m),
            Discount = g.Sum(o => o.DiscountAmount),
            Vat = g.Sum(o => o.TotalVatAmount),
            BeforeVat = g.Sum(o => o.SubtotalBeforeVat),
            Shipping = g.Sum(o => o.ShippingFee)
        }).SingleOrDefaultAsync(cancellationToken);

        var inventory = ScopedInventory(branchId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expiryCutoff = today.AddDays(expiringWithinDays);
        var lowStock = await inventory.CountAsync(
            i => i.QuantityOnHand - i.ReservedQuantity <= i.ReorderLevel, cancellationToken);
        var expiring = await inventory.CountAsync(
            i => i.QuantityOnHand > 0 && i.Batch!.ExpiryDate >= today &&
                 i.Batch.ExpiryDate <= expiryCutoff, cancellationToken);
        var pendingRx = await ScopedPrescriptions(branchId).CountAsync(
            p => p.Status == PrescriptionStatuses.Pending, cancellationToken);
        var total = statusCounts.Sum(x => x.Count);
        var cancelled = statusCounts.SingleOrDefault(x => x.Status == OrderStatuses.Cancelled)?.Count ?? 0;
        var gross = financial?.Gross ?? 0m;
        var refunded = financial?.Refunded ?? 0m;
        var completedCount = financial?.Count ?? 0;

        return Ok(new DashboardResponse(
            new ReportPeriod(range.From, range.To), total, completedCount, cancelled,
            financial?.BeforeVat ?? 0m,
            (financial?.BeforeVat ?? 0m) + (financial?.Vat ?? 0m),
            gross, refunded, gross - refunded, financial?.Discount ?? 0m,
            financial?.Vat ?? 0m, financial?.Shipping ?? 0m,
            completedCount == 0 ? 0 : Math.Round(gross / completedCount, 2),
            pendingRx, lowStock, expiring, statusCounts));
    }

    [HttpGet("sales/daily")]
    public async Task<ActionResult<IReadOnlyCollection<DailySalesResponse>>> DailySales(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] Guid? branchId, CancellationToken cancellationToken)
    {
        var range = NormalizeRange(from, to);
        if (range.Error is not null) return BadRequest(new { message = range.Error });
        var branchError = await ValidateBranch(branchId, cancellationToken);
        if (branchError is not null) return branchError;
        var rows = await ScopedOrders(branchId)
            .Where(o => o.Status == OrderStatuses.Completed &&
                        o.CreatedAt >= range.StartUtc && o.CreatedAt < range.EndExclusiveUtc)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key, Count = g.Count(), Gross = g.Sum(o => o.TotalAmount),
                Refunded = g.Sum(o => o.PaymentStatus == PaymentStatuses.Refunded ? o.TotalAmount : 0m),
                Discount = g.Sum(o => o.DiscountAmount), BeforeVat = g.Sum(o => o.SubtotalBeforeVat),
                Vat = g.Sum(o => o.TotalVatAmount), Shipping = g.Sum(o => o.ShippingFee)
            }).OrderBy(x => x.Date).ToListAsync(cancellationToken);
        return Ok(rows.Select(x => new DailySalesResponse(
            DateOnly.FromDateTime(x.Date), x.Count, x.Gross, x.Refunded,
            x.Gross - x.Refunded, x.Discount, x.BeforeVat, x.Vat,
            x.BeforeVat + x.Vat, x.Shipping)).ToArray());
    }

    [HttpGet("sales/by-branch")]
    public async Task<ActionResult<IReadOnlyCollection<BranchSalesResponse>>> SalesByBranch(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var range = NormalizeRange(from, to);
        if (range.Error is not null) return BadRequest(new { message = range.Error });
        var branchError = await ValidateBranch(null, cancellationToken);
        if (branchError is not null) return branchError;
        var query = ScopedOrders(null).Where(o => o.Status == OrderStatuses.Completed &&
            o.CreatedAt >= range.StartUtc && o.CreatedAt < range.EndExclusiveUtc);
        var rows = await query.GroupBy(o => new { o.BranchId, o.Branch!.Code, o.Branch.Name })
            .Select(g => new
            {
                g.Key.BranchId, g.Key.Code, g.Key.Name, Count = g.Count(),
                Gross = g.Sum(o => o.TotalAmount),
                Refunded = g.Sum(o => o.PaymentStatus == PaymentStatuses.Refunded ? o.TotalAmount : 0m),
                Net = g.Sum(o => o.PaymentStatus == PaymentStatuses.Refunded ? 0m : o.TotalAmount),
                BeforeVat = g.Sum(o => o.SubtotalBeforeVat), Vat = g.Sum(o => o.TotalVatAmount),
                Discount = g.Sum(o => o.DiscountAmount), Shipping = g.Sum(o => o.ShippingFee)
            })
            .OrderBy(x => x.Code).ToListAsync(cancellationToken);
        return Ok(rows.Select(x => new BranchSalesResponse(
            x.BranchId, x.Code, x.Name, x.Count, x.Gross, x.Refunded, x.Net,
            x.BeforeVat, x.Vat, x.BeforeVat + x.Vat, x.Discount, x.Shipping)).ToArray());
    }

    [HttpGet("products/top")]
    public async Task<ActionResult<IReadOnlyCollection<TopProductResponse>>> TopProducts(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] Guid? branchId, [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var range = NormalizeRange(from, to);
        if (range.Error is not null) return BadRequest(new { message = range.Error });
        if (limit is < 1 or > 100) return BadRequest(new { message = "limit phải từ 1 đến 100." });
        var branchError = await ValidateBranch(branchId, cancellationToken);
        if (branchError is not null) return branchError;
        var orderIds = ScopedOrders(branchId).Where(o => o.Status == OrderStatuses.Completed &&
            o.PaymentStatus != PaymentStatuses.Refunded && o.CreatedAt >= range.StartUtc &&
            o.CreatedAt < range.EndExclusiveUtc).Select(o => o.Id);
        var rows = await _context.OrderItems.AsNoTracking().Where(i => orderIds.Contains(i.OrderId))
            .GroupBy(i => new { i.ProductId, i.Product!.Code, i.Product.Name })
            .Select(g => new
            {
                g.Key.ProductId, g.Key.Code, g.Key.Name,
                Quantity = g.Sum(i => i.Quantity), Gross = g.Sum(i => i.LineTotal),
                Orders = g.Select(i => i.OrderId).Distinct().Count()
            })
            .OrderByDescending(x => x.Quantity).ThenByDescending(x => x.Gross)
            .Take(limit).ToListAsync(cancellationToken);
        return Ok(rows.Select(x => new TopProductResponse(
            x.ProductId, x.Code, x.Name, x.Quantity, x.Gross, x.Orders)).ToArray());
    }

    [HttpGet("inventory-alerts")]
    public async Task<ActionResult<IReadOnlyCollection<InventoryAlertResponse>>> InventoryAlerts(
        [FromQuery] Guid? branchId, [FromQuery] int expiringWithinDays = 30,
        CancellationToken cancellationToken = default)
    {
        if (expiringWithinDays is < 1 or > 365)
            return BadRequest(new { message = "expiringWithinDays phải từ 1 đến 365." });
        var branchError = await ValidateBranch(branchId, cancellationToken);
        if (branchError is not null) return branchError;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(expiringWithinDays);
        var rows = await ScopedInventory(branchId)
            .Where(i => i.QuantityOnHand - i.ReservedQuantity <= i.ReorderLevel ||
                        (i.QuantityOnHand > 0 && i.Batch!.ExpiryDate <= cutoff))
            .OrderBy(i => i.Batch!.ExpiryDate)
            .Select(i => new InventoryAlertResponse(
                i.BranchId, i.Branch!.Code, i.ProductId, i.Product!.Code, i.Product.Name,
                i.BatchId, i.Batch!.BatchNumber, i.Batch.ExpiryDate,
                i.QuantityOnHand, i.ReservedQuantity, i.QuantityOnHand - i.ReservedQuantity,
                i.ReorderLevel,
                i.Batch.ExpiryDate < today ? "EXPIRED" :
                i.Batch.ExpiryDate <= cutoff ? "EXPIRING" : "LOW_STOCK"))
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    private IQueryable<Order> ScopedOrders(Guid? branchId)
    {
        var query = _context.Orders.AsNoTracking().AsQueryable();
        var branches = _accessibleBranches;
        if (branches is not null) query = query.Where(o => branches.Contains(o.BranchId));
        if (branchId.HasValue) query = query.Where(o => o.BranchId == branchId.Value);
        return query;
    }

    private IQueryable<BranchInventory> ScopedInventory(Guid? branchId)
    {
        var query = _context.BranchInventories.AsNoTracking().AsQueryable();
        var branches = _accessibleBranches;
        if (branches is not null) query = query.Where(i => branches.Contains(i.BranchId));
        if (branchId.HasValue) query = query.Where(i => i.BranchId == branchId.Value);
        return query;
    }

    private IQueryable<Prescription> ScopedPrescriptions(Guid? branchId)
    {
        var query = _context.Prescriptions.AsNoTracking().AsQueryable();
        var branches = _accessibleBranches;
        if (branches is not null) query = query.Where(p => branches.Contains(p.BranchId));
        if (branchId.HasValue) query = query.Where(p => p.BranchId == branchId.Value);
        return query;
    }

    private async Task<ActionResult?> ValidateBranch(Guid? branchId, CancellationToken cancellationToken)
    {
        _accessibleBranches = await _branchAccess.GetAccessibleBranchIdsAsync(User, cancellationToken);
        return branchId.HasValue && _accessibleBranches is not null &&
               !_accessibleBranches.Contains(branchId.Value) ? Forbid() : null;
    }

    private static (DateOnly From, DateOnly To, DateTimeOffset StartUtc,
        DateTimeOffset EndExclusiveUtc, string? Error) NormalizeRange(DateOnly? from, DateOnly? to)
    {
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            DateTimeOffset.UtcNow, "Asia/Ho_Chi_Minh").DateTime);
        var end = to ?? localToday;
        var start = from ?? end.AddDays(-29);
        if (end < start) return (start, end, default, default, "Ngày kết thúc phải từ ngày bắt đầu trở đi.");
        if (end.DayNumber - start.DayNumber > 366)
            return (start, end, default, default, "Khoảng báo cáo tối đa 367 ngày.");
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        var startUtc = new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), zone.GetUtcOffset(start.ToDateTime(TimeOnly.MinValue))).ToUniversalTime();
        var next = end.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var endUtc = new DateTimeOffset(next, zone.GetUtcOffset(next)).ToUniversalTime();
        return (start, end, startUtc, endUtc, null);
    }
}
