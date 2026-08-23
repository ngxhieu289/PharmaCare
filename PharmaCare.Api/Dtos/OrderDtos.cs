using System.ComponentModel.DataAnnotations;

namespace PharmaCare.Api.Dtos;

public sealed class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }
    public Guid? SaleUnitId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public sealed class CreateOrderRequest
{
    public Guid BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? PrescriptionId { get; set; }

    [Required, MaxLength(20)]
    public string OrderType { get; set; } = "ONLINE";

    [Required, MaxLength(30)]
    public string PickupType { get; set; } = "SHIPPING";

    [Required, MaxLength(30)]
    public string PaymentMethod { get; set; } = "COD";

    [MaxLength(50)]
    public string? VoucherCode { get; set; }

    [MaxLength(100)]
    public string? RecipientName { get; set; }

    [MaxLength(20)]
    public string? RecipientPhone { get; set; }

    [EmailAddress, MaxLength(150)]
    public string? GuestEmail { get; set; }

    [MaxLength(500)]
    public string? ShippingAddress { get; set; }

    [MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

public sealed class GuestCheckoutRequest
{
    [Required, MinLength(2), MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress, MaxLength(150)]
    public string? Email { get; set; }

    [Required]
    public CreateOrderRequest Order { get; set; } = new();
}

public sealed class WalkInCheckoutRequest
{
    [Required, MinLength(2), MaxLength(100)]
    public string CustomerName { get; set; } = "Khách lẻ";

    [MaxLength(20)]
    public string? Phone { get; set; }

    public bool HasPhysicalPrescription { get; set; }

    [MaxLength(100)]
    public string? PatientName { get; set; }

    [MaxLength(1000)]
    public string? PharmacistNote { get; set; }

    [Required]
    public CreateOrderRequest Order { get; set; } = new();
}

public sealed class ChangeOrderStatusRequest
{
    [MaxLength(500)]
    public string? Note { get; set; }
}

public sealed class ConfirmPaymentRequest
{
    [MaxLength(100)]
    public string? ExternalReference { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

public sealed class RefundPaymentRequest
{
    [Required, MinLength(3), MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public record PaymentTransactionResponse(
    Guid Id, string TransactionType, string Method, decimal Amount,
    string Status, string? ExternalReference, string? Note,
    Guid CreatedBy, string CreatedByName, DateTimeOffset CreatedAt);

public record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    Guid BatchId,
    string BatchNumber,
    DateOnly ExpiryDate,
    int Quantity,
    int BaseQuantity,
    Guid? SaleUnitId,
    string SaleUnitName,
    decimal UnitPrice,
    decimal VatRate,
    decimal VatAmount,
    decimal LineTotal);

public record OrderStatusHistoryResponse(
    string? FromStatus,
    string ToStatus,
    string? Note,
    Guid ChangedBy,
    string ChangedByName,
    DateTimeOffset ChangedAt);

public record OrderResponse(
    Guid Id,
    string Code,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchCode,
    Guid? PrescriptionId,
    string OrderType,
    string PickupType,
    string Status,
    decimal SubtotalBeforeVat,
    decimal TotalVatAmount,
    decimal ShippingFee,
    decimal DiscountAmount,
    decimal TotalAmount,
    string? VoucherCode,
    string PaymentMethod,
    string PaymentStatus,
    string? RecipientName,
    string? RecipientPhone,
    string? GuestEmail,
    string? ShippingAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<OrderItemResponse> Items,
    IReadOnlyCollection<OrderStatusHistoryResponse> StatusHistory,
    IReadOnlyCollection<PaymentTransactionResponse> Payments);
