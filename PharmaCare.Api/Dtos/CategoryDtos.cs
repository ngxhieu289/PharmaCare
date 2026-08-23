using System.ComponentModel.DataAnnotations;

namespace PharmaCare.Api.Dtos;

public record CategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentId,
    string? ParentName,
    bool IsActive);

public sealed class SaveCategoryRequest
{
    [Required, MinLength(2), MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MinLength(2), MaxLength(120)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug chỉ gồm chữ thường không dấu, số và dấu gạch ngang.")]
    public string Slug { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }
}
