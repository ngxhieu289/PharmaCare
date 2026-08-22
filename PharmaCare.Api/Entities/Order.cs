using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("orders")]
public class Order
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(30)]
    [Column("code")]
    public string Code { get; set; } = string.Empty; // Mã đơn: ORD-20260822-001

    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("branch_id")]
    public Guid BranchId { get; set; }

    [Column("prescription_id")]
    public Guid? PrescriptionId { get; set; }

    [Required, MaxLength(20)]
    [Column("order_type")]
    public string OrderType { get; set; } = "ONLINE"; // ONLINE, POS

    [Required, MaxLength(30)]
    [Column("pickup_type")]
    public string PickupType { get; set; } = "SHIPPING"; // SHIPPING, STORE_PICKUP

    [Required, MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "PENDING";

    [Column("subtotal_before_vat")]
    public decimal SubtotalBeforeVat { get; set; } // Tiền trước thuế

    [Column("total_vat_amount")]
    public decimal TotalVatAmount { get; set; } // Tiền thuế VAT

    [Column("shipping_fee")]
    public decimal ShippingFee { get; set; } = 0;

    [Column("discount_amount")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column("total_amount")]
    public decimal TotalAmount { get; set; } // Tổng thực thu

    [MaxLength(50)]
    [Column("voucher_code")]
    public string? VoucherCode { get; set; }

    [Required, MaxLength(30)]
    [Column("payment_method")]
    public string PaymentMethod { get; set; } = "COD"; // COD, VIETQR, CASH_POS

    [Column("payment_status")]
    public string PaymentStatus { get; set; } = "UNPAID";

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}