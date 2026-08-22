using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("role_permissions")]
public class RolePermission
{
    [Column("role_id")]
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    [Column("permission_id")]
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}