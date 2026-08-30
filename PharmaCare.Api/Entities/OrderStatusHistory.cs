using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("order_status_histories")]
public class OrderStatusHistory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("order_id")]
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    [MaxLength(30)]
    [Column("from_status")]
    public string? FromStatus { get; set; }

    [Required, MaxLength(30)]
    [Column("to_status")]
    public string ToStatus { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("note")]
    public string? Note { get; set; }

    [Column("changed_by")]
    public Guid ChangedBy { get; set; }
    public User ChangedByUser { get; set; } = null!;

    [Column("changed_at")]
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
}
