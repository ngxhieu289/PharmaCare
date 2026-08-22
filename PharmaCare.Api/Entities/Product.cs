using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("products")]
public class Product
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(30)]
    [Column("code")]
    public string Code { get; set; } = string.Empty; // VD: MED-PAR-500

    [Required, MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty; // VD: Thuốc Paracetamol 500mg

    [MaxLength(255)]
    [Column("active_ingredient")]
    public string? ActiveIngredient { get; set; } // Hoạt chất: Paracetamol

    [MaxLength(500)]
    [Column("indications")]
    public string? Indications { get; set; } // Triệu chứng: Đau đầu, sốt, cảm cúm

    [Column("category_id")]
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    [Column("rx_flag")]
    public bool RxFlag { get; set; } = false; // true: Thuốc kê đơn / Kháng sinh

    [Column("vat_rate")]
    public decimal VatRate { get; set; } = 5.00m; // Thuế VAT: 5%, 8%, 10%

    [Column("packaging")]
    public string Packaging { get; set; } = "Hộp"; // Quy cách: Viên, Vỉ, Hộp, Chai 100ml

    [Column("unit_price")]
    public decimal UnitPrice { get; set; } // Giá bán đã có VAT

    [Column("storage_temp")]
    public string? StorageTemp { get; set; }

    [Column("warning_text")]
    public string? WarningText { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}