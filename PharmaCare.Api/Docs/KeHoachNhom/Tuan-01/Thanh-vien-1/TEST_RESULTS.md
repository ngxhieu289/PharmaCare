# Kết quả kiểm thử Tuần 1 - Thành viên 1

Ngày chạy: 30/08/2026  
Branch: `feat/tv1-w1-auth-hardening`

Kiểm thử được chạy trên database PostgreSQL tạm `pharmacare_week1_test`. Database
tạm đã được xóa sau khi hoàn thành; không tác động database làm việc hiện tại.

## Build và migration

```text
dotnet build --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)

EF Core: áp dụng thành công toàn bộ migration, bao gồm AddUserTokenVersion.
Migration script idempotent: tạo thành công, 1697 dòng.
Swagger: HTTP 200.
```

`dotnet test --no-restore` kết thúc mã 0. Repository hiện chưa có test project
.NET riêng; các kiểm thử hành vi được thực hiện bằng hai regression script dưới
đây.

## Ranh giới role

Chạy `scripts/system-regression.sh`:

```text
PASS Admin /me                                      HTTP 200
PASS Admin đọc tài khoản                           HTTP 200
PASS Admin đọc vai trò                             HTTP 200
PASS Admin đọc ma trận quyền                       HTTP 200
PASS Admin đọc audit                               HTTP 200
PASS Manager không quản trị user                   HTTP 403
PASS Manager đọc báo cáo                           HTTP 200
PASS Dược sĩ không quản trị user                   HTTP 403
PASS Dược sĩ đọc đơn hàng                          HTTP 200
PASS Nhân viên kho không đọc đơn                   HTTP 403
PASS Nhân viên kho đọc tồn                         HTTP 200
Result: PASS; roleBoundaryChecks: 10
```

Role `Customer` được tạo và kiểm tra trong regression xác thực; endpoint đăng ký
chỉ gán role Customer và các permission mặc định của role này.

## JWT, refresh rotation và revoke

Chạy `scripts/auth-rbac-regression.sh`:

```text
PASS Đăng ký Customer phục vụ kiểm thử              HTTP 200
PASS Access token mới truy cập /auth/me             HTTP 200
PASS Admin khóa tài khoản thử nghiệm                HTTP 204
PASS Access token cũ mất hiệu lực ngay              HTTP 401
PASS Refresh token bị thu hồi khi khóa user         HTTP 401
PASS Admin mở lại tài khoản thử nghiệm              HTTP 204
PASS Refresh token rotation lần đầu                 HTTP 200
PASS Phát lại refresh token cũ bị từ chối           HTTP 401
PASS Token thay thế bị thu hồi khi phát hiện replay HTTP 401
PASS Chủ động revoke refresh token                  HTTP 200
PASS Refresh sau revoke bị từ chối                  HTTP 401
```

## Frontend compatibility

```text
npm run lint  -> PASS
npm run build -> PASS
117 modules transformed; production bundle generated successfully.
```

Không có mật khẩu, JWT key hoặc connection string bí mật được ghi vào tài liệu
này.
