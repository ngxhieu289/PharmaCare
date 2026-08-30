using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("refresh_tokens")]
public class RefreshToken
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, MaxLength(64)]
    [Column("token_hash")]
    public string TokenHash { get; set; } = string.Empty;

    [Column("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(64)]
    [Column("created_by_ip")]
    public string CreatedByIp { get; set; } = string.Empty;

    [Column("revoked_at")]
    public DateTimeOffset? RevokedAt { get; set; }

    [MaxLength(64)]
    [Column("revoked_by_ip")]
    public string? RevokedByIp { get; set; }

    [MaxLength(64)]
    [Column("replaced_by_token_hash")]
    public string? ReplacedByTokenHash { get; set; }

    [NotMapped]
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
