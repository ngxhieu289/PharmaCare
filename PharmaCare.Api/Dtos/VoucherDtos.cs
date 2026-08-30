using System.ComponentModel.DataAnnotations;

namespace PharmaCare.Api.Dtos;

public sealed class SaveVoucherRequest : IValidatableObject
{
    [Required, MinLength(3), MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string DiscountType { get; set; } = "FIXED_AMOUNT";

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal DiscountValue { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal MinOrderAmount { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal? MaxDiscountAmount { get; set; }

    public DateTimeOffset ValidFrom { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ValidUntil { get; set; }

    [Range(1, int.MaxValue)]
    public int? UsageLimit { get; set; }

    [Range(1, int.MaxValue)]
    public int PerCustomerLimit { get; set; } = 1;

    public Guid? AssignedCustomerId { get; set; }
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ValidUntil.HasValue && ValidUntil <= ValidFrom)
            yield return new ValidationResult("ValidUntil phải sau ValidFrom.", [nameof(ValidUntil)]);
    }
}

public record VoucherResponse(
    Guid Id, string Code, string DiscountType, decimal DiscountValue,
    decimal MinOrderAmount, decimal? MaxDiscountAmount,
    DateTimeOffset ValidFrom, DateTimeOffset? ValidUntil,
    int? UsageLimit, int PerCustomerLimit, int UsedCount,
    Guid? AssignedCustomerId, string? AssignedCustomerName,
    bool IsActive, bool IsCurrentlyValid);

public record VoucherValidationResponse(
    string Code, bool IsValid, decimal DiscountAmount, string? Message);
