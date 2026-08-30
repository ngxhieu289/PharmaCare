using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("product_sale_units")]
public class ProductSaleUnit
{
    [Key, Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("product_id")]
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required, MaxLength(30), Column("unit_name")]
    public string UnitName { get; set; } = string.Empty;

    [Column("conversion_factor")]
    public int ConversionFactor { get; set; } = 1;

    [Column("sale_price")]
    public decimal SalePrice { get; set; }

    [Column("is_default")]
    public bool IsDefault { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
