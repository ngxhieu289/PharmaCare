using System.Security.Claims;
using System.Text.Json;
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
[Route("api/prescriptions")]
public class PrescriptionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IBranchAccessService _branchAccess;
    private readonly IPrescriptionFileStorage _fileStorage;

    public PrescriptionsController(
        AppDbContext context,
        IBranchAccessService branchAccess,
        IPrescriptionFileStorage fileStorage)
    {
        _context = context;
        _branchAccess = branchAccess;
        _fileStorage = fileStorage;
    }

    [Authorize(Policy = PermissionCodes.PrescriptionsRead)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<PrescriptionResponse>>> GetPrescriptions(
        [FromQuery] string? status,
        [FromQuery] Guid? branchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = _context.Prescriptions.AsNoTracking().AsQueryable();
        if (!User.IsInRole("Admin"))
        {
            var accessibleBranches = await _branchAccess
                .GetAccessibleBranchIdsAsync(User, cancellationToken) ?? Array.Empty<Guid>();
            query = query.Where(p => p.CustomerId == userId || accessibleBranches.Contains(p.BranchId));
        }
        if (branchId.HasValue)
        {
            var canAccessBranch = User.IsInRole("Admin") ||
                                  await _branchAccess.CanAccessAsync(User, branchId.Value, cancellationToken);
            if (!canAccessBranch && !await query.AnyAsync(
                    p => p.BranchId == branchId && p.CustomerId == userId, cancellationToken))
            {
                return Forbid();
            }
            query = query.Where(p => p.BranchId == branchId);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            if (!IsValidStatus(normalizedStatus))
            {
                return BadRequest(new { message = "Trạng thái đơn thuốc không hợp lệ." });
            }
            query = query.Where(p => p.Status == normalizedStatus);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await Project(query.OrderByDescending(p => p.CreatedAt))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(PagedResponse<PrescriptionResponse>.Create(items, page, pageSize, totalItems));
    }

    [Authorize(Policy = PermissionCodes.PrescriptionsRead)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PrescriptionResponse>> GetPrescription(
        Guid id,
        CancellationToken cancellationToken)
    {
        var access = await FindAccessible(id, cancellationToken);
        if (access is null)
        {
            return NotFound();
        }

        var response = await Project(_context.Prescriptions.AsNoTracking().Where(p => p.Id == id))
            .SingleAsync(cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = PermissionCodes.PrescriptionsCreate)]
    [Consumes("multipart/form-data")]
    [HttpPost]
    public async Task<ActionResult<PrescriptionResponse>> CreatePrescription(
        [FromForm] CreatePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        if (!await _context.Branches.AnyAsync(
            b => b.Id == request.BranchId && b.IsActive, cancellationToken))
        {
            return BadRequest(new { message = "Chi nhánh không tồn tại hoặc đã ngừng hoạt động." });
        }

        string storedName;
        try
        {
            storedName = await _fileStorage.SaveAsync(request.Image, cancellationToken);
        }
        catch (PrescriptionFileException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        var prescription = new Prescription
        {
            CustomerId = userId,
            BranchId = request.BranchId,
            ImageUrl = storedName,
            PatientName = request.PatientName.Trim(),
            Status = PrescriptionStatuses.Pending,
            Version = 1
        };

        try
        {
            _context.Prescriptions.Add(prescription);
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "PRESCRIPTION_CREATE",
                EntityName = nameof(Prescription),
                EntityId = prescription.Id.ToString(),
                NewValues = JsonSerializer.Serialize(new
                {
                    prescription.BranchId,
                    prescription.PatientName,
                    prescription.Status
                }),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _fileStorage.DeleteAsync(storedName);
            throw;
        }

        var response = await Project(
                _context.Prescriptions.AsNoTracking().Where(p => p.Id == prescription.Id))
            .SingleAsync(cancellationToken);
        return CreatedAtAction(nameof(GetPrescription), new { id = prescription.Id }, response);
    }

    [Authorize(Policy = PermissionCodes.PrescriptionsRead)]
    [HttpGet("{id:guid}/image")]
    public async Task<IActionResult> GetImage(Guid id, CancellationToken cancellationToken)
    {
        var prescription = await FindAccessible(id, cancellationToken);
        if (prescription is null)
        {
            return NotFound();
        }

        var file = await _fileStorage.OpenReadAsync(prescription.ImageUrl, cancellationToken);
        return file is null
            ? NotFound(new { message = "Không tìm thấy file đơn thuốc." })
            : File(file.Stream, file.ContentType, file.DownloadName, enableRangeProcessing: true);
    }

    [Authorize(Policy = PermissionCodes.PrescriptionsReview)]
    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> Review(
        Guid id,
        ReviewPrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var pharmacistId))
        {
            return Unauthorized();
        }

        var prescription = await _context.Prescriptions
            .Include(p => p.Items)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (prescription is null)
        {
            return NotFound();
        }
        if (!await _branchAccess.CanAccessAsync(User, prescription.BranchId, cancellationToken))
        {
            return Forbid();
        }
        if (prescription.Status != PrescriptionStatuses.Pending)
        {
            return Conflict(new { message = "Đơn thuốc đã được xử lý trước đó." });
        }

        if (request.Approved)
        {
            if (request.Items.Count == 0)
            {
                return BadRequest(new { message = "Đơn được duyệt phải có ít nhất một sản phẩm." });
            }
            if (request.Items.Select(i => i.ProductId).Distinct().Count() != request.Items.Count)
            {
                return BadRequest(new { message = "Danh sách duyệt có sản phẩm bị trùng." });
            }

            var productIds = request.Items.Select(i => i.ProductId).ToArray();
            var validProductCount = await _context.Products.CountAsync(
                p => productIds.Contains(p.Id) && p.IsActive, cancellationToken);
            if (validProductCount != productIds.Length)
            {
                return BadRequest(new { message = "Có sản phẩm không tồn tại hoặc đã ngừng hoạt động." });
            }

            foreach (var item in request.Items)
            {
                _context.PrescriptionItems.Add(new PrescriptionItem
                {
                    PrescriptionId = prescription.Id,
                    ProductId = item.ProductId,
                    ApprovedQuantity = item.ApprovedQuantity,
                    Dosage = item.Dosage.Trim(),
                    Instructions = Clean(item.Instructions)
                });
            }
        }
        else if (string.IsNullOrWhiteSpace(request.PharmacistNote))
        {
            return BadRequest(new { message = "Phải ghi lý do khi từ chối đơn thuốc." });
        }

        var oldStatus = prescription.Status;
        prescription.Status = request.Approved
            ? PrescriptionStatuses.Approved
            : PrescriptionStatuses.Rejected;
        prescription.PharmacistId = pharmacistId;
        prescription.PharmacistNote = Clean(request.PharmacistNote);
        prescription.ReviewedAt = DateTimeOffset.UtcNow;
        prescription.Version++;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = pharmacistId,
            Action = request.Approved ? "PRESCRIPTION_APPROVE" : "PRESCRIPTION_REJECT",
            EntityName = nameof(Prescription),
            EntityId = prescription.Id.ToString(),
            OldValues = JsonSerializer.Serialize(new { Status = oldStatus }),
            NewValues = JsonSerializer.Serialize(new
            {
                prescription.Status,
                prescription.PharmacistNote,
                ItemCount = request.Approved ? request.Items.Count : 0
            }),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Đơn thuốc vừa được xử lý bởi người khác." });
        }
        return NoContent();
    }

    private async Task<Prescription?> FindAccessible(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return null;
        }

        var prescription = await _context.Prescriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (prescription is null)
        {
            return null;
        }
        if (User.IsInRole("Admin") || prescription.CustomerId == userId ||
            await _branchAccess.CanAccessAsync(User, prescription.BranchId, cancellationToken))
        {
            return prescription;
        }
        return null;
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static bool IsValidStatus(string status) =>
        status is PrescriptionStatuses.Pending or
            PrescriptionStatuses.Approved or PrescriptionStatuses.Rejected;

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<PrescriptionResponse> Project(IQueryable<Prescription> query) =>
        query.Select(p => new PrescriptionResponse(
            p.Id, p.CustomerId, p.Customer!.DisplayName,
            p.BranchId, p.Branch!.Code, p.Branch.Name,
            $"/api/prescriptions/{p.Id}/image",
            p.PatientName, p.Status,
            p.PharmacistId, p.Pharmacist == null ? null : p.Pharmacist.DisplayName,
            p.PharmacistNote, p.ReviewedAt, p.CreatedAt,
            p.Items.OrderBy(i => i.Product!.Name)
                .Select(i => new PrescriptionItemResponse(
                    i.Id, i.ProductId, i.Product!.Code, i.Product.Name,
                    i.ApprovedQuantity, i.Dosage, i.Instructions))
                .ToArray()));
}
