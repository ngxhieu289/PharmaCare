using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("prescriptions")]
public class Prescription
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Required, MaxLength(500)]
    [Column("image_url")]
    public string ImageUrl { get; set; } = string.Empty; // Ảnh đơn thuốc

    [MaxLength(100)]
    [Column("patient_name")]
    public string PatientName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED

    [Column("pharmacist_id")]
    public Guid? PharmacistId { get; set; }

    [Column("pharmacist_note")]
    public string? PharmacistNote { get; set; } // Ghi chú liều dùng của Dược sĩ

    [Column("reviewed_at")]
    public DateTimeOffset? ReviewedAt { get; set; }
}