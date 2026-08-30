# PharmaCare

Hệ thống quản lý chuỗi nhà thuốc gồm backend ASP.NET Core, PostgreSQL và frontend
React/Vite. Backend quản lý xác thực JWT, phân quyền RBAC, sản phẩm, lô thuốc,
tồn kho, đơn thuốc, đơn hàng, voucher, thanh toán, báo cáo và audit log.

## Công nghệ và cấu trúc

- Backend: .NET 10, ASP.NET Core Web API, Entity Framework Core.
- Database: PostgreSQL; migration nằm trong `Migrations/`.
- Frontend: React, TypeScript, Vite, Zustand và Axios.
- API documentation: Swagger tại `/swagger`.
- Phân quyền: năm role `Admin`, `BranchManager`, `Pharmacist`,
  `WarehouseStaff`, `Customer` và các permission trong
  `Authorization/PermissionCodes.cs`.

## 1. Chuẩn bị máy

Cần cài .NET SDK 10, PostgreSQL, Node.js và npm. `curl` cùng `jq` được dùng bởi
các regression script.

```bash
dotnet --version
node --version
npm --version
pg_isready -h 127.0.0.1 -p 5432
```

## 2. Tạo database local

```bash
createdb -h localhost -U postgres pharmacare_db
```

Nếu database đã tồn tại, bỏ qua bước này. Không lưu mật khẩu PostgreSQL vào Git.

## 3. Cấu hình backend

Chạy các lệnh dưới đây trong **cùng Terminal** sẽ chạy backend. Thay các giá trị
`YOUR_...` bằng thông tin local của người đang chạy:

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=pharmacare_db;Username=postgres;Password=YOUR_POSTGRES_PASSWORD'
export Jwt__Key='YOUR_LOCAL_JWT_SIGNING_KEY_AT_LEAST_32_CHARACTERS'
export BootstrapAdmin__Email='admin@pharmacare.local'
export BootstrapAdmin__Password='YOUR_ADMIN_PASSWORD'
```

Muốn tạo tài khoản demo cho quá trình kiểm thử, cấu hình thêm:

```bash
export DemoAccounts__Enabled='true'
export DemoAccounts__AdminPassword='YOUR_ADMIN_PASSWORD'
export DemoAccounts__BranchManagerPassword='YOUR_MANAGER_PASSWORD'
export DemoAccounts__PharmacistPassword='YOUR_PHARMACIST_PASSWORD'
export DemoAccounts__WarehousePassword='YOUR_WAREHOUSE_PASSWORD'
```

| Role | Username | Email |
| --- | --- | --- |
| Admin | `admin` | `admin@pharmacare.local` |
| BranchManager | `manager` | `manager@pharmacare.local` |
| Pharmacist | `pharmacist` | `pharmacist@pharmacare.local` |
| WarehouseStaff | `warehouse` | `warehouse@pharmacare.local` |

Mật khẩu là giá trị từng người tự đặt bằng biến môi trường, không được commit.

## 4. Chạy backend

Từ thư mục `PharmaCare.Api`:

```bash
dotnet restore
dotnet run --urls http://127.0.0.1:5080
```

Ứng dụng tự áp dụng migration và seed dữ liệu nền. Swagger:

```text
http://127.0.0.1:5080/swagger
```

Nếu báo `Database connection is missing`, biến
`ConnectionStrings__DefaultConnection` chưa được đặt trong Terminal hiện tại.

## 5. Chạy frontend

Mở Terminal thứ hai:

```bash
cd pharmacare-frontend
npm install
npm run dev
```

Mở `http://localhost:3000`. Vite proxy chuyển `/api` đến
`http://127.0.0.1:5080`, vì vậy backend phải đang chạy trước.

## 6. Xác thực và bảo mật phiên

1. Login xác minh password hash và phát access token + refresh token.
2. Access token chứa role, permission và `token_version`.
3. Mỗi request xác thực đối chiếu `token_version` và trạng thái user trong DB.
4. Khóa/mở user, đổi role/quyền hoặc đổi mật khẩu sẽ tăng phiên bản và thu hồi
   refresh token; access token cũ mất hiệu lực ngay.
5. Refresh token được lưu dưới dạng SHA-256 hash và được rotate sau mỗi lần dùng.
6. Nếu refresh token cũ bị phát lại, toàn bộ phiên của user bị thu hồi.

## 7. Build và kiểm thử

```bash
dotnet build
dotnet test
bash -n scripts/auth-rbac-regression.sh
```

Sau khi backend và tài khoản demo đang chạy, kiểm tra ranh giới năm role:

```bash
export PHARMACARE_ADMIN_PASSWORD='YOUR_ADMIN_PASSWORD'
export PHARMACARE_MANAGER_PASSWORD='YOUR_MANAGER_PASSWORD'
export PHARMACARE_PHARMACIST_PASSWORD='YOUR_PHARMACIST_PASSWORD'
export PHARMACARE_WAREHOUSE_PASSWORD='YOUR_WAREHOUSE_PASSWORD'
./scripts/system-regression.sh
```

Kiểm tra login, khóa tài khoản, token version, refresh rotation/replay và revoke:

```bash
export PHARMACARE_ADMIN_PASSWORD='YOUR_ADMIN_PASSWORD'
./scripts/auth-rbac-regression.sh
```

Kiểm tra frontend:

```bash
cd pharmacare-frontend
npm run lint
npm run build
```

## 8. Quy tắc bảo mật repository

Không commit mật khẩu PostgreSQL, JWT key, `.env.local`, `node_modules`, `dist`,
`bin`, `obj`, ảnh đơn thuốc hoặc dữ liệu cá nhân thật. Mỗi thành viên làm trên
branch riêng và tạo Pull Request vào `develop`. Kế hoạch chi tiết nằm trong
`Docs/KeHoachNhom/`.
