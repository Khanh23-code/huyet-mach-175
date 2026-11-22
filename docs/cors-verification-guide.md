# Hướng dẫn kiểm tra cấu hình CORS (Local Production & Render)

> **[AGENT INSTRUCTION — BẮT BUỘC KIỂM TRA]**
> Khi Agent đọc tài liệu này, hoặc khi có bất kỳ thay đổi nào liên quan đến CORS, Auth/Cookie/CSRF trong `src/HuyetMach175.Api/Program.cs`, `src/HuyetMach175.Api/appsettings*.json`, hay cấu hình môi trường deploy, **Agent bắt buộc phải chạy hoặc hướng dẫn kiểm tra đầy đủ các bước trong tài liệu này** (cả case **CHO PHÉP** origin hợp lệ lẫn case **CHẶN** origin giả mạo như `https://evil.com` và `https://localhost.evil.com`). Không được chỉ chạy FE + BE local ở chế độ `Development` rồi kết luận đã đạt.

## 0. Khi nào cần chạy bài kiểm tra này?
- **Ngay sau khi deploy** bản cập nhật CORS mới lên Render.
- **Mỗi khi sửa cấu hình CORS / Auth**, bao gồm:
  - Sửa `src/HuyetMach175.Api/Program.cs` (chính sách `AllowReactApp`, middleware `UseCors`, bật/tắt `AllowCredentials`).
  - Thêm/bớt domain trong `Cors:AllowedOrigins` hoặc đổi `Cors:AllowLocalhostOrigins` ở `appsettings.json` / Environment Variables trên Render.
- **Trước khi đưa hệ thống vào vận hành thực tế tại bệnh viện** (đặc biệt sau khi đặt `Cors__AllowLocalhostOrigins=false` và gán domain bệnh viện chính thức).
- **Khi nâng cấp Auth sang `HttpOnly Cookie` + `CSRF Token` sau MVP** (bắt buộc test lại vì khi đó sẽ bật lại `AllowCredentials()`).

---

## 1. Cấu hình CORS hiện tại
- File cấu hình: `src/HuyetMach175.Api/appsettings.json`
  - `Cors:AllowedOrigins`: `["https://huyet-mach-175.pages.dev"]`
  - `Cors:AllowLocalhostOrigins`: `true` *(tạm bật để FE chạy `localhost` gọi được Render trong giai đoạn dev)*
- Middleware xử lý: `src/HuyetMach175.Api/Program.cs` (`AllowReactApp`)
  - Chỉ cho phép đúng các origin nằm trong `Cors:AllowedOrigins` (chuẩn hóa bỏ dấu `/` cuối) hoặc `http(s)://localhost:*` / `127.0.0.1:*` khi `Environment.IsDevelopment()` hoặc `Cors:AllowLocalhostOrigins == true`.
  - Không bật `AllowCredentials()` vì hệ thống đang dùng `Authorization: Bearer <JWT>`, chưa dùng Cookie.

> **Vì sao chạy BE + FE local bình thường là chưa đủ?**
> - Chạy local mặc định qua `src/HuyetMach175.Api/Properties/launchSettings.json` luôn đặt `ASPNETCORE_ENVIRONMENT=Development`, khác với `Production` trên Render.
> - FE local chạy được chỉ chứng minh *"localhost được phép"*, chưa chứng minh được **domain thật (`https://huyet-mach-175.pages.dev`) hoạt động đúng** và **domain lạ (`https://evil.com`) bị chặn**.

---

## 2. Bước 1: Test nhanh trên local ở đúng chế độ `Production`

### 2.1. Khởi chạy BE bỏ qua `launchSettings.json`
Mở terminal tại thư mục gốc repository và chạy:

```powershell
dotnet run --project src/HuyetMach175.Api/HuyetMach175.Api.csproj --no-launch-profile --urls http://localhost:5208
```

*(Ở chế độ `--no-launch-profile`, `ASPNETCORE_ENVIRONMENT` mặc định là `Production` và đọc `appsettings.json` tương tự Render. Vì `appsettings.json` dùng placeholder `YOUR_NEON_HOST`, startup sẽ log lỗi kết nối DB trong `try/catch` rồi vẫn mở cổng `5208` để test CORS preflight `OPTIONS`.)*

### 2.2. Chạy các lệnh kiểm tra CORS Preflight (`curl.exe`)
Mở một cửa sổ PowerShell khác và chạy lần lượt các case sau:

#### Case 1 – Cho phép FE Cloudflare Pages (Kỳ vọng: **CHO PHÉP**)
```powershell
curl.exe -i -X OPTIONS http://localhost:5208/api/auth/login -H "Origin: https://huyet-mach-175.pages.dev" -H "Access-Control-Request-Method: POST"
```
- **Đạt khi:** Response header **CÓ** `Access-Control-Allow-Origin: https://huyet-mach-175.pages.dev` và **KHÔNG CÓ** `Access-Control-Allow-Credentials: true`.

#### Case 2 – Cho phép FE chạy local (Kỳ vọng: **CHO PHÉP** trong giai đoạn dev)
```powershell
curl.exe -i -X OPTIONS http://localhost:5208/api/auth/login -H "Origin: http://localhost:5173" -H "Access-Control-Request-Method: POST"
```
- **Đạt khi:** Response header **CÓ** `Access-Control-Allow-Origin: http://localhost:5173`.

#### Case 3 – Chặn domain lạ & domain giả mạo `localhost` (Kỳ vọng: **CHẶN**)
```powershell
curl.exe -i -X OPTIONS http://localhost:5208/api/auth/login -H "Origin: https://evil.com" -H "Access-Control-Request-Method: POST"
curl.exe -i -X OPTIONS http://localhost:5208/api/auth/login -H "Origin: https://localhost.evil.com" -H "Access-Control-Request-Method: POST"
```
- **Đạt khi:** Response header **KHÔNG CÓ** dòng `Access-Control-Allow-Origin`.

---

## 3. Bước 2: Test thực tế sau khi deploy lên Render

Sau khi push code và Render hoàn tất deploy:

1. **Kiểm tra từ FE đã deploy (`https://huyet-mach-175.pages.dev/`):**
   - Đăng nhập và thao tác một vài API trên trình duyệt.
   - Mở **DevTools (`F12`) → tab Network** → kiểm tra response header từ Render có đúng `access-control-allow-origin: https://huyet-mach-175.pages.dev`.
2. **Kiểm tra từ FE local (`http://localhost:5173` hoặc cổng dev đang dùng):**
   - Trỏ `VITE_API_BASE_URL` sang URL Render → xác nhận vẫn gọi API bình thường, không dính lỗi CORS.
3. **Kiểm tra chặn domain lạ trực tiếp trên Render:**
   ```powershell
   curl.exe -i -X OPTIONS https://<link-render-cua-ban>/api/auth/login -H "Origin: https://evil.com" -H "Access-Control-Request-Method: POST"
   ```
   - **Đạt khi:** Không trả về header `Access-Control-Allow-Origin`.

---

## 4. Lưu ý khi triển khai Production tại bệnh viện

Trong **Render Dashboard → Web Service → Environment → Environment Variables**, cấu hình:
- `ASPNETCORE_ENVIRONMENT` = `Production`
- `Cors__AllowLocalhostOrigins` = `false` *(tắt cho phép `localhost` khi vận hành chính thức)*
- `Cors__AllowedOrigins__0` = `https://<domain-chinh-thuc-cua-benh-vien>` *(không có dấu `/` ở cuối)*
