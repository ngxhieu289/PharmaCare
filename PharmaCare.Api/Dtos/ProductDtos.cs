using System.ComponentModel.DataAnnotations;

namespace PharmaCare.Api.Dtos;

public record ProductResponse(
    Guid Id,
    string Code,
    string Name,
    string? ActiveIngredient,
    string? Indications,
    Guid CategoryId,
    string CategoryName,
    bool RxFlag,
    decimal VatRate,
    string Packaging,
    decimal UnitPrice,
    string? StorageTemp,
    string? WarningText,
    bool IsActive);

public sealed class SaveProductRequest
{
    [Required, MinLength(2), MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, MinLength(2), MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? ActiveIngredient { get; set; }

    [MaxLength(500)]
    public string? Indications { get; set; }

    public Guid CategoryId { get; set; }
    public bool RxFlag { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal VatRate { get; set; } = 5m;

    [Required, MaxLength(255)]
    public string Packaging { get; set; } = "Hộp";

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal UnitPrice { get; set; }

    [MaxLength(100)]
    public string? StorageTemp { get; set; }

    [MaxLength(1000)]
    public string? WarningText { get; set; }
}
