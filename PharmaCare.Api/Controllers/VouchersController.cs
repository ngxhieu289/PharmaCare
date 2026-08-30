using System.Security.Claims;
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
[Authorize]
[Route("api/vouchers")]
public class VouchersController : ControllerBase
{
    private readonly AppDbContext _context;
    public VouchersController(AppDbContext context) => _context = context;

    [Authorize(Policy = PermissionCodes.VouchersManage)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<VoucherResponse>>> GetAll(
        [FromQuery] string? search, [FromQuery] bool? active,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = _context.Vouchers.AsNoTracking().AsQueryable();
        if (active.HasValue) query = query.Where(v => v.IsActive == active);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(v => EF.Functions.ILike(v.Code, pattern));
        }
        var count = await query.CountAsync(cancellationToken);
        var items = await Project(query.OrderByDescending(v => v.ValidFrom))
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(PagedResponse<VoucherResponse>.Create(items, page, pageSize, count));
    }

    [Authorize(Policy = PermissionCodes.VouchersManage)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VoucherResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await Project(_context.Vouchers.AsNoTracking().Where(v => v.Id == id))
            .SingleOrDefaultAsync(cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionCodes.VouchersManage)]
    [HttpPost]
    public async Task<ActionResult<VoucherResponse>> Create(
        SaveVoucherRequest request, CancellationToken cancellationToken)
    {
        var error = await Validate(request, null, cancellationToken);
        if (error is not null) return error;
        var voucher = new Voucher();
        Map(request, voucher);
        _context.Vouchers.Add(voucher);
        await _context.SaveChangesAsync(cancellationToken);
        var response = await Project(_context.Vouchers.AsNoTracking().Where(v => v.Id == voucher.Id))
            .SingleAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = voucher.Id }, response);
    }

    [Authorize(Policy = PermissionCodes.VouchersManage)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, SaveVoucherRequest request, CancellationToken cancellationToken)
    {
        var voucher = await _context.Vouchers.FindAsync([id], cancellationToken);
        if (voucher is null) return NotFound();
        if (voucher.UsedCount > 0)
            return Conflict(new { message = "Voucher đã được sử dụng; chỉ có thể đổi trạng thái hoạt động." });
        var error = await Validate(request, id, cancellationToken);
        if (error is not null) return error;
        Map(request, voucher);
        voucher.Version++;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = PermissionCodes.VouchersManage)]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id, SetActiveRequest request, CancellationToken cancellationToken)
    {
        var voucher = await _context.Vouchers.FindAsync([id], cancellationToken);
        if (voucher is null) return NotFound();
        voucher.IsActive = request.IsActive;
        voucher.Version++;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("validate/{code}")]
    public async Task<ActionResult<VoucherValidationResponse>> ValidateCode(
        string code,
        [FromQuery] decimal orderAmount,
        [FromQuery] string? customerPhone,
        CancellationToken cancellationToken)
    {
        Guid? customerId = null;
        var phone = string.IsNullOrWhiteSpace(customerPhone) ? null : customerPhone.Trim();
        if (phone is null &&
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var authenticatedCustomerId))
        {
            customerId = authenticatedCustomerId;
        }
        if (!customerId.HasValue && phone is null)
        {
            return Ok(new VoucherValidationResponse(
                code.Trim().ToUpperInvariant(), false, 0,
                "Khách vãng lai cần nhập số điện thoại để sử dụng voucher."));
        }

        var voucher = await _context.Vouchers.AsNoTracking()
            .SingleOrDefaultAsync(v => v.Code == code.Trim().ToUpper(), cancellationToken);
        var message = GetInvalidReason(voucher, customerId, orderAmount, DateTimeOffset.UtcNow);
        if (message is not null)
            return Ok(new VoucherValidationResponse(code.ToUpperInvariant(), false, 0, message));

        var usages = _context.VoucherUsages.AsNoTracking().Where(
            u => u.VoucherId == voucher!.Id && u.Status == VoucherUsageStatuses.Redeemed);
        usages = customerId.HasValue
            ? usages.Where(u => u.CustomerId == customerId.Value)
            : usages.Where(u => u.Customer.IsGuest && u.Customer.Phone == phone);
        var used = await usages.CountAsync(cancellationToken);
        if (used >= voucher!.PerCustomerLimit)
            return Ok(new VoucherValidationResponse(voucher.Code, false, 0, "Đã hết lượt dùng của khách hàng."));
        var discount = voucher.DiscountType == VoucherDiscountTypes.Percentage
            ? Math.Round(orderAmount * voucher.DiscountValue / 100m, 2, MidpointRounding.AwayFromZero)
            : voucher.DiscountValue;
        if (voucher.MaxDiscountAmount.HasValue) discount = Math.Min(discount, voucher.MaxDiscountAmount.Value);
        discount = Math.Min(discount, orderAmount);
        return Ok(new VoucherValidationResponse(voucher.Code, true, discount, null));
    }

    private async Task<ActionResult?> Validate(
        SaveVoucherRequest request, Guid? id, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (request.DiscountType.Trim().ToUpperInvariant() is not
            (VoucherDiscountTypes.FixedAmount or VoucherDiscountTypes.Percentage))
            return BadRequest(new { message = "Loại giảm giá không hợp lệ." });
        if (request.DiscountType.Equals(VoucherDiscountTypes.Percentage, StringComparison.OrdinalIgnoreCase) &&
            request.DiscountValue > 100)
            return BadRequest(new { message = "Phần trăm giảm không được vượt quá 100%." });
        if (await _context.Vouchers.AnyAsync(v => v.Id != id && v.Code == code, cancellationToken))
            return Conflict(new { message = "Mã voucher đã tồn tại." });
        if (request.AssignedCustomerId.HasValue && !await _context.Users.AnyAsync(
                u => u.Id == request.AssignedCustomerId && u.IsActive, cancellationToken))
            return BadRequest(new { message = "Khách hàng được gán không hợp lệ." });
        return null;
    }

    private static string? GetInvalidReason(Voucher? voucher, Guid? customerId, decimal amount, DateTimeOffset now)
    {
        if (voucher is null) return "Voucher không tồn tại.";
        if (!voucher.IsActive || voucher.ValidFrom > now ||
            (voucher.ValidUntil.HasValue && voucher.ValidUntil <= now)) return "Voucher chưa có hiệu lực hoặc đã hết hạn.";
        if (voucher.AssignedCustomerId.HasValue && voucher.AssignedCustomerId != customerId) return "Voucher không thuộc khách hàng này.";
        if (amount < voucher.MinOrderAmount) return "Chưa đạt giá trị đơn tối thiểu.";
        if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit) return "Voucher đã hết lượt sử dụng.";
        return null;
    }

    private static IQueryable<VoucherResponse> Project(IQueryable<Voucher> query)
    {
        var now = DateTimeOffset.UtcNow;
        return query.Select(v => new VoucherResponse(
            v.Id, v.Code, v.DiscountType, v.DiscountValue, v.MinOrderAmount,
            v.MaxDiscountAmount, v.ValidFrom, v.ValidUntil, v.UsageLimit,
            v.PerCustomerLimit, v.UsedCount, v.AssignedCustomerId,
            v.AssignedCustomer == null ? null : v.AssignedCustomer.DisplayName,
            v.IsActive, v.IsActive && v.ValidFrom <= now &&
                        (!v.ValidUntil.HasValue || v.ValidUntil > now) &&
                        (!v.UsageLimit.HasValue || v.UsedCount < v.UsageLimit)));
    }

    private static void Map(SaveVoucherRequest request, Voucher voucher)
    {
        voucher.Code = request.Code.Trim().ToUpperInvariant();
        voucher.DiscountType = request.DiscountType.Trim().ToUpperInvariant();
        voucher.DiscountValue = request.DiscountValue;
        voucher.MinOrderAmount = request.MinOrderAmount;
        voucher.MaxDiscountAmount = request.MaxDiscountAmount;
        voucher.ValidFrom = request.ValidFrom.ToUniversalTime();
        voucher.ValidUntil = request.ValidUntil?.ToUniversalTime();
        voucher.UsageLimit = request.UsageLimit;
        voucher.PerCustomerLimit = request.PerCustomerLimit;
        voucher.AssignedCustomerId = request.AssignedCustomerId;
        voucher.IsActive = request.IsActive;
    }
}
