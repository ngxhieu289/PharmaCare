using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaCare.Api.DTOs.Auth;
using PharmaCare.Api.Services;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Api.Data;

namespace PharmaCare.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly AppDbContext _context;
    public AuthController(IAuthService auth, AppDbContext context)
    {
        _auth = auth;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _auth.LoginAsync(request, ip);
        return result is null ? Unauthorized(new { error = "Invalid credentials" }) : Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _auth.RegisterAsync(request, ip);
        return result is null ? BadRequest(new { error = "Email already exists" }) : Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _auth.RefreshTokenAsync(request.RefreshToken, ip);
        return result is null ? Unauthorized(new { error = "Invalid refresh token" }) : Ok(result);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _auth.RevokeTokenAsync(request.RefreshToken, ip);
        return Ok(new { message = "Token revoked" });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var changed = await _auth.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        return changed
            ? Ok(new { message = "Password changed. Please sign in again." })
            : BadRequest(new { error = "Current password is incorrect" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var user = await _context.Users.AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new CurrentUserResponse(
                u.Id, u.Email, u.Username, u.DisplayName, u.Phone,
                u.UserRoles.Select(ur => ur.Role.Name).Distinct().OrderBy(name => name).ToArray(),
                u.UserRoles.SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.Code).Distinct().OrderBy(code => code).ToArray(),
                u.UserBranches.OrderByDescending(ub => ub.IsPrimary).ThenBy(ub => ub.Branch.Name)
                    .Select(ub => new CurrentUserBranchResponse(
                        ub.BranchId, ub.Branch.Code, ub.Branch.Name, ub.IsPrimary)).ToArray()))
            .SingleOrDefaultAsync(cancellationToken);

        return user is null ? Unauthorized() : Ok(user);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var user = await _context.Users.FindAsync([userId], cancellationToken);
        if (user is null || !user.IsActive) return Unauthorized();

        user.DisplayName = request.DisplayName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
