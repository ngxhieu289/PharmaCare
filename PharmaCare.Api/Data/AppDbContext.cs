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

        // --- CẤU HÌNH BẢNG NỐI ROLE_PERMISSIONS ---
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // --- CẤU HÌNH BẢNG TỒN KHO CHÍNH XÁC NHAU (BRANCH_INVENTORIES) ---
        // Khóa chính phức hợp 3 cột: BranchId + ProductId + BatchId (Tồn theo từng Lô tại từng Chi nhánh)
        modelBuilder.Entity<BranchInventory>()
            .HasKey(bi => new { bi.BranchId, bi.ProductId, bi.BatchId });
    }
}