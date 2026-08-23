namespace PharmaCare.Api.Dtos;

public record ReportPeriod(DateOnly From, DateOnly To);

public record OrderStatusCountResponse(string Status, int Count);

public record DashboardResponse(
    ReportPeriod Period,
    int TotalOrders,
    int CompletedOrders,
    int CancelledOrders,
    decimal GrossSales,
    decimal RefundedAmount,
    decimal NetRevenue,
    decimal DiscountAmount,
    decimal VatAmount,
    decimal AverageOrderValue,
    int PendingPrescriptions,
    int LowStockRows,
    int ExpiringBatchRows,
    IReadOnlyCollection<OrderStatusCountResponse> OrdersByStatus);

public record DailySalesResponse(
    DateOnly Date, int OrderCount, decimal GrossSales,
    decimal RefundedAmount, decimal NetRevenue, decimal DiscountAmount);

public record TopProductResponse(
    Guid ProductId, string ProductCode, string ProductName,
    int QuantitySold, decimal GrossSales, int OrderCount);

public record BranchSalesResponse(
    Guid BranchId, string BranchCode, string BranchName,
    int OrderCount, decimal GrossSales, decimal RefundedAmount, decimal NetRevenue);

public record InventoryAlertResponse(
    Guid BranchId, string BranchCode, Guid ProductId, string ProductCode,
    string ProductName, Guid BatchId, string BatchNumber, DateOnly ExpiryDate,
    int QuantityOnHand, int ReservedQuantity, int AvailableQuantity,
    int ReorderLevel, string AlertType);
