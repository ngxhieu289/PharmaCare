using System.ComponentModel.DataAnnotations;

namespace PharmaCare.Api.Dtos;

public record ProductResponse(
    Guid Id,
    string Code,
    string Name,
    string? ActiveIngredient,
    string? Indications,
    string? Brand,
    string? RegistrationNumber,
    string? DosageForm,
    string? Manufacturer,
    string? CountryOfOrigin,
    string? ShelfLife,
    string? Composition,
    string? UsageInstructions,
    string? Contraindications,
    string? SideEffects,
    Guid CategoryId,
    string CategoryName,
    bool RxFlag,
    decimal VatRate,
    string Packaging,
    decimal UnitPrice,
    string? StorageTemp,
    string? WarningText,
    string? ImageUrl,
    bool IsActive,
    IReadOnlyCollection<ProductSaleUnitResponse> SaleUnits);

public record ProductSaleUnitResponse(
    Guid Id, string UnitName, int ConversionFactor,
    decimal SalePrice, bool IsDefault, bool IsActive);

public record ProductAvailabilityResponse(
    Guid ProductId,
    Guid BranchId,
    int AvailableQuantity,
    string Status,
    Guid? SaleUnitId = null,
    string? UnitName = null);

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

    [MaxLength(150)] public string? Brand { get; set; }
    [MaxLength(100)] public string? RegistrationNumber { get; set; }
    [MaxLength(150)] public string? DosageForm { get; set; }
    [MaxLength(255)] public string? Manufacturer { get; set; }
    [MaxLength(100)] public string? CountryOfOrigin { get; set; }
    [MaxLength(100)] public string? ShelfLife { get; set; }
    [MaxLength(2000)] public string? Composition { get; set; }
    [MaxLength(2000)] public string? UsageInstructions { get; set; }
    [MaxLength(2000)] public string? Contraindications { get; set; }
    [MaxLength(2000)] public string? SideEffects { get; set; }

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

    [MaxLength(2048)]
    public string? ImageUrl { get; set; }

    [MinLength(1)]
    public List<SaveProductSaleUnitRequest> SaleUnits { get; set; } = [];
}

public sealed class SaveProductSaleUnitRequest
{
    public Guid? Id { get; set; }
    [Required, MinLength(1), MaxLength(30)] public string UnitName { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int ConversionFactor { get; set; } = 1;
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] public decimal SalePrice { get; set; }
    public bool IsDefault { get; set; }
}
