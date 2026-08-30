using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Api.Data;
using PharmaCare.Api.Entities;
using PharmaCare.Api.Authorization;
using PharmaCare.Api.Dtos;

namespace PharmaCare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _context;

    public RolesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/roles (Lấy danh sách 5 Roles mặc định + Permissions)
    [Authorize(Policy = PermissionCodes.RolesRead)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleResponse>>> GetRoles()
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .Select(r => new RoleResponse(
                r.Id,
                r.Name,
                r.Description,
                r.RolePermissions
                    .Select(rp => rp.Permission.Code)
                    .OrderBy(code => code)
                    .ToArray()))
            .ToListAsync();

        return Ok(roles);
    }

    [Authorize(Policy = PermissionCodes.RolesRead)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleResponse>> GetRole(Guid id)
    {
        var role = await Project(_context.Roles.AsNoTracking().Where(r => r.Id == id))
            .SingleOrDefaultAsync();
        return role is null ? NotFound() : Ok(role);
    }

    [Authorize(Policy = PermissionCodes.RolesRead)]
    [HttpGet("/api/permissions")]
    public async Task<ActionResult<IEnumerable<PermissionResponse>>> GetPermissions() =>
        Ok(await _context.Permissions.AsNoTracking().OrderBy(p => p.Code)
            .Select(p => new PermissionResponse(p.Id, p.Code, p.Description)).ToListAsync());

    [Authorize(Policy = PermissionCodes.RolesManage)]
    [HttpPost]
    public async Task<ActionResult<RoleResponse>> CreateRole(SaveRoleRequest request)
    {
        var name = request.Name.Trim();
        if (await _context.Roles.AnyAsync(r => r.Name.ToLower() == name.ToLower()))
            return Conflict(new { message = "Tên vai trò đã tồn tại." });

        var permissionIds = request.PermissionIds.Distinct().ToArray();
        var permissions = await _context.Permissions.Where(p => permissionIds.Contains(p.Id)).ToListAsync();
        if (permissions.Count != permissionIds.Length)
            return BadRequest(new { message = "Có quyền không tồn tại." });

        var role = new Role { Name = name, Description = Clean(request.Description) };
        foreach (var permission in permissions)
            role.RolePermissions.Add(new RolePermission { Role = role, Permission = permission });
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetRole), new { id = role.Id },
            new RoleResponse(role.Id, role.Name, role.Description, permissions.Select(p => p.Code).Order().ToArray()));
    }

    [Authorize(Policy = PermissionCodes.RolesManage)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRole(Guid id, SaveRoleRequest request)
    {
        var role = await _context.Roles.Include(r => r.RolePermissions).SingleOrDefaultAsync(r => r.Id == id);
        if (role is null) return NotFound();
        if (role.Name == "Admin") return Conflict(new { message = "Không thể sửa vai trò Admin hệ thống." });

        var name = request.Name.Trim();
        if (SystemRoleNames.Contains(role.Name) && name != role.Name)
            return Conflict(new { message = "Không thể đổi tên vai trò hệ thống." });
        if (await _context.Roles.AnyAsync(r => r.Id != id && r.Name.ToLower() == name.ToLower()))
            return Conflict(new { message = "Tên vai trò đã tồn tại." });

        var permissionIds = request.PermissionIds.Distinct().ToArray();
        var permissions = await _context.Permissions.Where(p => permissionIds.Contains(p.Id)).ToListAsync();
        if (permissions.Count != permissionIds.Length)
            return BadRequest(new { message = "Có quyền không tồn tại." });

        role.Name = name;
        role.Description = Clean(request.Description);
        var requestedIds = permissions.Select(p => p.Id).ToHashSet();
        var removedMappings = role.RolePermissions
            .Where(rp => !requestedIds.Contains(rp.PermissionId)).ToList();
        _context.RolePermissions.RemoveRange(removedMappings);
        var existingIds = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        foreach (var permissionId in requestedIds.Where(permissionId => !existingIds.Contains(permissionId)))
        {
            role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
        }
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Policy = PermissionCodes.RolesManage)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var role = await _context.Roles.Include(r => r.UserRoles).SingleOrDefaultAsync(r => r.Id == id);
        if (role is null) return NotFound();
        if (SystemRoleNames.Contains(role.Name))
            return Conflict(new { message = "Không thể xóa vai trò hệ thống." });
        if (role.UserRoles.Count != 0)
            return Conflict(new { message = "Không thể xóa vai trò đang được gán cho người dùng." });
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static readonly HashSet<string> SystemRoleNames =
        ["Admin", "BranchManager", "Pharmacist", "WarehouseStaff", "Customer"];

    private static IQueryable<RoleResponse> Project(IQueryable<Role> query) =>
        query.Select(r => new RoleResponse(r.Id, r.Name, r.Description,
            r.RolePermissions.Select(rp => rp.Permission.Code).OrderBy(code => code).ToArray()));

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
