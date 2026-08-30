# Tuần 1 - Nguyễn Xuân Hiếu

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

## Phạm vi mã nguồn phụ trách

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

## Minh chứng

- Swagger hoạt động thành công với HTTP 200.
- Đăng nhập, refresh token rotation và revoke token đều kiểm thử thành công.
- `dotnet build` hoàn thành với 0 lỗi và 0 cảnh báo.
- Kết quả kiểm thử chi tiết được lưu tại [`TEST_RESULTS.md`](./TEST_RESULTS.md).
- Commit và Pull Request được liên kết trong phần kết quả thực tế bên dưới.

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
- Link commit: https://github.com/ngxhieu289/PharmaCare/commit/8a60324cdee455b300f3443644609259cfb2545b
- Link Pull Request: https://github.com/ngxhieu289/PharmaCare/pull/3
- Việc còn lại: Không còn công việc mã nguồn trong phạm vi Tuần 1.
