# Huyết Mạch 175 - Blood Bank Management System

[![React](https://img.shields.io/badge/React-61DAFB?logo=react&logoColor=white)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![Vite](https://img.shields.io/badge/Vite-646CFF?logo=vite&logoColor=white)](https://vite.dev/)
[![Ant Design](https://img.shields.io/badge/Ant_Design-0170FE?logo=antdesign&logoColor=white)](https://ant.design/)
[![.NET](https://img.shields.io/badge/.NET_9%2F10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)

---

## 1. Tổng quan hệ thống (Overview)

**Huyết Mạch 175** là giải pháp phần mềm quản trị toàn diện chu trình lưu thông máu tại Bệnh viện Quân y 175, bao gồm các phân hệ: tiếp nhận người hiến máu, điều chế - lưu trữ chế phẩm, xét nghiệm sàng lọc túi máu và tiếp nhận y lệnh cấp phát lâm sàng khẩn cấp/thường quy[cite: 1, 4].

Hệ thống được tổ chức theo mô hình **Monorepo** với kiến trúc **Modular Monolith** kết hợp nguyên lý **Domain-Driven Design (DDD)** và **Clean Architecture**:

- **Backend:** .NET Web API, Entity Framework Core (PostgreSQL Provider), MediatR (CQRS), FluentValidation, SignalR, JWT Authentication.
- **Frontend:** React, TypeScript, Vite, Ant Design (AntD), TanStack Query, Zustand, React Hook Form, Zod.
- **Database & Hạ tầng:** PostgreSQL 16+ (Dockerized container) tận dụng cấu trúc JSONB và Partial Unique Indexes để bảo toàn dữ liệu[cite: 4, 6].

---

## 2. Cấu trúc thư mục dự án (Repository Structure)

```text
huyet-mach-175/
├── database/
│   └── scripts/
│       └── init-schema.sql             # Script khởi tạo 16 bảng CSDL và chỉ mục
├── src/                                # Source code Backend (.NET)
│   ├── HuyetMach175.Api/               # Entry point Web API, cấu hình CORS, DI, Pipeline
│   ├── HuyetMach175.SharedKernel/      # Base entities, CQRS contracts, mẫu dùng chung
│   ├── HuyetMach175.Modules.Auth/      # Phân hệ RBAC, người dùng, phòng ban, JWT
│   ├── HuyetMach175.Modules.Donation/  # Phân hệ tiếp nhận, khảo sát, phiên lấy máu
│   ├── HuyetMach175.Modules.Inventory/ # Phân hệ kho chế phẩm, lưu trữ, xét nghiệm Lab
│   ├── HuyetMach175.Modules.Clinical/  # Phân hệ yêu cầu máu, giữ chỗ (Allocations), hoàn trả
│   └── HuyetMach175.Modules.Audit/     # Phân hệ kiểm toán biến động (Append-only)
├── client/                             # Source code Frontend (React + TypeScript)
│   ├── src/
│   │   ├── features/                   # Feature slices (auth, donation, inventory, clinical)
│   │   ├── components/                 # UI components dùng chung (Layouts, Barcode Reader)
│   │   ├── services/                   # Axios client, Interceptors, Base API queries
│   │   └── types/                      # TypeScript definitions & API contracts
│   ├── package.json
│   └── vite.config.ts
├── docker-compose.yml                  # Cấu hình container hóa PostgreSQL Database
├── HuyetMach175.sln                    # Visual Studio / .NET Solution
└── README.md
```

---

## 3. Khởi chạy Database bằng Docker Compose

Hệ thống cung cấp sẵn container PostgreSQL chạy ngầm và tự động nạp toàn bộ cấu trúc bảng từ script `init-schema.sql` ngay trong lần chạy đầu tiên[cite: 4, 6].

### Yêu cầu tiên quyết
- Cài đặt **Docker Desktop** và đảm bảo Docker engine đang hoạt động[cite: 6].
- **Lưu ý xung đột cổng:** Nếu máy bạn đã cài sẵn dịch vụ PostgreSQL (cổng 5432 trên Windows), hãy dừng dịch vụ này trước khi chạy Docker (`Stop-Service postgresql*` hoặc dừng trong tab *Services* của Task Manager)[cite: 2, 6].

### Lệnh khởi chạy
Tại thư mục gốc `huyet-mach-175`:

```bash
# Khởi chạy PostgreSQL container dưới chế độ nền
docker compose up -d

# Kiểm tra trạng thái hoạt động của container
docker ps
```

### Thông số kết nối mặc định (Local Development)
- **Host:** `localhost`
- **Port:** `5432`[cite: 6]
- **Database Name:** `huyetmach175_db`
- **Username:** `postgres`
- **Password:** `admin123`

---

## 4. Hướng dẫn khởi chạy Backend (.NET)

### Yêu cầu tiên quyết
- [.NET SDK](https://dotnet.microsoft.com/download) phiên bản 8.0, 9.0 hoặc 10.0 trở lên.

### Các bước chạy Web API
```bash
# Di chuyển vào thư mục Web API
cd src/HuyetMach175.Api

# Khôi phục dependencies và chạy API
dotnet run
```

- API sẽ lắng nghe tại cổng HTTPS/HTTP mặc định (thường là `https://localhost:7xxx` hoặc `http://localhost:5xxx`).
- Môi trường phát triển đã cấu hình sẵn OpenAPI/Swagger UI và kích hoạt chính sách CORS cho phép client Vite kết nối.

---

## 5. Hướng dẫn khởi chạy Frontend (React + AntD)

### Yêu cầu tiên quyết
- [Node.js](https://nodejs.org/) bản 18+ (hoặc 20+ LTS).

### Các bước chạy Client
```bash
# Di chuyển vào thư mục client
cd client

# Cài đặt toàn bộ thư viện phụ thuộc
npm install

# Khởi chạy server phát triển
npm run dev
```

- Truy cập ứng dụng tại đường dẫn: `http://localhost:5173`[cite: 5].

---

## 6. Các quy tắc kỹ thuật cốt lõi (Important Architectural Notes)

- **Chống cấp phát trùng (No Double-Booking):** CSDL áp dụng chỉ mục duy nhất có điều kiện `uq_active_allocation_per_bag` (`WHERE status IN ('RESERVED', 'ISSUED')`), đảm bảo một túi máu không bao giờ bị gán đồng thời cho hai y lệnh cấp phát khác nhau[cite: 4].
- **Bảo toàn dữ liệu y tế (Medical Immutability):** Áp dụng quy tắc `ON DELETE RESTRICT` cho hồ sơ người hiến, kết quả xét nghiệm và túi máu[cite: 1, 4]. Bảng `audit_logs` tuân thủ nguyên tắc Append-Only, ghi lại toàn bộ trạng thái cũ/mới dạng `JSONB` mà không cho phép sửa/xóa[cite: 4].
- **Định tuyến nghiệp vụ (No Slug Policy):** Ứng dụng là cổng thông tin nội viện khép kín, không phục vụ mục đích SEO. Toàn bộ định tuyến (Routing) sử dụng định danh kỹ thuật (`bag_id`, `session_id`) hoặc mã nghiệp vụ duy nhất (Mã vạch chuẩn ISBT 128 `barcode`, Mã phiếu yêu cầu `request_code`)[cite: 4].