using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("batches")]
public class Batch
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("product_id")]
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, MaxLength(50)]
    [Column("batch_number")]
    public string BatchNumber { get; set; } = string.Empty; // Mã lô: BT202608

    [Column("mfg_date")]
    public DateOnly MfgDate { get; set; } // Ngày sản xuất

    [Column("expiry_date")]
    public DateOnly ExpiryDate { get; set; } // Hạn sử dụng (Date)

    [Column("cost_price")]
    public decimal CostPrice { get; set; } // Giá vốn nhập
}
