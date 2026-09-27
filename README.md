# Huyết Mạch 175 - Blood Bank Management System

Hệ thống quản trị ngân hàng máu bệnh viện chuẩn hoá luồng tiếp nhận, điều chế, lưu trữ và cấp phát lâm sàng.

## Kiến trúc hệ thống
- **Backend:** .NET 8/9 LTS (Modular Monolith + DDD + Clean Architecture)
- **Frontend:** React + TypeScript + Ant Design
- **Database:** PostgreSQL 16+

## Hướng dẫn khởi chạy nhanh môi trường Database (Docker)
```bash
docker compose up -d
```
Database sẽ chạy ở cổng 5432, DB Name: huyetmach175_db, User: postgres, Password: admin123