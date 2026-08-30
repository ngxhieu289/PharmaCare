using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("order_items")]
public class OrderItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("order_id")]
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    [Column("batch_id")]
    public Guid BatchId { get; set; } // Lô xuất theo FIFO
    public Batch? Batch { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("sale_unit_id")]
    public Guid? SaleUnitId { get; set; }
    public ProductSaleUnit? SaleUnit { get; set; }

    [Required, MaxLength(30), Column("sale_unit_name")]
    public string SaleUnitName { get; set; } = string.Empty;

    [Column("sale_quantity")]
    public int SaleQuantity { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("vat_rate")]
    public decimal VatRate { get; set; }

    [Column("vat_amount")]
    public decimal VatAmount { get; set; }

    [Column("line_total")]
    public decimal LineTotal { get; set; }
}
