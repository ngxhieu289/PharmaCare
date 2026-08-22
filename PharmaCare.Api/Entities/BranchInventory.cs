using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaCare.Api.Entities;

[Table("branch_inventories")]
public class BranchInventory
{
    [Column("branch_id")]
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    [Column("batch_id")]
    public Guid BatchId { get; set; }
    public Batch? Batch { get; set; }

    [Column("quantity_on_hand")]
    public int QuantityOnHand { get; set; } = 0; // Tồn thực tế

    [Column("reserved_quantity")]
    public int ReservedQuantity { get; set; } = 0; // Đang giữ đơn online

    [Column("reorder_level")]
    public int ReorderLevel { get; set; } = 10; // Ngưỡng tồn thấp
}