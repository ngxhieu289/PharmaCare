# Kế hoạch làm việc nhóm PharmaCare

Thư mục này dùng để quản lý tiến độ 4 tuần của nhóm 3 thành viên.

## Quy tắc quan trọng

- Không sao chép mã nguồn vào thư mục tuần. Mã nguồn phải được sửa tại đúng vị trí thật như `Controllers/`, `Services/` hoặc `pharmacare-frontend/src/`.
- Thư mục của từng thành viên dùng để cập nhật checklist, ghi kết quả test và lưu ảnh minh chứng.
- Mỗi thành viên làm trên branch riêng và tạo Pull Request vào `develop`.
- Không đẩy trực tiếp vào `main`.
- Không commit mật khẩu PostgreSQL, JWT key, `.env.local`, `node_modules`, `dist`, `bin`, `obj` hoặc ảnh đơn thuốc thật.

## Cấu trúc

```text
Docs/KeHoachNhom/
├── Tuan-01/
│   ├── Thanh-vien-1/README.md
│   ├── Thanh-vien-2/README.md
│   └── Thanh-vien-3/README.md
├── Tuan-02/
├── Tuan-03/
└── Tuan-04/
```

## Mỗi thành viên phải cập nhật gì?

Trong `README.md` của mình, thành viên phải điền:

1. Họ tên và tài khoản GitHub.
2. Các task đã hoàn thành.
3. Danh sách file mã nguồn đã thay đổi.
4. Các lệnh kiểm thử đã chạy và kết quả.
5. Link commit.
6. Link Pull Request.
7. Lỗi còn lại hoặc việc chuyển sang tuần sau.

Ảnh minh chứng có thể đặt cùng thư mục và đặt tên:

```text
01-man-hinh-chinh.png
02-ket-qua-test.png
03-swagger-api.png
```

## Quy trình Git mỗi tuần

Thay tên branch bằng tên ghi trong thư mục tuần của mình.

```bash
git switch develop
git pull origin develop
git switch -c TEN_BRANCH
```

Sau khi hoàn thành:

```bash
git status
git add DUONG_DAN_FILE_DA_SUA
git add Docs/KeHoachNhom/Tuan-XX/Thanh-vien-X
git commit -m "feat(module): mô tả công việc"
git push -u origin TEN_BRANCH
```

Sau đó tạo Pull Request trên GitHub:

```text
TEN_BRANCH → develop
```

## Kiểm tra bắt buộc trước Pull Request

Backend:

```bash
dotnet build
dotnet test
```

Frontend:

```bash
cd pharmacare-frontend
npm run lint
npm run build
```

## Quy trình trưởng nhóm cuối tuần

1. Review ba Pull Request.
2. Yêu cầu sửa nếu build/test thất bại.
3. Merge đủ ba Pull Request vào `develop`.
4. Chạy lại kiểm thử toàn hệ thống.
5. Tạo tag `week-1`, `week-2`, `week-3`, `week-4`.

```bash
git switch develop
git pull origin develop
git tag -a week-1 -m "PharmaCare weekly release 1"
git push origin week-1
```

Tuần 4 tạo Pull Request `develop → main`, sau đó tạo tag `v1.0.0`.
