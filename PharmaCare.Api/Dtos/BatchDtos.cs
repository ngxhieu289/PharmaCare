using System.ComponentModel.DataAnnotations;

namespace PharmaCare.Api.Dtos;

public record BatchResponse(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string BatchNumber,
    DateOnly MfgDate,
    DateOnly ExpiryDate,
    decimal CostPrice,
    bool IsExpired);

public sealed class SaveBatchRequest
{
    public Guid ProductId { get; set; }

    [Required, MinLength(1), MaxLength(50)]
    public string BatchNumber { get; set; } = string.Empty;

    public DateOnly MfgDate { get; set; }
    public DateOnly ExpiryDate { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal CostPrice { get; set; }
}
