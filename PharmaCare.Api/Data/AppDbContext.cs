using Microsoft.EntityFrameworkCore;
using PharmaCare.Api.Entities;

namespace PharmaCare.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // 1. NHÓM RBAC
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();

    // 2. NHÓM NGHIỆP VỤ THỰC TẾ (PHARMACARE DOMAIN)
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BranchInventory> BranchInventories => Set<BranchInventory>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<VoucherUsage> VoucherUsages => Set<VoucherUsage>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- CẤU HÌNH UNIQUE INDEX ---
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();
        modelBuilder.Entity<Permission>().HasIndex(p => p.Code).IsUnique();
        modelBuilder.Entity<Branch>().HasIndex(b => b.Code).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(p => p.Code).IsUnique();
        modelBuilder.Entity<Order>().HasIndex(o => o.Code).IsUnique();
        modelBuilder.Entity<Voucher>().HasIndex(v => v.Code).IsUnique();
        modelBuilder.Entity<RefreshToken>().HasIndex(t => t.TokenHash).IsUnique();
        modelBuilder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
        modelBuilder.Entity<Category>().Property(c => c.IsActive).HasDefaultValue(true);
        modelBuilder.Entity<Batch>().HasIndex(b => new { b.ProductId, b.BatchNumber }).IsUnique();
        modelBuilder.Entity<Prescription>().HasIndex(p => new { p.BranchId, p.Status });
        modelBuilder.Entity<PrescriptionItem>()
            .HasIndex(i => new { i.PrescriptionId, i.ProductId })
            .IsUnique();
        modelBuilder.Entity<Order>().HasIndex(o => new { o.BranchId, o.Status, o.CreatedAt });
        modelBuilder.Entity<VoucherUsage>().HasIndex(u => u.OrderId).IsUnique();
        modelBuilder.Entity<VoucherUsage>().HasIndex(u => new { u.VoucherId, u.CustomerId, u.Status });
        modelBuilder.Entity<PaymentTransaction>().HasIndex(p => new { p.OrderId, p.CreatedAt });
        modelBuilder.Entity<PaymentTransaction>().HasIndex(p => p.ExternalReference)
            .IsUnique().HasFilter("external_reference IS NOT NULL");
        modelBuilder.Entity<AuditLog>().HasIndex(a => a.CreatedAt);
        modelBuilder.Entity<AuditLog>().HasIndex(a => new { a.Action, a.CreatedAt });
        modelBuilder.Entity<AuditLog>().HasIndex(a => new { a.EntityName, a.EntityId });

        // --- CẤU HÌNH BẢNG NỐI USER_ROLES ---
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserBranch>()
            .HasKey(ub => new { ub.UserId, ub.BranchId });

        modelBuilder.Entity<UserBranch>()
            .HasIndex(ub => ub.UserId)
            .IsUnique()
            .HasFilter("is_primary = TRUE");

        modelBuilder.Entity<UserBranch>()
            .HasOne(ub => ub.User)
            .WithMany(u => u.UserBranches)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserBranch>()
            .HasOne(ub => ub.Branch)
            .WithMany(b => b.UserBranches)
            .HasForeignKey(ub => ub.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- CẤU HÌNH BẢNG NỐI ROLE_PERMISSIONS ---
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // --- CẤU HÌNH BẢNG TỒN KHO CHÍNH XÁC NHAU (BRANCH_INVENTORIES) ---
        // Khóa chính phức hợp 3 cột: BranchId + ProductId + BatchId (Tồn theo từng Lô tại từng Chi nhánh)
        modelBuilder.Entity<BranchInventory>()
            .HasKey(bi => new { bi.BranchId, bi.ProductId, bi.BatchId });

        modelBuilder.Entity<BranchInventory>()
            .Property(bi => bi.Version)
            .IsConcurrencyToken();

        // Một BatchId phải luôn đi cùng đúng ProductId của lô đó.
        modelBuilder.Entity<Batch>()
            .HasAlternateKey(b => new { b.ProductId, b.Id });

        modelBuilder.Entity<BranchInventory>()
            .HasOne(bi => bi.Batch)
            .WithMany()
            .HasForeignKey(bi => new { bi.ProductId, bi.BatchId })
            .HasPrincipalKey(b => new { b.ProductId, b.Id })
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Batch)
            .WithMany()
            .HasForeignKey(oi => new { oi.ProductId, oi.BatchId })
            .HasPrincipalKey(b => new { b.ProductId, b.Id })
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.Batch)
            .WithMany()
            .HasForeignKey(t => new { t.ProductId, t.BatchId })
            .HasPrincipalKey(b => new { b.ProductId, b.Id })
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.Branch)
            .WithMany()
            .HasForeignKey(t => t.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.Product)
            .WithMany()
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(t => new { t.BranchId, t.CreatedAt });

        modelBuilder.Entity<Category>()
            .HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Batch>()
            .HasOne(b => b.Product)
            .WithMany()
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .Property(o => o.Version)
            .IsConcurrencyToken();

        modelBuilder.Entity<OrderStatusHistory>()
            .HasOne(h => h.Order)
            .WithMany(o => o.StatusHistory)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderStatusHistory>()
            .HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Branch)
            .WithMany()
            .HasForeignKey(o => o.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Prescription)
            .WithMany()
            .HasForeignKey(o => o.PrescriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prescription>()
            .HasOne(p => p.Customer)
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prescription>()
            .HasOne(p => p.Branch)
            .WithMany()
            .HasForeignKey(p => p.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prescription>()
            .HasOne(p => p.Pharmacist)
            .WithMany()
            .HasForeignKey(p => p.PharmacistId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Prescription>()
            .Property(p => p.Version)
            .IsConcurrencyToken();

        modelBuilder.Entity<PrescriptionItem>()
            .HasOne(i => i.Prescription)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PrescriptionItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Voucher>()
            .HasOne(v => v.AssignedCustomer)
            .WithMany()
            .HasForeignKey(v => v.AssignedCustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Voucher>()
            .Property(v => v.Version)
            .IsConcurrencyToken();

        modelBuilder.Entity<VoucherUsage>()
            .HasOne(u => u.Voucher)
            .WithMany(v => v.Usages)
            .HasForeignKey(u => u.VoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VoucherUsage>()
            .HasOne(u => u.Order)
            .WithOne(o => o.VoucherUsage)
            .HasForeignKey<VoucherUsage>(u => u.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VoucherUsage>()
            .HasOne(u => u.Customer)
            .WithMany()
            .HasForeignKey(u => u.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        ConfigurePrecisionAndConstraints(modelBuilder);
    }

    private static void ConfigurePrecisionAndConstraints(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().Property(p => p.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(p => p.VatRate).HasPrecision(5, 2);
        modelBuilder.Entity<Batch>().Property(b => b.CostPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(o => o.SubtotalBeforeVat).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(o => o.TotalVatAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(o => o.ShippingFee).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(o => o.DiscountAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(oi => oi.VatRate).HasPrecision(5, 2);
        modelBuilder.Entity<OrderItem>().Property(oi => oi.VatAmount).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(oi => oi.LineTotal).HasPrecision(18, 2);
        modelBuilder.Entity<Voucher>().Property(v => v.DiscountValue).HasPrecision(18, 2);
        modelBuilder.Entity<Voucher>().Property(v => v.MinOrderAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Voucher>().Property(v => v.MaxDiscountAmount).HasPrecision(18, 2);
        modelBuilder.Entity<VoucherUsage>().Property(v => v.DiscountAmount).HasPrecision(18, 2);
        modelBuilder.Entity<PaymentTransaction>().Property(p => p.Amount).HasPrecision(18, 2);

        modelBuilder.Entity<Product>().ToTable("products", table =>
        {
            table.HasCheckConstraint("CK_products_unit_price", "unit_price >= 0");
            table.HasCheckConstraint("CK_products_vat_rate", "vat_rate >= 0 AND vat_rate <= 100");
        });
        modelBuilder.Entity<Batch>().ToTable("batches", table =>
        {
            table.HasCheckConstraint("CK_batches_cost_price", "cost_price >= 0");
            table.HasCheckConstraint("CK_batches_dates", "expiry_date >= mfg_date");
        });
        modelBuilder.Entity<BranchInventory>().ToTable("branch_inventories", table =>
        {
            table.HasCheckConstraint("CK_branch_inventories_quantities", "quantity_on_hand >= 0 AND reserved_quantity >= 0 AND reorder_level >= 0");
            table.HasCheckConstraint("CK_branch_inventories_reserved", "reserved_quantity <= quantity_on_hand");
        });
        modelBuilder.Entity<OrderItem>().ToTable("order_items", table =>
        {
            table.HasCheckConstraint("CK_order_items_quantity", "quantity > 0");
            table.HasCheckConstraint("CK_order_items_amounts", "unit_price >= 0 AND vat_amount >= 0 AND line_total >= 0");
            table.HasCheckConstraint("CK_order_items_vat_rate", "vat_rate >= 0 AND vat_rate <= 100");
        });
        modelBuilder.Entity<Order>().ToTable("orders", table =>
            table.HasCheckConstraint("CK_orders_amounts", "subtotal_before_vat >= 0 AND total_vat_amount >= 0 AND shipping_fee >= 0 AND discount_amount >= 0 AND total_amount >= 0"));
        modelBuilder.Entity<Voucher>().ToTable("vouchers", table =>
        {
            table.HasCheckConstraint("CK_vouchers_values", "discount_value > 0 AND min_order_amount >= 0 AND (max_discount_amount IS NULL OR max_discount_amount > 0)");
            table.HasCheckConstraint("CK_vouchers_percentage", "discount_type <> 'PERCENTAGE' OR discount_value <= 100");
            table.HasCheckConstraint("CK_vouchers_type", "discount_type IN ('FIXED_AMOUNT','PERCENTAGE')");
            table.HasCheckConstraint("CK_vouchers_limits", "per_customer_limit > 0 AND used_count >= 0 AND (usage_limit IS NULL OR usage_limit > 0)");
            table.HasCheckConstraint("CK_vouchers_dates", "valid_until IS NULL OR valid_until > valid_from");
        });
        modelBuilder.Entity<VoucherUsage>().ToTable("voucher_usages", table =>
        {
            table.HasCheckConstraint("CK_voucher_usages_amount", "discount_amount > 0");
            table.HasCheckConstraint("CK_voucher_usages_status", "status IN ('REDEEMED','REVERSED')");
        });
        modelBuilder.Entity<PaymentTransaction>().ToTable("payment_transactions", table =>
        {
            table.HasCheckConstraint("CK_payment_transactions_amount", "amount > 0");
            table.HasCheckConstraint("CK_payment_transactions_type", "transaction_type IN ('PAYMENT','REFUND')");
            table.HasCheckConstraint("CK_payment_transactions_method", "method IN ('COD','VIETQR','CASH_POS')");
            table.HasCheckConstraint("CK_payment_transactions_status", "status = 'SUCCEEDED'");
        });
        modelBuilder.Entity<InventoryTransaction>().ToTable("inventory_transactions", table =>
        {
            table.HasCheckConstraint("CK_inventory_transactions_quantity", "quantity <> 0");
            table.HasCheckConstraint("CK_inventory_transactions_balance", "balance_after >= 0");
            table.HasCheckConstraint(
                "CK_inventory_transactions_type",
                "transaction_type IN ('IMPORT','ADJUST_IN','ADJUST_OUT','TRANSFER_IN','TRANSFER_OUT','RESERVE','RELEASE','SALE','RETURN')");
        });
        modelBuilder.Entity<Prescription>().ToTable("prescriptions", table =>
            table.HasCheckConstraint(
                "CK_prescriptions_status",
                "status IN ('PENDING','APPROVED','REJECTED')"));
        modelBuilder.Entity<PrescriptionItem>().ToTable("prescription_items", table =>
            table.HasCheckConstraint("CK_prescription_items_quantity", "approved_quantity > 0"));
        modelBuilder.Entity<Order>().ToTable("orders", table =>
        {
            table.HasCheckConstraint(
                "CK_orders_status",
                "status IN ('PENDING','CONFIRMED','COMPLETED','CANCELLED')");
            table.HasCheckConstraint("CK_orders_type", "order_type IN ('ONLINE','POS')");
            table.HasCheckConstraint("CK_orders_pickup", "pickup_type IN ('SHIPPING','STORE_PICKUP')");
            table.HasCheckConstraint("CK_orders_payment_method", "payment_method IN ('COD','VIETQR','CASH_POS')");
            table.HasCheckConstraint("CK_orders_payment_status", "payment_status IN ('UNPAID','PAID','REFUNDED')");
        });
    }
}
