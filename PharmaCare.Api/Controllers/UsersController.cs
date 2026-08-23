using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Api.Data;
using PharmaCare.Api.Dtos;
using PharmaCare.Api.Entities;
using PharmaCare.Api.Authorization;

namespace PharmaCare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/users (Lấy danh sách Người dùng + Roles)
    [Authorize(Policy = PermissionCodes.UsersRead)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers()
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Select(u => new UserResponse(
                u.Id,
                u.Email,
                u.DisplayName,
                u.Phone,
                u.IsActive,
                u.CreatedAt,
                u.UserRoles.Select(ur => ur.Role.Name).ToList()
            ))
            .ToListAsync();

        return Ok(users);
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
            Array.Empty<string>()
        );

        return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, response);
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

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PUT: api/users/{userId}/branches/{branchId}?isPrimary=true
    [Authorize(Policy = PermissionCodes.BranchesManage)]
    [HttpPut("{userId}/branches/{branchId}")]
    public async Task<IActionResult> AssignBranch(Guid userId, Guid branchId, [FromQuery] bool isPrimary = false)
    {
        if (!await _context.Users.AnyAsync(u => u.Id == userId))
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
}
