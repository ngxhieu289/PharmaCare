namespace PharmaCare.Api.Dtos;

public record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyCollection<string> Permissions);

public record PermissionResponse(Guid Id, string Code, string? Description);

public sealed class SaveRoleRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MinLength(2)]
    [System.ComponentModel.DataAnnotations.MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(255)]
    public string? Description { get; set; }

    public IReadOnlyCollection<Guid> PermissionIds { get; set; } = Array.Empty<Guid>();
}
