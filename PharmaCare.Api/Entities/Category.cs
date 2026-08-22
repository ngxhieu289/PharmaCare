using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("categories")]
public class Category
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty; // VD: Thuốc kháng sinh, Thực phẩm chức năng

    [Required, MaxLength(120)]
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("parent_id")]
    public Guid? ParentId { get; set; }
}