using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("vouchers")]
public class Voucher
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(50)]
    [Column("code")]
    public string Code { get; set; } = string.Empty; // Mã Voucher

    [Column("discount_type")]
    public string DiscountType { get; set; } = "FIXED_AMOUNT"; // FIXED_AMOUNT, PERCENTAGE

    [Column("discount_value")]
    public decimal DiscountValue { get; set; }

    [Column("min_order_amount")]
    public decimal MinOrderAmount { get; set; } = 0;

    [Column("max_discount_amount")]
    public decimal? MaxDiscountAmount { get; set; }

    [Column("valid_from")]
    public DateTimeOffset ValidFrom { get; set; } = DateTimeOffset.UtcNow;

    [Column("valid_until")]
    public DateTimeOffset? ValidUntil { get; set; }

    [Column("usage_limit")]
    public int? UsageLimit { get; set; }

    [Column("per_customer_limit")]
    public int PerCustomerLimit { get; set; } = 1;

    [Column("used_count")]
    public int UsedCount { get; set; }

    [Column("assigned_customer_id")]
    public Guid? AssignedCustomerId { get; set; } // Null: Mã chung sự kiện; Có ID: Mã gán riêng khách lẻ
    public User? AssignedCustomer { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("version")]
    public long Version { get; set; } = 1;

    public ICollection<VoucherUsage> Usages { get; set; } = new List<VoucherUsage>();
}

public static class VoucherDiscountTypes
{
    public const string FixedAmount = "FIXED_AMOUNT";
    public const string Percentage = "PERCENTAGE";
}
