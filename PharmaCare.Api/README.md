# PharmaCare API

Backend ASP.NET Core và PostgreSQL cho hệ thống quản lý nhà thuốc PharmaCare.

## Chạy local

Yêu cầu .NET SDK 10 và PostgreSQL. Cập nhật connection string bằng environment
variable nếu cấu hình PostgreSQL trên máy khác với giá trị development mẫu.

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=pharmacare_db;Username=postgres;Password=postgres'
export Jwt__Key='replace-with-a-local-signing-key-at-least-32-characters'
export BootstrapAdmin__Email='admin@pharmacare.local'
export BootstrapAdmin__Password='replace-with-a-strong-local-password'
dotnet run
```

Ứng dụng tự áp dụng EF Core migrations khi khởi động. Swagger mặc định nằm tại
`/swagger`.

Không commit mật khẩu, JWT signing key production hoặc connection string
production vào repository.
