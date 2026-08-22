using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("branches")]
public class Branch
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(30)]
    [Column("code")]
    public string Code { get; set; } = string.Empty; // VD: CN01, CN02

    [Required, MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty; // VD: Nhà thuốc PharmaCare Cầu Giấy

    [Required, MaxLength(500)]
    [Column("address")]
    public string Address { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [MaxLength(100)]
    [Column("province")]
    public string? Province { get; set; }

    [MaxLength(100)]
    [Column("district")]
    public string? District { get; set; }

    [MaxLength(100)]
    [Column("ward")]
    public string? Ward { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}