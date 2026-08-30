# PharmaCare — Frontend AI Handoff

## Mục tiêu

Xây frontend dựa đúng trên backend ASP.NET Core hiện có. Không tự suy đoán route,
DTO, permission hoặc trạng thái nghiệp vụ.

## Trình tự đọc bắt buộc

1. `README.md`, `Program.cs`, `appsettings.json`.
2. `Authorization/PermissionCodes.cs`.
3. Toàn bộ `Controllers/*.cs` để lập danh mục API.
4. Toàn bộ `Dtos/**/*.cs` để tạo TypeScript types.
5. `Data/DbInitializer.cs` để hiểu role–permission mặc định.
6. `Entities/*.cs` để hiểu trạng thái và quan hệ.
7. `Services/*.cs` để hiểu business rule và lỗi 409.
8. `Data/AppDbContext.cs` để hiểu constraint, FK và concurrency.

## Quy tắc tuyệt đối

- Route và HTTP method chỉ lấy từ Controllers.
- Request/response chỉ lấy từ DTOs.
- Permission chỉ lấy từ `PermissionCodes` và `DbInitializer`.
- Không tự tạo `/api/cart`, `/api/checkout`, `/api/profile` hay API khác.
- Cart giữ ở frontend; checkout dùng `POST /api/orders`.
- FEFO, giữ tồn, tính VAT/voucher và kiểm Rx do backend xử lý.
- Xử lý riêng 400, 401, 403, 404, 409.
- JWT gửi bằng `Authorization: Bearer`.
- Implement refresh rotation; refresh lỗi thì xóa phiên và về login.
- Menu và nút theo permission claim; vẫn xử lý 403 từ server.
- Không hard-code UUID, token, branch hoặc permission.
- Không commit `.env` hay secret.

## Vai trò

- `Customer`: catalog, prescription, checkout, order của chính mình.
- `Pharmacist`: xem tồn, review prescription, xử lý order/payment theo branch.
- `WarehouseStaff`: catalog, batch, receive/adjust/transfer inventory.
- `BranchManager`: kho, order, voucher, report theo branch; không review Rx mặc định.
- `Admin`: toàn bộ permission và phạm vi toàn hệ thống.

## Business rules phải phản ánh trên UI

- Thuốc Rx cần prescription `APPROVED` đúng customer, branch, product và quantity.
- Tạo order tăng reserved; cancel release; complete trừ reserved và on-hand.
- Order: `PENDING → CONFIRMED → COMPLETED` hoặc `CANCELLED`.
- VIETQR phải `PAID` trước khi complete.
- Voucher có validity, assignment, min amount, cap và usage limits.
- Customer không được confirm/complete order.
- Nhân viên chỉ truy cập branch được phân công.
- 401 = chưa xác thực; 403 = thiếu quyền; 409 = xung đột nghiệp vụ/concurrency.

## Việc phải làm trước khi xây UI

Tạo và trình bày để review:

1. `docs/backend-api-inventory.md`: method, route, permission, query, request,
   response, status code cho mọi endpoint.
2. `src/types/api.ts`: TypeScript types khớp DTO.
3. `src/api/httpClient.ts`: Bearer token, refresh queue, error normalization.
4. `src/auth`: auth store, permission guard, protected route.
5. Danh sách màn hình và mapping màn hình → API → permission.

Chỉ bắt đầu component/page sau khi hoàn thành năm đầu ra trên.

## Kiến trúc frontend đề xuất

```text
frontend-customer/   # website mua thuốc
frontend-admin/      # dược sĩ, kho, quản lý, Admin
```

Mỗi ứng dụng có `src/api`, `src/types`, `src/auth`, `src/modules`, `src/routes`
và `.env.example` riêng.

Backend đã cấu hình CORS qua `Cors:AllowedOrigins`. Development mặc định cho phép
`http://localhost:3000` và `http://localhost:5173`. Có thể dùng Vite proxy `/api`;
production nên cấu hình origin thật hoặc reverse proxy cùng origin.

## Xác nhận cấu trúc và các điểm dễ hiểu nhầm

