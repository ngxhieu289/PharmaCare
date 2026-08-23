using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Api.Authorization;
using PharmaCare.Api.Entities;
using PharmaCare.Api.Services;

namespace PharmaCare.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(
        AppDbContext context,
        IPasswordHasher<User> passwordHasher,
        BootstrapAdminSettings bootstrapAdmin)
    {
        // 1. Seed Roles theo từng bản ghi để có thể bổ sung khi database đã có dữ liệu.
        var roleDefinitions = new Dictionary<string, string>
        {
            ["Admin"] = "Quản trị viên toàn hệ thống",
            ["BranchManager"] = "Quản lý chi nhánh nhà thuốc",
            ["Pharmacist"] = "Dược sĩ thẩm định & Bán hàng POS",
            ["WarehouseStaff"] = "Nhân viên quản lý kho & Lô hàng",
            ["Customer"] = "Khách hàng mua thuốc"
        };

        var roles = await context.Roles.ToListAsync();
        foreach (var definition in roleDefinitions)
        {
            if (roles.All(r => r.Name != definition.Key))
            {
                var role = new Role { Name = definition.Key, Description = definition.Value };
                roles.Add(role);
                context.Roles.Add(role);
            }
        }
        await context.SaveChangesAsync();

        // 2. Seed permissions và ánh xạ mặc định cho từng Role.
        var permissions = await context.Permissions.ToListAsync();
        foreach (var code in PermissionCodes.All)
        {
            if (permissions.All(p => p.Code != code))
            {
                var permission = new Permission { Code = code, Description = DescribePermission(code) };
                permissions.Add(permission);
                context.Permissions.Add(permission);
            }
        }
        await context.SaveChangesAsync();

        var rolePermissionMap = new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["Admin"] = PermissionCodes.All,
            ["BranchManager"] =
            [
                PermissionCodes.BranchesRead, PermissionCodes.ProductsRead,
                PermissionCodes.InventoryRead, PermissionCodes.InventoryAdjust,
                PermissionCodes.PrescriptionsRead, PermissionCodes.OrdersRead,
                PermissionCodes.OrdersManage, PermissionCodes.VouchersManage,
                PermissionCodes.ReportsRead
            ],
            ["Pharmacist"] =
            [
                PermissionCodes.ProductsRead, PermissionCodes.InventoryRead,
                PermissionCodes.PrescriptionsRead, PermissionCodes.PrescriptionsReview,
                PermissionCodes.OrdersRead, PermissionCodes.OrdersManage
            ],
            ["WarehouseStaff"] =
            [
                PermissionCodes.BranchesRead, PermissionCodes.ProductsRead,
                PermissionCodes.ProductsManage, PermissionCodes.InventoryRead,
                PermissionCodes.InventoryAdjust
            ],
            ["Customer"] =
            [
                PermissionCodes.BranchesRead, PermissionCodes.ProductsRead,
                PermissionCodes.PrescriptionsCreate,
                PermissionCodes.PrescriptionsRead, PermissionCodes.OrdersCreate,
                PermissionCodes.OrdersRead
            ]
        };

        var existingMappings = await context.RolePermissions
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync();
        var existingKeys = existingMappings
            .Select(rp => (rp.RoleId, rp.PermissionId))
            .ToHashSet();

        foreach (var mapping in rolePermissionMap)
        {
            var role = roles.Single(r => r.Name == mapping.Key);
            foreach (var code in mapping.Value)
            {
                var permission = permissions.Single(p => p.Code == code);
                if (existingKeys.Add((role.Id, permission.Id)))
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id
                    });
                }
            }
        }
        await context.SaveChangesAsync();

        // 3. Bootstrap Admin tùy chọn. Chỉ tạo khi cấu hình có đủ email/password.
        if (!string.IsNullOrWhiteSpace(bootstrapAdmin.Email) &&
            !string.IsNullOrWhiteSpace(bootstrapAdmin.Password))
        {
            var email = bootstrapAdmin.Email.Trim().ToLowerInvariant();
            var admin = await context.Users
                .Include(u => u.UserRoles)
                .SingleOrDefaultAsync(u => u.Email == email);

            if (admin is null)
            {
                admin = new User
                {
                    Email = email,
                    DisplayName = bootstrapAdmin.DisplayName.Trim(),
                    IsActive = true
                };
                admin.PasswordHash = passwordHasher.HashPassword(admin, bootstrapAdmin.Password);
                context.Users.Add(admin);
            }

            var adminRole = roles.Single(r => r.Name == "Admin");
            if (admin.UserRoles.All(ur => ur.RoleId != adminRole.Id))
            {
                admin.UserRoles.Add(new UserRole { User = admin, Role = adminRole });
            }

            await context.SaveChangesAsync();
        }

        // 4. Seed 2 Chi nhánh Nhà thuốc
        if (!await context.Branches.AnyAsync())
        {
            var branches = new List<Branch>
            {
                new() { Code = "CN01", Name = "PharmaCare Cơ sở 1 - Cầu Giấy", Address = "123 Cầu Giấy, Hà Nội", Province = "Hà Nội", District = "Cầu Giấy", Ward = "Dịch Vọng" },
                new() { Code = "CN02", Name = "PharmaCare Cơ sở 2 - Quận 1", Address = "45 Nguyễn Trãi, Q.1, TP.HCM", Province = "TP. Hồ Chí Minh", District = "Quận 1", Ward = "Bến Thành" }
            };
            await context.Branches.AddRangeAsync(branches);
        }

        // 5. Seed Danh mục & Thuốc mẫu
        if (!await context.Categories.AnyAsync())
        {
            var catGiamDau = new Category { Name = "Thuốc Giảm đau - Hạ sốt", Slug = "thuoc-giam-dau-ha-sot" };
            var catKhangSinh = new Category { Name = "Thuốc Kháng sinh", Slug = "thuoc-khang-sinh" };
            await context.Categories.AddRangeAsync(catGiamDau, catKhangSinh);
            await context.SaveChangesAsync();

            var products = new List<Product>
            {
                new() {
                    Code = "MED-PAR-500",
                    Name = "Thuốc Paracetamol 500mg",
                    ActiveIngredient = "Paracetamol",
                    Indications = "Đau đầu, sốt, đau răng, cảm cúm",
                    CategoryId = catGiamDau.Id,
                    RxFlag = false, // OTC
                    VatRate = 5.00m,
                    Packaging = "Hộp 10 vỉ x 10 viên",
                    UnitPrice = 55000m,
                    WarningText = "Không dùng quá 4g/ngày. Thận trọng với người bệnh gan."
                },
                new() {
                    Code = "MED-PAN-500",
                    Name = "Thuốc Panactol 500mg",
                    ActiveIngredient = "Paracetamol",
                    Indications = "Đau đầu, đau nhức cơ thể, sốt cao",
                    CategoryId = catGiamDau.Id,
                    RxFlag = false, // OTC
                    VatRate = 5.00m,
                    Packaging = "Hộp 5 vỉ x 10 viên",
                    UnitPrice = 35000m,
                    WarningText = "Chống chỉ định cho người mẫn cảm với Paracetamol."
                },
                new() {
                    Code = "MED-AMO-500",
                    Name = "Thuốc Kháng Sinh Amoxicillin 500mg",
                    ActiveIngredient = "Amoxicillin",
                    Indications = "Nhiễm khuẩn hô hấp, viêm họng, viêm tai giữa",
                    CategoryId = catKhangSinh.Id,
                    RxFlag = true, // KHÁNG SINH - CẦN ĐƠN THUỐC (RX)
                    VatRate = 5.00m,
                    Packaging = "Hộp 10 vỉ x 10 viên",
                    UnitPrice = 120000m,
                    WarningText = "Cần đơn của Bác sĩ. Uống đúng liều lượng và đủ ngày chỉ định."
                }
            };
            await context.Products.AddRangeAsync(products);
        }

        await context.SaveChangesAsync();
    }

    private static string DescribePermission(string code) => code switch
    {
        PermissionCodes.UsersRead => "Xem danh sách người dùng",
        PermissionCodes.UsersManage => "Tạo và cập nhật người dùng",
        PermissionCodes.RolesRead => "Xem vai trò và quyền",
        PermissionCodes.RolesManage => "Gán hoặc gỡ vai trò và quyền",
        PermissionCodes.BranchesRead => "Xem chi nhánh",
        PermissionCodes.BranchesManage => "Quản lý chi nhánh",
        PermissionCodes.ProductsRead => "Xem sản phẩm",
        PermissionCodes.ProductsManage => "Quản lý sản phẩm",
        PermissionCodes.InventoryRead => "Xem tồn kho",
        PermissionCodes.InventoryAdjust => "Nhập, xuất và điều chỉnh kho",
        PermissionCodes.PrescriptionsCreate => "Tạo đơn thuốc",
        PermissionCodes.PrescriptionsRead => "Xem đơn thuốc được phép truy cập",
        PermissionCodes.PrescriptionsReview => "Duyệt hoặc từ chối đơn thuốc",
        PermissionCodes.OrdersCreate => "Tạo đơn hàng",
        PermissionCodes.OrdersRead => "Xem đơn hàng được phép truy cập",
        PermissionCodes.OrdersManage => "Xử lý và cập nhật đơn hàng",
        PermissionCodes.VouchersManage => "Quản lý voucher",
        PermissionCodes.ReportsRead => "Xem báo cáo",
        PermissionCodes.AuditRead => "Xem nhật ký kiểm toán",
        _ => code
    };
}
