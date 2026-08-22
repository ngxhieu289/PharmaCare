using Microsoft.EntityFrameworkCore;
using PharmaCare.Api.Entities;

namespace PharmaCare.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Seed 5 Roles mặc định
        if (!await context.Roles.AnyAsync())
        {
            var defaultRoles = new List<Role>
            {
                new() { Name = "Admin", Description = "Quản trị viên toàn hệ thống" },
                new() { Name = "BranchManager", Description = "Quản lý chi nhánh nhà thuốc" },
                new() { Name = "Pharmacist", Description = "Dược sĩ thẩm định & Bán hàng POS" },
                new() { Name = "WarehouseStaff", Description = "Nhân viên quản lý kho & Lô hàng" },
                new() { Name = "Customer", Description = "Khách hàng mua thuốc" }
            };
            await context.Roles.AddRangeAsync(defaultRoles);
        }

        // 2. Seed 2 Chi nhánh Nhà thuốc
        if (!await context.Branches.AnyAsync())
        {
            var branches = new List<Branch>
            {
                new() { Code = "CN01", Name = "PharmaCare Cơ sở 1 - Cầu Giấy", Address = "123 Cầu Giấy, Hà Nội", Province = "Hà Nội", District = "Cầu Giấy", Ward = "Dịch Vọng" },
                new() { Code = "CN02", Name = "PharmaCare Cơ sở 2 - Quận 1", Address = "45 Nguyễn Trãi, Q.1, TP.HCM", Province = "TP. Hồ Chí Minh", District = "Quận 1", Ward = "Bến Thành" }
            };
            await context.Branches.AddRangeAsync(branches);
        }

        // 3. Seed Danh mục & Thuốc mẫu
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
}