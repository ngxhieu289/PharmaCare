using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("inventory_transactions")]
public class InventoryTransaction
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("branch_id")]
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    [Column("product_id")]
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Column("batch_id")]
    public Guid BatchId { get; set; }
    public Batch Batch { get; set; } = null!;

    [Required, MaxLength(30)]
    [Column("transaction_type")]
    public string TransactionType { get; set; } = string.Empty;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("balance_after")]
    public int BalanceAfter { get; set; }

    [MaxLength(50)]
    [Column("reference_type")]
    public string? ReferenceType { get; set; }

    [MaxLength(100)]
    [Column("reference_id")]
    public string? ReferenceId { get; set; }

    [MaxLength(500)]
    [Column("note")]
    public string? Note { get; set; }

    [Column("created_by")]
    public Guid CreatedBy { get; set; }
    public User CreatedByUser { get; set; } = null!;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class InventoryTransactionTypes
{
    public const string Import = "IMPORT";
    public const string AdjustIn = "ADJUST_IN";
    public const string AdjustOut = "ADJUST_OUT";
    public const string TransferIn = "TRANSFER_IN";
    public const string TransferOut = "TRANSFER_OUT";
    public const string Reserve = "RESERVE";
    public const string Release = "RELEASE";
    public const string Sale = "SALE";
    public const string Return = "RETURN";

    public static readonly IReadOnlyCollection<string> All =
    [Import, AdjustIn, AdjustOut, TransferIn, TransferOut, Reserve, Release, Sale, Return];
}
