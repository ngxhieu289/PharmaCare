using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PharmaCare.Api.Dtos;

public record PrescriptionItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    int ApprovedQuantity,
    string Dosage,
    string? Instructions);

public record PrescriptionResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchCode,
    string BranchName,
    string ImageUrl,
    string PatientName,
    string Status,
    Guid? PharmacistId,
    string? PharmacistName,
    string? PharmacistNote,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<PrescriptionItemResponse> Items);

public sealed class CreatePrescriptionRequest
{
    public Guid BranchId { get; set; }

    [Required, MinLength(2), MaxLength(100)]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    public IFormFile Image { get; set; } = null!;
}

public sealed class ReviewPrescriptionItemRequest
{
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int ApprovedQuantity { get; set; }

    [Required, MinLength(2), MaxLength(255)]
    public string Dosage { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Instructions { get; set; }
}

public sealed class ReviewPrescriptionRequest
{
    public bool Approved { get; set; }

    [MaxLength(1000)]
    public string? PharmacistNote { get; set; }

    public List<ReviewPrescriptionItemRequest> Items { get; set; } = [];
}
