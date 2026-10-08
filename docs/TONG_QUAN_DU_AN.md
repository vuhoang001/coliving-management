# Tổng quan dự án — Hệ thống quản lý & vận hành chuỗi căn hộ Co-living

Tài liệu kỹ thuật tóm tắt phục vụ báo cáo đồ án. Chi tiết chạy nhanh xem `README.md`.

## 1. Mục tiêu

Xây dựng hệ thống quản lý toàn bộ vòng đời vận hành chuỗi căn hộ dịch vụ theo mô hình co-living:
cơ sở vật chất & tài sản, khách thuê/ở ghép, đặt chỗ, hợp đồng điện tử, nhận/trả phòng, sự cố,
tiện ích chung, thuê dịch vụ, thông báo realtime, hoá đơn & thanh toán (có chia tiền ở ghép),
và báo cáo thống kê.

## 2. Kiến trúc

Clean Architecture 4 tầng (backend) + SPA (frontend):

```
Coliving.Domain          → Entities, Enums, BaseEntity (không phụ thuộc gì)
Coliving.Application     → DTOs, Interfaces, Services (logic nghiệp vụ); phụ thuộc Domain
Coliving.Infrastructure  → AppDbContext (EF Core), JWT, MinIO, Email; hiện thực các Interface
Coliving.API             → Controllers, Program.cs, SignalR Hub, Middleware, dịch vụ nền
```

- Tầng Application chỉ phụ thuộc trừu tượng (`IAppDbContext`, `IEmailSender`, `IFileStorage`,
  `IRealtimeNotifier`...) → dễ test, không bị khoá vào Infrastructure.
- **Công nghệ:** ASP.NET Core .NET 8, EF Core 8 + PostgreSQL (Npgsql), JWT (access + refresh
  xoay vòng), Serilog, Swagger, SignalR, MinIO (S3). Frontend: Vue 3 + TypeScript + Vite +
  PrimeVue + Pinia + vue-router + chart.js.
- **Schema:** tạo bằng `EnsureCreated` lúc khởi động + seed dữ liệu demo (có retry chờ DB sẵn sàng).

## 3. Các module nghiệp vụ

| Nghiệp vụ | Thành phần backend |
|---|---|
| Xác thực & phân quyền (Tenant/Staff/Manager/Admin) | AuthController, AuthService, JWT, RefreshToken, **quên/đặt lại mật khẩu** |
| Cơ sở vật chất | Buildings, Apartments, Rooms |
| Tài sản & danh mục dịch vụ | Assets, ServiceCatalog |
| Đặt chỗ | Bookings |
| Hợp đồng điện tử | Contracts (ký điện tử) |
| Nhận/Trả phòng | Check (chỉ số điện/nước, tình trạng phòng) |
| Sự cố | Incidents (ưu tiên, giao việc, gắn tài sản, ảnh MinIO) |
| Tiện ích chung | Amenities + AmenityBookings (kiểm tra sức chứa/khung giờ) |
| Thuê dịch vụ | ServiceRequests |
| Hoá đơn & thanh toán | Invoices + InvoiceShares (chia tiền ở ghép) + Payments (Cash/Chuyển khoản/VNPay) |
| Thông báo realtime | Notifications (lưu DB + đẩy SignalR) |
| Báo cáo thống kê | Dashboard (doanh thu, tỉ lệ lấp đầy, công nợ) |
| **Nhật ký thao tác** | AuditLogs (ai làm gì, khi nào) |

## 4. Tính năng kỹ thuật (cross-cutting)

- **Bảo mật:** JWT + refresh token xoay vòng; băm mật khẩu PBKDF2 (HMAC-SHA256);
  **rate limiting** (10 req/phút cho auth, 20 req/phút cho payment theo IP) chống dò mật khẩu/spam.
- **Email:** `IEmailSender` cắm được — `Log` (ghi console, demo) hoặc `Smtp` (gửi thật). Dùng cho
  luồng quên/đặt lại mật khẩu (token 1 giờ, thu hồi mọi refresh token cũ sau khi đổi).
- **Nhật ký:** audit log tự ghi khi tạo/sửa/đổi trạng thái người dùng; màn hình xem cho Manager/Admin.
- **Dịch vụ nền:** `InvoiceOverdueService` quét định kỳ (30 phút) → đánh dấu hoá đơn quá hạn +
  gửi thông báo nhắc khách thuê.
- **Lưu trữ file:** MinIO (S3) — upload ảnh sự cố/hợp đồng, trả URL công khai.
- **Soft-delete** toàn cục (query filter theo `DeletedAt`), **precision 18,2** cho mọi cột tiền tệ.
- **Thanh toán VNPay:** tích hợp sandbox thật (ký HMAC-SHA512 + xử lý Return/IPN) với fallback chế độ
  mock khi chưa cấu hình credential.

## 5. Kiểm thử (tests)

Dự án `backend/tests/Coliving.Tests` (xUnit + SQLite in-memory, cô lập từng test):

- `PasswordHasherTests` — băm/verify, salt ngẫu nhiên, hash hỏng.
- `AuthServiceTests` — đăng ký (băm mật khẩu, vai trò Tenant), trùng email, sai mật khẩu,
  luồng quên→đặt lại mật khẩu (đổi mật khẩu + thu hồi refresh token).
- `InvoiceSplitTests` — **chia tiền hoá đơn**: chia đều, chia hỗn hợp (chỉ định + tự động),
  bù chênh lệch làm tròn, vượt tổng, người ở ghép không tồn tại, danh sách rỗng.

Chạy: `cd backend && dotnet test` → **17 test, 100% pass**.

## 6. CI/CD

`.github/workflows/ci.yml` — mỗi push/PR lên `main`/`master`:
- Backend: `dotnet restore` → `build -c Release` → **`dotnet test`**.
- Frontend: `npm ci` → `npm run build` (type-check `vue-tsc` + Vite).

## 7. Vận hành

- **Chạy:** `docker compose up -d --build` → web `:5175`, API/Swagger `:8082`, MinIO console `:9005`, DB `:5434`.
- **Sao lưu/di chuyển dữ liệu:** `./scripts/export-data.sh` (dump Postgres + ảnh MinIO ra `./backup`),
  `./scripts/import-data.sh` (nạp lại ở máy mới).
- **Cấu hình nhạy cảm:** qua `.env` (xem `.env.example`) — JWT secret, MinIO, VNPay, Email/SMTP.

## 8. Hướng phát triển tiếp

- Chuyển `EnsureCreated` → **EF Core Migrations** khi cần quản lý tiến hoá schema ở môi trường thật.
- Bổ sung integration test (WebApplicationFactory) cho các endpoint chính.
- Tích hợp VNPay sandbox thật bằng credential (hiện chạy mock khi chưa cấu hình).
