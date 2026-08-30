using System.ComponentModel.DataAnnotations;

namespace PharmaCare.Api.Dtos;

public record InventoryResponse(
    Guid BranchId,
    string BranchCode,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    Guid BatchId,
    string BatchNumber,
    DateOnly ExpiryDate,
    int QuantityOnHand,
    int ReservedQuantity,
    int AvailableQuantity,
    int ReorderLevel,
    bool IsLowStock,
    bool IsExpired,
    long Version);

public record InventoryTransactionResponse(
    Guid Id,
    Guid BranchId,
    string BranchCode,
    Guid ProductId,
    string ProductCode,
    Guid BatchId,
    string BatchNumber,
    string TransactionType,
    int Quantity,
    int BalanceAfter,
    string? ReferenceType,
    string? ReferenceId,
    string? Note,
    Guid CreatedBy,
    string CreatedByName,
    DateTimeOffset CreatedAt);

public sealed class ReceiveInventoryRequest
{
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public Guid BatchId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; } = 10;

    [MaxLength(500)]
    public string? Note { get; set; }
}

public sealed class AdjustInventoryRequest
{
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public Guid BatchId { get; set; }

    public int QuantityDelta { get; set; }

    [Required, MinLength(3), MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class TransferInventoryRequest
{
    public Guid FromBranchId { get; set; }
    public Guid ToBranchId { get; set; }
    public Guid ProductId { get; set; }
    public Guid BatchId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
