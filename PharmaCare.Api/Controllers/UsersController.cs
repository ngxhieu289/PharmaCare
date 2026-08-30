using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Api.Data;
using PharmaCare.Api.Dtos;
using PharmaCare.Api.Entities;
using PharmaCare.Api.Authorization;
using PharmaCare.Api.Dtos.Common;
using PharmaCare.Api.Services;

namespace PharmaCare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IUserSessionService _userSessionService;

    public UsersController(AppDbContext context, IUserSessionService userSessionService)
    {
        _context = context;
        _userSessionService = userSessionService;
    }

    // GET: api/users (Lấy danh sách Người dùng + Roles)
    [Authorize(Policy = PermissionCodes.UsersRead)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = _context.Users.AsNoTracking().AsQueryable();
        if (isActive.HasValue) query = query.Where(u => u.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u => EF.Functions.ILike(u.Email, pattern) ||
                                     EF.Functions.ILike(u.DisplayName, pattern) ||
                                     (u.Phone != null && EF.Functions.ILike(u.Phone, pattern)));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var users = await Project(query.OrderByDescending(u => u.CreatedAt))
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(PagedResponse<UserResponse>.Create(users, page, pageSize, totalItems));
    }

    [Authorize(Policy = PermissionCodes.UsersRead)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await Project(_context.Users.AsNoTracking().Where(u => u.Id == id))
            .SingleOrDefaultAsync(cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    // POST: api/users (Tạo User mới + Hash Mật khẩu)
    [Authorize(Policy = PermissionCodes.UsersManage)]
    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser(CreateUserRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail))
        {
            return Conflict(new { message = "Email này đã được sử dụng." });
        }

        var user = new User
        {
            Email = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            Phone = request.Phone?.Trim()
        };

        // Mã hóa mật khẩu chuẩn PasswordHasher (Không lưu plain-text)
        var passwordHasher = new PasswordHasher<User>();
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var response = new UserResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Phone,
            user.IsActive,
            user.CreatedAt,
            Array.Empty<string>(),
            Array.Empty<UserBranchResponse>()
        );

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
    }

    [Authorize(Policy = PermissionCodes.UsersManage)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync([id], cancellationToken);
        if (user is null) return NotFound();
        user.DisplayName = request.DisplayName.Trim();
        user.Phone = Clean(request.Phone);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = PermissionCodes.UsersManage)]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, SetActiveRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync([id], cancellationToken);
        if (user is null) return NotFound();

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!request.IsActive && currentUserId == id.ToString())
        {
            return Conflict(new { message = "Không thể tự khóa tài khoản đang đăng nhập." });
        }

        if (user.IsActive != request.IsActive)
        {
            user.IsActive = request.IsActive;
            await _userSessionService.InvalidateUserAsync(
                user,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);
        }
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

        // PUT: api/users/{userId}/roles/{roleId} (Gán Role cho User)
    [Authorize(Policy = PermissionCodes.RolesManage)]
    [HttpPut("{userId}/roles/{roleId}")]
    public async Task<IActionResult> AssignRole(Guid userId, Guid roleId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "Không tìm thấy User" });

        var role = await _context.Roles.FindAsync(roleId);
        if (role == null) return NotFound(new { message = "Không tìm thấy Role" });

        // Kiểm tra xem User đã có Role này chưa
        var userRoleExists = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (!userRoleExists)
        {
            _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            await _userSessionService.InvalidateUserAsync(
                user,
                HttpContext.Connection.RemoteIpAddress?.ToString());
            await _context.SaveChangesAsync();
        }

        return NoContent(); // 204 No Content
    }

    // DELETE: api/users/{userId}/roles/{roleId} (Gỡ Role khỏi User)
    [Authorize(Policy = PermissionCodes.RolesManage)]
    [HttpDelete("{userId}/roles/{roleId}")]
    public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole == null) return NotFound();

        var user = await _context.Users.FindAsync(userId);
        if (user is null) return NotFound();

        _context.UserRoles.Remove(userRole);
        await _userSessionService.InvalidateUserAsync(
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString());
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PUT: api/users/{userId}/branches/{branchId}?isPrimary=true
    [Authorize(Policy = PermissionCodes.BranchesManage)]
    [HttpPut("{userId}/branches/{branchId}")]
    public async Task<IActionResult> AssignBranch(Guid userId, Guid branchId, [FromQuery] bool isPrimary = false)
    {
        var user = await _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return NotFound(new { message = "Không tìm thấy User" });
        }

        if (!await _context.Branches.AnyAsync(b => b.Id == branchId && b.IsActive))
        {
            return NotFound(new { message = "Không tìm thấy chi nhánh đang hoạt động" });
        }

        var assignments = await _context.UserBranches
            .Where(ub => ub.UserId == userId)
            .ToListAsync();

        var isBranchManager = user.UserRoles.Any(ur => ur.Role.Name == "BranchManager");
        if (isBranchManager)
        {
            _context.UserBranches.RemoveRange(assignments.Where(ub => ub.BranchId != branchId));
            assignments = assignments.Where(ub => ub.BranchId == branchId).ToList();
            isPrimary = true;
        }

        var assignment = assignments.SingleOrDefault(ub => ub.BranchId == branchId);
        if (assignment is null)
        {
            assignment = new UserBranch { UserId = userId, BranchId = branchId };
            _context.UserBranches.Add(assignment);
        }

        if (isPrimary)
        {
            foreach (var item in assignments)
            {
                item.IsPrimary = false;
            }
        }
        assignment.IsPrimary = isPrimary;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Policy = PermissionCodes.BranchesManage)]
    [HttpDelete("{userId}/branches/{branchId}")]
    public async Task<IActionResult> RemoveBranch(Guid userId, Guid branchId)
    {
        var assignment = await _context.UserBranches.FindAsync(userId, branchId);
        if (assignment is null)
        {
            return NotFound();
        }

        _context.UserBranches.Remove(assignment);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static IQueryable<UserResponse> Project(IQueryable<User> query) =>
        query.Select(u => new UserResponse(
            u.Id, u.Email, u.DisplayName, u.Phone, u.IsActive, u.CreatedAt,
            u.UserRoles.Select(ur => ur.Role.Name).OrderBy(name => name).ToArray(),
            u.UserBranches.OrderByDescending(ub => ub.IsPrimary).ThenBy(ub => ub.Branch.Name)
                .Select(ub => new UserBranchResponse(ub.BranchId, ub.Branch.Code, ub.Branch.Name, ub.IsPrimary))
                .ToArray()));

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
