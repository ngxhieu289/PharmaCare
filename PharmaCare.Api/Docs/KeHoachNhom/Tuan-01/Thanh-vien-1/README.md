# Tuần 1 - Thành viên 1 (Trưởng nhóm)

- Thời gian: 30/08/2026 - 05/09/2026
- Branch: `feat/tv1-w1-auth-hardening`
- Phạm vi: nền tảng backend, database, JWT và RBAC.

## Công việc

- [x] Chốt baseline hiện tại và bảo đảm project clone về chạy được.
- [x] Kiểm tra cấu hình PostgreSQL, migration và seed data.
- [x] Kiểm tra đăng nhập, Access Token, Refresh Token và revoke.
- [x] Kiểm tra năm role: Admin, BranchManager, Pharmacist, WarehouseStaff, Customer.
- [x] Kiểm tra Swagger và tài khoản demo.
- [x] Cập nhật README hướng dẫn chạy backend/frontend.

## File dự kiến

```text
Program.cs
Authorization/
Data/AppDbContext.cs
Data/DbInitializer.cs
Services/AuthService.cs
Services/TokenService.cs
Controllers/AuthController.cs
README.md
```

## Minh chứng phải đẩy

- Ảnh Swagger mở thành công.
- Ảnh hoặc log đăng nhập và refresh thành công.
- Log `dotnet build` và `dotnet test`.
- Link commit và Pull Request.

Kết quả kiểm thử dạng văn bản được lưu tại `TEST_RESULTS.md`. Trước khi tạo Pull
Request, trưởng nhóm chụp thêm Swagger trên máy của mình để làm minh chứng giao
diện, không chụp token hoặc mật khẩu.

## Kết quả thực tế

- Họ tên/GitHub: Nguyễn Xuân Hiếu / `ngxhieu289`
- File đã sửa: `Program.cs`, `Authorization/TokenClaimTypes.cs`, `Entities/User.cs`,
  `Data/AppDbContext.cs`, `Data/AppDbContextFactory.cs`, `Services/AuthService.cs`,
  `Services/TokenService.cs`, `Services/IUserSessionService.cs`,
  `Services/UserSessionService.cs`, `Controllers/UsersController.cs`,
  `Controllers/RolesController.cs`, migration `AddUserTokenVersion`, `README.md`,
  `scripts/auth-rbac-regression.sh`.
- Kết quả test: build backend 0 lỗi/0 cảnh báo; migration và seed chạy thành
  công trên database tạm; Swagger HTTP 200; 10 kiểm tra ranh giới role đạt;
  11 kiểm tra login/token-version/refresh/replay/revoke đạt; frontend lint và
  production build đạt.
- Link commit: điền sau khi thực hiện bước commit thủ công.
- Link Pull Request: điền sau khi tạo Pull Request vào `develop`.
- Việc còn lại: chụp ảnh Swagger, commit, push và tạo Pull Request.
