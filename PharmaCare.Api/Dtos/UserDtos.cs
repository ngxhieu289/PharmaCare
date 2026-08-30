using System.ComponentModel.DataAnnotations;

namespace PharmaCare.Api.Dtos;

// Chuyển sang Class để tương thích 100% với Model Binding trong ASP.NET Core
public class CreateUserRequest
{
    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên hiển thị không được để trống")]
    [MinLength(2, ErrorMessage = "Tên hiển thị phải từ 2 ký tự")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải từ 6 ký tự")]
    public string Password { get; set; } = string.Empty;

    public string? Phone { get; set; }
}

public record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? Phone,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<UserBranchResponse> Branches
);

public record UserBranchResponse(Guid Id, string Code, string Name, bool IsPrimary);

public sealed class UpdateUserRequest
{
    [Required, MinLength(2), MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }
}
