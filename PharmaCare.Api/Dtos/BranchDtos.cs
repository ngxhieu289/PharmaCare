using System.ComponentModel.DataAnnotations;

namespace PharmaCare.Api.Dtos;

public record BranchResponse(
    Guid Id,
    string Code,
    string Name,
    string Address,
    string? Phone,
    string? Province,
    string? District,
    string? Ward,
    bool IsActive);

public sealed class SaveBranchRequest
{
    [Required, MinLength(2), MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, MinLength(2), MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required, MinLength(5), MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }

    [MaxLength(100)]
    public string? District { get; set; }

    [MaxLength(100)]
    public string? Ward { get; set; }
}
