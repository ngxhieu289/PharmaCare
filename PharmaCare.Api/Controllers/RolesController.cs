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
}
