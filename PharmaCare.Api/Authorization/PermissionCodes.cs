namespace PharmaCare.Api.Authorization;

public static class PermissionCodes
{
    public const string ClaimType = "permission";

    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string RolesRead = "roles.read";
    public const string RolesManage = "roles.manage";
    public const string BranchesRead = "branches.read";
    public const string BranchesManage = "branches.manage";
    public const string ProductsRead = "products.read";
    public const string ProductsManage = "products.manage";
    public const string InventoryRead = "inventory.read";
    public const string InventoryAdjust = "inventory.adjust";
    public const string PrescriptionsCreate = "prescriptions.create";
    public const string PrescriptionsRead = "prescriptions.read";
    public const string PrescriptionsReview = "prescriptions.review";
    public const string OrdersCreate = "orders.create";
    public const string OrdersRead = "orders.read";
    public const string OrdersManage = "orders.manage";
    public const string VouchersManage = "vouchers.manage";
    public const string ReportsRead = "reports.read";
    public const string AuditRead = "audit.read";

    public static readonly IReadOnlyCollection<string> All =
    [
        UsersRead, UsersManage, RolesRead, RolesManage,
        BranchesRead, BranchesManage, ProductsRead, ProductsManage,
        InventoryRead, InventoryAdjust,
        PrescriptionsCreate, PrescriptionsRead, PrescriptionsReview,
        OrdersCreate, OrdersRead, OrdersManage,
        VouchersManage, ReportsRead, AuditRead
    ];
}
