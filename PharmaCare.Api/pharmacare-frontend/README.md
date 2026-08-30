# PharmaCare Frontend

React + TypeScript + Vite + TailwindCSS frontend cho PharmaCare API.

## Customer Portal

- Đăng ký, đăng nhập, tự xoay refresh token và cập nhật hồ sơ.
- Catalog có tìm kiếm, lọc danh mục, phân trang và chi tiết thuốc.
- Chọn chi nhánh, xem tồn khả dụng và quản lý giỏ hàng cục bộ.
- Upload/theo dõi đơn thuốc và xem lại ảnh đã gửi.
- Checkout giao hàng/nhận tại cửa hàng, COD/VIETQR, kiểm voucher và thuốc Rx.
- Xem lịch sử, chi tiết, tiến trình và hủy đơn hợp lệ.

## Chạy development

Backend mặc định được proxy tới `http://127.0.0.1:5080`. Cổng 5000 trên macOS
thường bị ControlCenter/AirPlay chiếm dụng. Có thể đổi bằng cách
sao chép `.env.example` thành `.env.local` và cập nhật `VITE_BACKEND_URL`.

```bash
npm install
npm run dev
```

Mở `http://localhost:3000`. Backend cần có PostgreSQL, JWT key và tài khoản seed
được cấu hình theo README ở thư mục cha.

## Build production

```bash
npm run build
```

Không commit `.env`, token, mật khẩu hoặc thư mục `node_modules`.
