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
        BootstrapAdminSettings bootstrapAdmin,
        DemoAccountsSettings demoAccounts)
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
            // Chỉ khởi tạo ma trận mặc định một lần. Các thay đổi RBAC của Admin
            // không được tự động hoàn tác ở lần khởi động tiếp theo.
            if (mapping.Key != "Admin" && existingKeys.Any(key => key.RoleId == role.Id))
            {
                continue;
            }
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

        await context.SaveChangesAsync();
        if (demoAccounts.Enabled)
        {
            var primaryBranch = await context.Branches.OrderBy(branch => branch.Code).FirstAsync();
            var allBranches = await context.Branches.Where(branch => branch.IsActive).ToListAsync();
            var definitions = new[]
            {
                new { Email = "pharmacist@pharmacare.local", Username = "pharmacist", Name = "Dược sĩ PharmaCare", Phone = "0901000001", Role = "Pharmacist", Password = demoAccounts.PharmacistPassword },
                new { Email = "warehouse@pharmacare.local", Username = "warehouse", Name = "Nhân viên kho PharmaCare", Phone = "0901000002", Role = "WarehouseStaff", Password = demoAccounts.WarehousePassword },
                new { Email = "manager@pharmacare.local", Username = "manager", Name = "Quản lý chi nhánh PharmaCare", Phone = "0901000003", Role = "BranchManager", Password = demoAccounts.BranchManagerPassword },
                new { Email = "admin@pharmacare.local", Username = "admin", Name = "Quản trị hệ thống PharmaCare", Phone = "0901000004", Role = "Admin", Password = demoAccounts.AdminPassword }
            };
            foreach (var definition in definitions)
            {
                if (string.IsNullOrWhiteSpace(definition.Password)) continue;
                var user = await context.Users.Include(item => item.UserRoles).Include(item => item.UserBranches)
                    .SingleOrDefaultAsync(item => item.Email == definition.Email);
                if (user is null)
                {
                    user = new User { Email = definition.Email, DisplayName = definition.Name, Phone = definition.Phone, IsActive = true };
                    context.Users.Add(user);
                }
                user.DisplayName = definition.Name;
                user.Username = definition.Username;
                user.Phone = definition.Phone;
                user.IsActive = true;
                user.IsGuest = false;
                user.PasswordHash = passwordHasher.HashPassword(user, definition.Password);
                var role = roles.Single(item => item.Name == definition.Role);
                if (user.UserRoles.All(item => item.RoleId != role.Id))
                    user.UserRoles.Add(new UserRole { User = user, Role = role });
                if (definition.Role != "Admin")
                {
                    var assignedBranches = definition.Role == "BranchManager" ? [primaryBranch] : allBranches;
                    if (definition.Role == "BranchManager")
                        context.UserBranches.RemoveRange(user.UserBranches.Where(item => item.BranchId != primaryBranch.Id));
                    foreach (var branch in assignedBranches.Where(branch => user.UserBranches.All(item => item.BranchId != branch.Id)))
                        user.UserBranches.Add(new UserBranch { User = user, Branch = branch, IsPrimary = branch.Id == primaryBranch.Id });
                }
            }
            await context.SaveChangesAsync();
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
        await SeedExpandedCatalogAsync(context);
    }

    private static async Task SeedExpandedCatalogAsync(AppDbContext context)
    {
        var categoryDefinitions = new (string Name, string Slug)[]
        {
            ("Thuốc ho và cảm", "thuoc-ho-va-cam"),
            ("Tiêu hóa", "tieu-hoa"),
            ("Dị ứng", "di-ung"),
            ("Vitamin và khoáng chất", "vitamin-khoang-chat"),
            ("Da liễu", "da-lieu"),
            ("Mắt và tai", "mat-va-tai"),
            ("Dầu xoa và giảm đau", "dau-xoa-giam-dau")
        };
        var categories = await context.Categories.ToListAsync();
        foreach (var definition in categoryDefinitions)
        {
            if (categories.All(category => category.Slug != definition.Slug))
            {
                var category = new Category { Name = definition.Name, Slug = definition.Slug };
                categories.Add(category);
                context.Categories.Add(category);
            }
        }
        await context.SaveChangesAsync();

        var existingCodes = await context.Products.Select(product => product.Code).ToHashSetAsync();
        Guid Category(string slug) => categories.Single(category => category.Slug == slug).Id;
        var products = new Product[]
        {
            NewProduct("MED-HAP-650", "Hapacol 650mg", "Paracetamol", "Đau đầu, đau răng, đau cơ, hạ sốt, cảm cúm", "thuoc-giam-dau-ha-sot", "Hộp 10 vỉ x 10 viên", 72000),
            NewProduct("MED-EFF-500", "Efferalgan 500mg", "Paracetamol", "Đau đầu, đau nửa đầu, đau răng, hạ sốt", "thuoc-giam-dau-ha-sot", "Hộp 4 vỉ x 4 viên sủi", 68000),
            NewProduct("MED-IBU-400", "Ibuprofen 400mg", "Ibuprofen", "Đau đầu, đau bụng kinh, đau cơ xương, viêm và sốt", "thuoc-giam-dau-ha-sot", "Hộp 10 vỉ x 10 viên", 89000),
            NewProduct("MED-DEC-C", "Decolgen Forte", "Paracetamol, Phenylephrine", "Nghẹt mũi, sổ mũi, đau đầu, cảm lạnh, cảm cúm", "thuoc-ho-va-cam", "Hộp 25 vỉ x 4 viên", 95000),
            NewProduct("MED-PRO-H", "Prospan siro ho", "Cao lá thường xuân", "Ho khan, ho có đờm, viêm đường hô hấp", "thuoc-ho-va-cam", "Chai 100ml", 118000),
            NewProduct("MED-EUG-H", "Eugica xanh", "Eucalyptol, Menthol", "Ho, đau họng, khàn tiếng, sổ mũi", "thuoc-ho-va-cam", "Hộp 10 vỉ x 10 viên", 78000),
            NewProduct("MED-SME-3G", "Smecta 3g", "Diosmectite", "Tiêu chảy cấp, đau bụng, rối loạn tiêu hóa", "tieu-hoa", "Hộp 30 gói", 125000),
            NewProduct("MED-BER-100", "Berberin 100mg", "Berberine chloride", "Tiêu chảy, lỵ, nhiễm khuẩn đường ruột", "tieu-hoa", "Lọ 100 viên", 35000),
            NewProduct("MED-GAV-10", "Gaviscon Dual Action", "Natri alginate", "Ợ nóng, ợ chua, trào ngược dạ dày", "tieu-hoa", "Hộp 24 gói", 185000),
            NewProduct("MED-CET-10", "Cetirizine 10mg", "Cetirizine", "Viêm mũi dị ứng, hắt hơi, sổ mũi, nổi mề đay", "di-ung", "Hộp 10 vỉ x 10 viên", 52000),
            NewProduct("MED-LOR-10", "Loratadine 10mg", "Loratadine", "Dị ứng, ngứa, mề đay, viêm mũi dị ứng", "di-ung", "Hộp 3 vỉ x 10 viên", 46000),
            NewProduct("MED-COR-C", "Vitamin C Corbière", "Vitamin C", "Thiếu vitamin C, mệt mỏi, tăng sức đề kháng", "vitamin-khoang-chat", "Hộp 30 ống", 135000),
            NewProduct("MED-CAL-D", "Calcium Corbière", "Calcium, Vitamin D3", "Thiếu canxi, loãng xương, phụ nữ mang thai", "vitamin-khoang-chat", "Hộp 30 ống", 165000),
            NewProduct("MED-ORS-20", "Oresol bù nước điện giải", "Glucose, điện giải", "Mất nước do tiêu chảy, nôn, sốt cao", "vitamin-khoang-chat", "Hộp 20 gói", 48000),
            NewProduct("MED-POV-10", "Povidone Iodine 10%", "Povidone iodine", "Sát khuẩn vết thương, trầy xước, bỏng nhẹ", "da-lieu", "Chai 20ml", 28000),
            NewProduct("MED-NAT-E", "Natri Clorid 0,9%", "Sodium chloride", "Rửa mắt, rửa mũi, nghẹt mũi, vệ sinh tai", "mat-va-tai", "Lọ 10ml", 9000),
            NewProduct("MED-TD-OIL", "Dầu gừng Thái Dương", "Tinh dầu gừng", "Đau đầu, đau lưng, đau cơ, lạnh bụng", "dau-xoa-giam-dau", "Chai 24ml", 80000),
            NewProduct("MED-SAL-5", "Salonpas miếng dán", "Methyl salicylate, Menthol", "Đau vai gáy, đau lưng, đau cơ, bong gân", "dau-xoa-giam-dau", "Hộp 10 miếng", 42000),
            NewProduct("MED-AZI-500", "Azithromycin 500mg", "Azithromycin", "Nhiễm khuẩn hô hấp, viêm họng, viêm phế quản", "thuoc-khang-sinh", "Hộp 1 vỉ x 3 viên", 97000, true)
        };
        foreach (var product in products.Where(product => !existingCodes.Contains(product.Code)))
        {
            context.Products.Add(product);
        }
        await context.SaveChangesAsync();
        await SeedSaleUnitsAsync(context);

        var branches = await context.Branches.Where(branch => branch.IsActive).ToListAsync();
        var activeProducts = await context.Products.Where(product => product.IsActive).ToListAsync();
        var nextYear = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2));
        foreach (var product in activeProducts)
        {
            product.Composition ??= product.ActiveIngredient;
            product.DosageForm ??= InferDosageForm(product.Packaging);
            product.UsageInstructions ??= product.RxFlag
                ? "Sử dụng đúng liều và thời gian theo đơn của bác sĩ hoặc hướng dẫn của dược sĩ."
                : "Đọc kỹ hướng dẫn trên bao bì và sử dụng đúng liều khuyến cáo; hỏi dược sĩ nếu cần tư vấn thêm.";
            product.Contraindications ??= "Không sử dụng nếu mẫn cảm với bất kỳ thành phần nào của sản phẩm.";
            var batch = await context.Batches.FirstOrDefaultAsync(item => item.ProductId == product.Id);
            if (batch is null)
            {
                batch = new Batch { ProductId = product.Id, BatchNumber = $"SEED-{product.Code}", MfgDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2)), ExpiryDate = nextYear, CostPrice = Math.Round(product.UnitPrice * 0.65m, 2) };
                context.Batches.Add(batch);
            }
            foreach (var branch in branches)
            {
                if (!await context.BranchInventories.AnyAsync(stock => stock.BranchId == branch.Id && stock.ProductId == product.Id && stock.BatchId == batch.Id))
                {
                    var baseUnitsPerPackage = await context.ProductSaleUnits
                        .Where(unit => unit.ProductId == product.Id)
                        .MaxAsync(unit => (int?)unit.ConversionFactor) ?? 1;
                    context.BranchInventories.Add(new BranchInventory { BranchId = branch.Id, ProductId = product.Id, BatchId = batch.Id, QuantityOnHand = baseUnitsPerPackage * 30, ReservedQuantity = 0, ReorderLevel = baseUnitsPerPackage * 5, Version = 1 });
                }
            }
        }
        await context.SaveChangesAsync();

        Product NewProduct(string code, string name, string ingredient, string indications, string categorySlug, string packaging, decimal price, bool rx = false) =>
            new() { Code = code, Name = name, ActiveIngredient = ingredient, Composition = ingredient, Indications = indications, DosageForm = InferDosageForm(packaging), CategoryId = Category(categorySlug), Packaging = packaging, UnitPrice = price, VatRate = 5, RxFlag = rx, WarningText = rx ? "Thuốc kê đơn, chỉ sử dụng theo hướng dẫn của bác sĩ hoặc dược sĩ." : "Đọc kỹ hướng dẫn sử dụng trước khi dùng." };

        static string InferDosageForm(string packaging) =>
            packaging.Contains("viên sủi", StringComparison.OrdinalIgnoreCase) ? "Viên sủi"
            : packaging.Contains("viên", StringComparison.OrdinalIgnoreCase) ? "Viên uống"
            : packaging.Contains("gói", StringComparison.OrdinalIgnoreCase) ? "Dạng gói"
            : packaging.Contains("ống", StringComparison.OrdinalIgnoreCase) ? "Dung dịch uống"
            : packaging.Contains("miếng", StringComparison.OrdinalIgnoreCase) ? "Miếng dùng ngoài"
            : packaging.Contains("chai", StringComparison.OrdinalIgnoreCase) || packaging.Contains("lọ", StringComparison.OrdinalIgnoreCase) ? "Dung dịch"
            : "Dạng dùng theo bao bì";
    }

    private static async Task SeedSaleUnitsAsync(AppDbContext context)
    {
        // Seed runs in the same long-lived initialization context. Clear already-saved
        // tracked graphs so inventory concurrency tokens cannot be updated twice.
        context.ChangeTracker.Clear();
        var products = await context.Products.AsNoTracking().ToListAsync();
        var existingUnits = await context.ProductSaleUnits.ToListAsync();
        foreach (var duplicateGroup in existingUnits.GroupBy(unit => new
                 { unit.ProductId, unit.ConversionFactor }).Where(group => group.Count() > 1))
        {
            var keep = duplicateGroup.FirstOrDefault(unit => unit.IsDefault) ?? duplicateGroup.First();
            foreach (var duplicate in duplicateGroup.Where(unit => unit.Id != keep.Id))
                duplicate.IsActive = false;
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var configuredProductIds = await context.ProductSaleUnits.AsNoTracking()
            .Select(unit => unit.ProductId).Distinct().ToHashSetAsync();
        foreach (var product in products.Where(product => !configuredProductIds.Contains(product.Id)))
        {
            var numbers = System.Text.RegularExpressions.Regex.Matches(product.Packaging, @"\d+")
                .Select(match => int.Parse(match.Value)).ToArray();
            var outerName = product.Packaging.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? "Đơn vị";
            var baseName = product.Packaging.Contains("viên", StringComparison.OrdinalIgnoreCase) ? "Viên"
                : product.Packaging.Contains("gói", StringComparison.OrdinalIgnoreCase) ? "Gói"
                : product.Packaging.Contains("ống", StringComparison.OrdinalIgnoreCase) ? "Ống"
                : product.Packaging.Contains("miếng", StringComparison.OrdinalIgnoreCase) ? "Miếng"
                : outerName;
            var totalBaseUnits = numbers.Length >= 2 ? checked(numbers[0] * numbers[1])
                : numbers.Length == 1 && baseName != outerName ? numbers[0] : 1;

            context.ProductSaleUnits.Add(new ProductSaleUnit
            {
                ProductId = product.Id, UnitName = outerName, ConversionFactor = totalBaseUnits,
                SalePrice = product.UnitPrice, IsDefault = true
            });
            if (product.Packaging.Contains("vỉ", StringComparison.OrdinalIgnoreCase) && numbers.Length >= 2)
            {
                context.ProductSaleUnits.Add(new ProductSaleUnit
                {
                    ProductId = product.Id, UnitName = "Vỉ", ConversionFactor = numbers[1],
                    SalePrice = RoundRetail(product.UnitPrice / numbers[0]), IsDefault = false
                });
            }
            if (baseName != outerName && totalBaseUnits > 1)
            {
                context.ProductSaleUnits.Add(new ProductSaleUnit
                {
                    ProductId = product.Id, UnitName = baseName, ConversionFactor = 1,
                    SalePrice = RoundRetail(product.UnitPrice / totalBaseUnits), IsDefault = false
                });
            }

            if (totalBaseUnits > 1)
            {
                await context.BranchInventories
                    .Where(stock => stock.ProductId == product.Id)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(stock => stock.QuantityOnHand,
                            stock => stock.QuantityOnHand * totalBaseUnits)
                        .SetProperty(stock => stock.ReservedQuantity,
                            stock => stock.ReservedQuantity * totalBaseUnits)
                        .SetProperty(stock => stock.ReorderLevel,
                            stock => stock.ReorderLevel * totalBaseUnits)
                        .SetProperty(stock => stock.Version, stock => stock.Version + 1));
            }
        }
        await context.SaveChangesAsync();

        static decimal RoundRetail(decimal value) =>
            Math.Max(0.01m, Math.Round(value, 2, MidpointRounding.AwayFromZero));
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
