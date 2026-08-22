using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("user_roles")]
public class UserRole
{
    [Column("user_id")]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Column("role_id")]
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}