using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("voucher_usages")]
public class VoucherUsage
{
    [Key, Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("voucher_id")]
    public Guid VoucherId { get; set; }
    public Voucher Voucher { get; set; } = null!;

    [Column("order_id")]
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    [Column("customer_id")]
    public Guid CustomerId { get; set; }
    public User Customer { get; set; } = null!;

    [Column("discount_amount")]
    public decimal DiscountAmount { get; set; }

    [Required, MaxLength(20), Column("status")]
    public string Status { get; set; } = VoucherUsageStatuses.Redeemed;

    [Column("used_at")]
    public DateTimeOffset UsedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("reversed_at")]
    public DateTimeOffset? ReversedAt { get; set; }
}

public static class VoucherUsageStatuses
{
    public const string Redeemed = "REDEEMED";
    public const string Reversed = "REVERSED";
}
