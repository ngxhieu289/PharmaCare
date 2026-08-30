using System.ComponentModel.DataAnnotations;

namespace PharmaCare.Api.DTOs.Auth;

public sealed class LoginRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(3), MaxLength(50), RegularExpression("^[a-zA-Z0-9._-]+$")]
    public string Username { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, MinLength(2), MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }
}

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string[] Roles,
    string[] Permissions);
public sealed class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class UpdateProfileRequest
{
    [Required, MinLength(2), MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }
}

public record CurrentUserResponse(
    Guid Id,
    string Email,
    string? Username,
    string DisplayName,
    string? Phone,
    string[] Roles,
    string[] Permissions,
    IReadOnlyCollection<CurrentUserBranchResponse> Branches);

public record CurrentUserBranchResponse(Guid Id, string Code, string Name, bool IsPrimary);
