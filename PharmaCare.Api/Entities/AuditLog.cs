using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required, MaxLength(50)]
    [Column("action")]
    public string Action { get; set; } = string.Empty; // VD: PRESCRIPTION_APPROVE, POS_CHECKOUT, INVENTORY_ADJUST

    [Required, MaxLength(100)]
    [Column("entity_name")]
    public string EntityName { get; set; } = string.Empty;

    [Column("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [Column("old_values", TypeName = "jsonb")]
    public string? OldValues { get; set; }

    [Column("new_values", TypeName = "jsonb")]
    public string? NewValues { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}