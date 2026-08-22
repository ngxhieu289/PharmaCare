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

    [Column("assigned_customer_id")]
    public Guid? AssignedCustomerId { get; set; } // Null: Mã chung sự kiện; Có ID: Mã gán riêng khách lẻ

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}