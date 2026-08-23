using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("prescription_items")]
public class PrescriptionItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("prescription_id")]
    public Guid PrescriptionId { get; set; }
    public Prescription Prescription { get; set; } = null!;

    [Column("product_id")]
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Column("approved_quantity")]
    public int ApprovedQuantity { get; set; }

    [Required, MaxLength(255)]
    [Column("dosage")]
    public string Dosage { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("instructions")]
    public string? Instructions { get; set; }
}
