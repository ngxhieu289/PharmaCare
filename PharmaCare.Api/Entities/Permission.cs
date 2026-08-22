using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("permissions")]
public class Permission
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("code")]
    public string Code { get; set; } = string.Empty; // Ví dụ: "prescription.approve"

    [MaxLength(255)]
    [Column("description")]
    public string? Description { get; set; }

    // Quan hệ Many-to-Many với Role
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}