- File này nằm tại root repository: `FRONTEND_AI_HANDOFF.md`.
- Không có `Entities/Inventory.cs`. Tồn kho hiện tại là
  `Entities/BranchInventory.cs`, gồm khóa ghép `BranchId + ProductId + BatchId`.
  Lịch sử biến động kho nằm trong `Entities/InventoryTransaction.cs`.
- Tất cả request/response DTO nằm trong `Dtos/`; DTO xác thực nằm trong
  `Dtos/Auth/`, kiểu phân trang nằm trong `Dtos/Common/`.
- Các interface backend nằm trong `Services/`: `IAuthService.cs`,
  `IInventoryService.cs`, `IOrderService.cs`, `IBranchAccessService.cs`,
  `IPrescriptionFileStorage.cs` và `ITokenService.cs`.
- Không tạo entity Inventory hoặc đoán DTO thay thế. Luôn map TypeScript từ DTO
  thật và Swagger của backend.

## Luồng frontend đã được backend xác nhận

- Cart chỉ là state phía frontend. Khi checkout, gửi toàn bộ `items` trong
  `CreateOrderRequest` đến `POST /api/orders`; backend kiểm tồn, chọn lô FEFO,
  giữ hàng, kiểm thuốc Rx, tính VAT, phí giao hàng và voucher.
- Không có `/api/checkout`. `POST /api/orders` chính là thao tác checkout.
- COD và VIETQR đều được chọn qua `paymentMethod`. Backend hiện chưa có callback
  cổng thanh toán/QR tự động. Nhân viên xác nhận VIETQR bằng
  `POST /api/orders/{id}/payments/confirm` trước khi hoàn thành đơn.
- Hồ sơ cá nhân dùng `GET /api/auth/me` và `PUT /api/auth/me`; không dùng
  `/api/profile` và không cần lấy user id từ frontend để gọi `/api/users/{id}`.
- JWT chứa nhiều claim có type chính xác là `permission`, mỗi claim mang một mã
  như `products.read`. Role dùng claim chuẩn `ClaimTypes.Role`.
- `IBranchAccessService`: Admin truy cập mọi chi nhánh; user khác chỉ truy cập
  chi nhánh có bản ghi `UserBranch`. Frontend chỉ ẩn/khóa UI; backend vẫn là nơi
  bắt buộc kiểm soát quyền và phạm vi chi nhánh.
- Refresh token rotation đã triển khai: token cũ được đặt `RevokedAt`, lưu hash
  token thay thế tại `ReplacedByTokenHash`, và không dùng lại được.
- Upload đơn thuốc dùng `multipart/form-data`, field `image`; chấp nhận JPEG,
  PNG, WebP, kiểm cả MIME và chữ ký file, tối đa 5 MB. Mặc định lưu tại
  `Storage/Prescriptions`; xem ảnh qua `GET /api/prescriptions/{id}/image`.
- Voucher validation yêu cầu đăng nhập:
  `GET /api/vouchers/validate/{code}?orderAmount=...`.
- Khách xem tồn khả dụng bằng
  `GET /api/products/{id}/availability?branchId=...`, không gọi API kho nội bộ.

## Câu hỏi kiểm tra hiểu dự án

AI phải trả lời đúng trước khi code:

1. Tạo order qua endpoint nào? — `POST /api/orders`.
2. Cart ở đâu? — Frontend state, backend chưa có Cart.
3. Ai xử lý FEFO? — Backend.
4. Tạo order có trừ on-hand ngay không? — Không, chỉ reserve.
5. BranchManager có review Rx mặc định không? — Không.
6. Customer có confirm order không? — Không.
7. Complete VIETQR cần gì? — Payment status `PAID`.
8. 401/403/409 khác nhau thế nào? — Xác thực/quyền/xung đột nghiệp vụ.

## Tài liệu bổ sung trong gói

- `Docs/PharmaCare_Ho_so_Phan_tich_Thiet_ke_HTTT.docx`: hồ sơ phân tích đầy đủ.
- `PharmaCare.Api.http`: HTTP request mẫu hiện có.
- Swagger được bật tại `/swagger` khi backend chạy.
