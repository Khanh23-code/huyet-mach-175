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

**Huyết Mạch 175** là giải pháp phần mềm quản trị toàn diện chu trình lưu thông máu tại Bệnh viện Quân y 175, bao gồm các phân hệ: tiếp nhận người hiến máu, điều chế - lưu trữ chế phẩm, xét nghiệm sàng lọc túi máu và tiếp nhận y lệnh cấp phát lâm sàng khẩn cấp/thường quy.

Hệ thống được tổ chức theo mô hình **Monorepo** với kiến trúc **Modular Monolith** kết hợp nguyên lý **Domain-Driven Design (DDD)** và **Clean Architecture**:

- **Backend:** .NET Web API, Entity Framework Core (PostgreSQL Provider), MediatR (CQRS), FluentValidation, SignalR, JWT Authentication.
- **Frontend:** React, TypeScript, Vite, Ant Design (AntD), TanStack Query, Zustand, React Hook Form, Zod.
- **Database & Hạ tầng:** PostgreSQL 16+ (Dockerized container) tận dụng cấu trúc JSONB và Partial Unique Indexes để bảo toàn dữ liệu.

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

Hệ thống cung cấp sẵn container PostgreSQL chạy ngầm và tự động nạp toàn bộ cấu trúc bảng từ script `init-schema.sql` ngay trong lần chạy đầu tiên.

### Yêu cầu tiên quyết

- Cài đặt **Docker Desktop** và đảm bảo Docker engine đang hoạt động.
- **Lưu ý xung đột cổng:** Nếu máy bạn đã cài sẵn dịch vụ PostgreSQL (cổng 5432 trên Windows), hãy dừng dịch vụ này trước khi chạy Docker (`Stop-Service postgresql*` hoặc dừng trong tab _Services_ của Task Manager).

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
- **Port:** `5432`
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

- API sẽ lắng nghe tại cổng HTTPS/HTTP mặc định (thường là `https://localhost:7059` hoặc `http://localhost:5208`).
- Môi trường phát triển đã cấu hình sẵn giao diện tài liệu **Scalar API Reference** và OpenAPI, đồng thời kích hoạt chính sách CORS cho phép client Vite kết nối.

### Tài liệu & Giao diện Test API (Scalar API Reference)

Hệ thống sử dụng **Scalar API Reference** để tương tác và kiểm thử API một cách trực quan, hiện đại thay cho Swagger mặc định.

- **Đường dẫn truy cập:**
  - HTTP: [http://localhost:5208/scalar/v1](http://localhost:5208/scalar/v1)
  - HTTPS: [https://localhost:7059/scalar/v1](https://localhost:7059/scalar/v1)
- **Hướng dẫn xác thực Bearer Token trên Scalar:**
  1. Đăng nhập qua API **`POST /api/Auth/login`** với tài khoản test (ví dụ: username `admin`, password `admin123`) để lấy `accessToken`.
  2. Ở danh sách API cột bên trái, click chọn một API cần xác thực (ví dụ: **`GET /api/users`** hoặc **`GET /api/Auth/me`**).
  3. Ở khung xem chi tiết bên phải, bấm nút **`▶ Test Request`** để mở cửa sổ API Client popup.
  4. Trong phần **Headers**, thêm một dòng mới:
     * **Key:** `Authorization`
     * **Value:** `Bearer <dán_chuỗi_access_token_vào_đây>`
  5. Bấm nút **Send** (ở góc trên cùng popup) để gửi request.

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

- Truy cập ứng dụng tại đường dẫn: `http://localhost:5173`.

---

## 6. Quy chuẩn cộng tác & Kiểm soát nhánh (Git Workflow & Rulesets)

Dự án áp dụng chặt chẽ quy chuẩn **Git Branch Protection Rulesets** nhằm bảo đảm tính ổn định tuyệt đối cho các nhánh trọng yếu (`main`, `dev`):

- **Cấm đẩy mã nguồn trực tiếp (No Direct Push):** Thành viên không được phép sử dụng `git push` thẳng vào nhánh `dev` hoặc `main`. Mọi sự thay đổi bắt buộc phải tách nhánh tính năng/sửa lỗi (`feat/*`, `fix/*`) và đưa vào qua **Pull Request (PR)**.
- **Quy tắc duyệt bắt buộc (Mandatory Code Review):** Mỗi Pull Request gửi vào `dev` hoặc `main` phải có tối thiểu **1 chấp thuận (Approve)** từ trưởng nhóm (Git Lead/Reviewer) mới đủ điều kiện sáp nhập mã nguồn.
- **Tự động hủy phê duyệt khi cập nhật (Dismiss Stale Approvals):** Khi có bất kỳ commit mới nào được đẩy thêm vào PR đang chờ duyệt, các lượt approve trước đó sẽ tự động bị vô hiệu hóa để phục vụ việc đánh giá lại từ đầu.
- **Chặn ghi đè lịch sử (Block Force Pushes) & Xóa nhánh:** Vô hiệu hóa hoàn toàn cờ `--force` (`git push -f`) và quyền xóa đối với các nhánh được bảo vệ nhằm ngăn chặn nguy cơ mất mát lịch sử commit.

---

## 7. Tự động hóa tích hợp liên tục (CI/CD Pipeline)

Dự án tích hợp hệ thống kiểm thử và đóng gói tự động hóa thông qua **GitHub Actions** (`.github/workflows/ci.yml`). Pipeline sẽ được kích hoạt tự động mỗi khi có sự kiện `push` hoặc tạo `pull_request` nhắm vào các nhánh `dev` và `main`.

### Luồng kiểm tra chất lượng (Verification Jobs)

1. **Kiểm tra Backend (`Build & Check Backend (.NET)`):**
   - Tự động khởi tạo máy ảo Ubuntu, nạp bộ công cụ .NET SDK tương ứng.
   - Chạy lệnh `dotnet restore` và biên dịch toàn bộ cấu trúc dự án `HuyetMach175.slnx` ở cấu hình `Release`.
   - Phát hiện sớm mọi lỗi sai cú pháp, xung đột thư viện hay lỗi phụ thuộc tầng (Layer Dependency) giữa 5 Bounded Contexts.
2. **Kiểm tra Frontend (`Build & Check Frontend (React Vite)`):**
   - Khởi tạo môi trường Node.js 20 LTS và tối ưu bộ nhớ đệm phụ thuộc qua tệp khóa `package-lock.json`.
   - Cài đặt môi trường sạch bằng `npm ci`.
   - Chạy tiến trình đóng gói kiểm thử `npm run build` để xác thực toàn bộ tính an toàn kiểu dữ liệu (TypeScript Type Checking) và tính hợp lệ của cây giao diện Ant Design.

### Cơ chế khóa gộp tự động (Required Status Checks)

- Cả 2 job kiểm tra trên được đặt làm điều kiện tiên quyết (**Status Checks Must Pass**).
- Nếu có bất kỳ lỗi biên dịch nào ở phía Frontend hoặc Backend, GitHub Actions sẽ đánh dấu trạng thái thất bại (`Failure`) và **tự động vô hiệu hóa nút Merge Pull Request**, ngăn chặn toàn bộ mã nguồn lỗi lọt vào nhánh phát triển chung.
