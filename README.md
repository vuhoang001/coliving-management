# Coliving — Hệ thống quản lý & vận hành chuỗi căn hộ dịch vụ (mô hình Co-living)

Đồ án: **Xây dựng hệ thống quản lý và vận hành chuỗi căn hộ dịch vụ theo mô hình Co-living.**

Hệ thống quản lý toàn bộ vòng đời vận hành: cơ sở vật chất & tài sản, khách thuê/ở ghép, đặt chỗ,
hợp đồng điện tử, nhận/trả phòng, tiếp nhận & xử lý sự cố, đặt lịch tiện ích chung, thuê dịch vụ,
thông báo realtime, hoá đơn & thanh toán (có chia tiền theo người ở ghép), và báo cáo thống kê.

## Kiến trúc & công nghệ

Cùng stack với dự án `phonestore` (Clean Architecture):

- **Backend:** ASP.NET Core .NET 8 — 4 tầng `Domain / Application / Infrastructure / API`
  - EF Core 8 + Npgsql (PostgreSQL), `EnsureCreated` + seed lúc khởi động
  - JWT (access + refresh token xoay vòng), phân quyền theo vai trò
  - Swagger, Serilog, MinIO (lưu ảnh), SignalR (thông báo realtime)
- **Frontend:** Vue 3 + TypeScript + Vite + PrimeVue + Pinia + vue-router + axios + chart.js + SignalR client
- **Hạ tầng:** Docker Compose — `postgres:16`, `minio`, `api`, `web (nginx)`

## Nghiệp vụ (bám theo yêu cầu đề tài)

| Nghiệp vụ | Module |
|---|---|
| Quản lý cơ sở vật chất, trang thiết bị, tài sản, dịch vụ | Buildings, Apartments, Rooms, Assets, Services |
| Quản lý khách thuê / ở ghép, đặt chỗ, hợp đồng điện tử | Users (Tenant), Bookings, Contracts (ký điện tử) |
| Quản lý check-in / check-out | Check (ghi chỉ số điện/nước, tình trạng phòng) |
| Tiếp nhận & xử lý sự cố | Incidents (ưu tiên, giao việc, gắn tài sản) |
| Đặt lịch dùng tiện ích chung (bể bơi, gym, BBQ...) | Amenities + AmenityBookings (kiểm tra sức chứa/khung giờ) |
| Thuê dịch vụ (giặt đồ, vệ sinh...) | ServiceCatalog + ServiceRequests |
| Quản lý thông báo | Notifications (lưu DB + đẩy realtime SignalR) |
| Quản lý hoá đơn & thanh toán (chia tiền phòng/dịch vụ) | Invoices + InvoiceShares + Payments (Cash/Chuyển khoản/VNPay mock) |
| Báo cáo thống kê | Dashboard (doanh thu, tỉ lệ lấp đầy, công nợ...) |

## Chạy nhanh (Docker)

```bash
cp .env.example .env          # tuỳ chọn: chỉnh secret
docker compose up -d --build
```

| Dịch vụ | URL |
|---|---|
| Web (frontend) | http://localhost:5175 |
| API + Swagger | http://localhost:8082/swagger |
| MinIO Console | http://localhost:9005 (minioadmin / minioadmin) |
| PostgreSQL | localhost:5434 (postgres / postgres) |

> Cổng chọn lệch để chạy song song với các dự án khác (`hai`=5432, `phonestore`=5433 → dự án này DB=5434).

Dừng: `docker compose down` (thêm `-v` để xoá dữ liệu).

## Tài khoản demo (được seed sẵn)

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | admin@coliving.local | Admin@123 |
| Quản lý | manager@coliving.local | Manager@123 |
| Nhân viên | staff@coliving.local | Staff@123 |
| Khách thuê | an@coliving.local | Tenant@123 |

## Cấu trúc thư mục

```
coliving-management/
├─ backend/
│  ├─ Dockerfile, Coliving.sln
│  └─ src/
│     ├─ Coliving.Domain/          # Entities, Enums, BaseEntity
│     ├─ Coliving.Application/      # DTOs, Interfaces, Services (nghiệp vụ)
│     ├─ Coliving.Infrastructure/  # AppDbContext, JWT, MinIO, DbSeeder
│     └─ Coliving.API/             # Controllers, Program.cs, SignalR Hub
├─ frontend/                       # Vue 3 + PrimeVue SPA
├─ docker-compose.yml
└─ .env.example
```

## Tính năng kỹ thuật bổ sung

- **Kiểm thử:** `backend/tests/Coliving.Tests` (xUnit + SQLite in-memory) — chạy `cd backend && dotnet test` (17 test, phủ hashing, auth, quên/đặt lại mật khẩu, chia tiền hoá đơn).
- **CI:** `.github/workflows/ci.yml` tự build + test backend và build frontend mỗi push/PR.
- **Sao lưu/di chuyển:** `./scripts/export-data.sh` và `./scripts/import-data.sh` (Postgres + ảnh MinIO).
- **Bảo mật:** rate limiting cho endpoint auth (10/phút) và payment (20/phút).
- **Email:** quên/đặt lại mật khẩu (`/forgot-password`, `/reset-password`); mặc định ghi log (demo), đổi `EMAIL_PROVIDER=Smtp` để gửi thật.
- **Nhật ký thao tác:** màn hình *Nhật ký* cho Manager/Admin (AuditLogs).
- **Dịch vụ nền:** tự đánh dấu hoá đơn quá hạn và nhắc khách thuê (mỗi 30 phút).
- Tài liệu kỹ thuật tổng quan: `docs/TONG_QUAN_DU_AN.md`.

## Ghi chú

- VNPay chạy ở **chế độ mock** khi chưa cấu hình `VNPAY_TMNCODE`/`HASHSECRET` thật.
- Ảnh (sự cố, toà nhà, hợp đồng...) tải lên MinIO qua `POST /api/uploads/image`.
- Dự án được scaffold kèm bộ **vibecode-pro-max-kit** (RIPER-5) trong `.claude/` — chạy `vc-setup`
  trong Claude Code để bật quy trình phát triển có kiểm soát.
