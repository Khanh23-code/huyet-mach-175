# Phần 1: Thiết lập Tài khoản Mail & Xây dựng EmailService (SMTP)

Tài liệu này hướng dẫn từ bước chưa có tài khoản gửi mail cho tới khi có một `SmtpEmailSender` hoàn chỉnh hỗ trợ gửi đơn lẻ (`SendAsync`) và gửi theo lô tái sử dụng kết nối (`SendBatchAsync`).

---

## 1. Chuẩn bị tài khoản & thông số SMTP (Khi chưa có gì)

### 1.1. Môi trường Dev / Demo / Đồ án: Dùng Gmail + App Password (Miễn phí)

Gmail cá nhân **không cho phép** đăng nhập SMTP bằng mật khẩu thường. Bạn bắt buộc phải tạo **App Password (Mật khẩu ứng dụng)**:

1. Tạo một Gmail riêng cho hệ thống (hoặc dùng Gmail hiện có, ví dụ `myproject.noreply@gmail.com`).
2. Vào [Google Account Security](https://myaccount.google.com/security):
   - Bật **Xác minh 2 bước (2-Step Verification)** — bắt buộc phải bật mới thấy mục App Passwords.
3. Tìm kiếm từ khóa **App passwords** (hoặc truy cập trực tiếp `https://myaccount.google.com/apppasswords`).
4. Đặt tên ứng dụng (ví dụ `MyProject SMTP`) → bấm **Create**.
5. Google sẽ hiển thị chuỗi **16 ký tự** (dạng `abcd efgh ijkl mnop`). Khi đưa vào cấu hình hãy **xóa các dấu cách** thành `abcdefghijklmnop`.

**Thông số SMTP của Gmail:**
- **Host:** `smtp.gmail.com`
- **Port:** `587` (dùng `StartTls`) hoặc `465` (dùng `SslOnConnect`)
- **Username:** địa chỉ Gmail đầy đủ (`myproject.noreply@gmail.com`)
- **Password:** chuỗi App Password 16 ký tự
- **Giới hạn miễn phí:** ~500 email/ngày với Gmail cá nhân (`@gmail.com`), đủ cho dev, test và hệ thống quy mô vừa/nhỏ.

> **Mẹo khi chỉ muốn test giao diện & nội dung mail mà không muốn spam hộp thư thật:** Có thể dùng **Mailpit** (chạy Docker `axllent/mailpit` cổng SMTP `1025`, Web UI `8025`) hoặc **Mailtrap Sandbox**. Khi dùng Mailpit local, đặt `Security = None`, `Host = localhost`, `Port = 1025`.

### 1.2. Môi trường Production (Khuyến nghị khi vận hành thật)

Khi hệ thống gửi nhiều email hoặc cần độ uy tín tên miền (`noreply@yourdomain.com`):
- Dùng các nhà cung cấp transactional email có gói miễn phí/rẻ hỗ trợ SMTP Relay: **Brevo** (300 mail/ngày free), **Resend**, **Amazon SES**, hoặc **Postmark**.
- Khi chuyển sang dịch vụ này, bạn **không cần sửa code** — chỉ cần thay `Host`, `Port`, `Username`, `Password`, `FromAddress` trong biến môi trường.

---

## 2. Cấu hình an toàn (Không bao giờ commit mật khẩu lên Git)

### 2.1. Thêm cấu trúc mặc định vào `appsettings.json` (chỉ để giá trị không nhạy cảm)

```json
{
  "Notifications": {
    "Smtp": {
      "Host": "",
      "Port": 587,
      "FromAddress": "",
      "FromName": "MyProject System",
      "Username": "",
      "Password": "",
      "Security": "StartTls",
      "TimeoutSeconds": 30
    },
    "Delivery": {
      "Enabled": true,
      "PublicWebBaseUrl": "http://localhost:5173",
      "PollIntervalSeconds": 5,
      "BatchSize": 25,
      "MaxAttempts": 5,
      "StaleProcessingMinutes": 10
    }
  }
}
```

### 2.2. Truyền thông tin nhạy cảm qua `.env` hoặc User Secrets ở local

Trong .NET, dấu `:` trong `appsettings.json` tương ứng với `__` (hai dấu gạch dưới) trong biến môi trường:

```env
Notifications__Smtp__Host=smtp.gmail.com
Notifications__Smtp__Port=587
Notifications__Smtp__Security=StartTls
Notifications__Smtp__FromAddress=myproject.noreply@gmail.com
Notifications__Smtp__FromName=MyProject Notifications
Notifications__Smtp__Username=myproject.noreply@gmail.com
Notifications__Smtp__Password=abcdefghijklmnop
```

> **Quan trọng:** Nhớ thêm `.env` và `appsettings.*.local.json` vào `.gitignore`. Trên server thật (Docker, Render, Azure, VPS...), nạp các biến này qua Secret Manager / Environment Variables.

---

## 3. Cài đặt gói NuGet & Thiết kế `SmtpEmailSender`

### 3.1. Cài đặt `MailKit`

Không dùng `System.Net.Mail.SmtpClient` cũ của .NET (đã bị Microsoft khuyến cáo lỗi thời). Hãy dùng **MailKit**:

```bash
dotnet add package MailKit
```

### 3.2. Contract `IEmailSender` & `EmailMessage`

Tạo ở tầng Contracts/Application để mọi module đều có thể gọi mà không phụ thuộc `MailKit`:

```csharp
public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string TextBody);

public sealed record EmailBatchItem(
    Guid Id,
    EmailMessage Message);

public sealed record EmailBatchItemResult(
    Guid Id,
    bool Succeeded,
    bool IsPermanentFailure = false,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailBatchItemResult>> SendBatchAsync(
        IReadOnlyCollection<EmailBatchItem> items,
        CancellationToken cancellationToken = default);
}
```

### 3.3. Cấu hình `SmtpOptions` & Validation khi khởi động

```csharp
public enum SmtpSecurityMode
{
    None,
    StartTls,
    SslOnConnect
}

public sealed record SmtpOptions
{
    public const string SectionName = "Notifications:Smtp";

    public required string Host { get; init; }
    public int Port { get; init; } = 587;
    public required string FromAddress { get; init; }
    public required string FromName { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public SmtpSecurityMode Security { get; init; } = SmtpSecurityMode.StartTls;
    public int TimeoutSeconds { get; init; } = 30;
}
```

**Lưu ý khi validate `SmtpOptions`:**
- Chỉ cho phép `SmtpSecurityMode.None` ở môi trường `Development` (khi test Mailpit/Mailtrap local). Trên `Production`, bắt buộc `StartTls` (port `587`) hoặc `SslOnConnect` (port `465`).
- Nếu có `Username` thì bắt buộc phải có `Password` (và ngược lại).

### 3.4. Logic cốt lõi của `SmtpEmailSender.SendBatchAsync`

Đây là điểm tối ưu hiệu năng quan trọng nhất trong UGTM:

1. **Mở 1 kết nối `SmtpClient` duy nhất cho cả lô** (`ConnectAsync` → `AuthenticateAsync`).
2. **Vòng lặp từng `EmailBatchItem`:**
   - Dựng `MimeMessage` riêng cho đúng 1 người nhận (`To`), đính kèm cả `HtmlBody` và `TextBody` qua `BodyBuilder`.
   - Kiểm tra `if (!transport.IsConnected)` thì tự kết nối & xác thực lại (phòng trường hợp server SMTP ngắt kết nối giữa chừng).
   - Bắt lỗi riêng cho **từng thư**:
     - Lỗi vĩnh viễn (`FormatException` sai email, `SmtpCommandException` mã `5xx` như `550 User unknown`) → đánh dấu `IsPermanentFailure = true` để Worker không retry vô ích.
     - Lỗi tạm thời (`4xx`, timeout, rớt mạng) → `IsPermanentFailure = false` để Worker lên lịch retry. Nếu lỗi giao thức/kết nối, đóng kết nối hiện tại để thư kế tiếp tự mở kết nối sạch.
3. **Ngắt kết nối sạch sẽ** trong khối `finally`: `await transport.DisconnectAsync(quit: true, ...)` (bỏ qua lỗi lúc disconnect nếu socket đã đóng).

---

## 4. Thiết kế Email Template chuẩn & an toàn XSS

Trong UGTM, `NotificationEmailTemplateRenderer` dùng chung một khung HTML tương thích hầu hết email client (Gmail Web, Gmail Mobile, Outlook):

- **Dùng Layout bảng (`<table>`) + Inline CSS:** Email client không hỗ trợ Tailwind hay Flexbox/Grid ổn định.
- **2 biến thể bố cục:**
  1. **Standard Layout:** Header màu thương hiệu + Badge danh mục + Lời chào cá nhân hóa (`Kính gửi {FullName}`) + Nội dung ngắn gọn (~15–20 từ) + Bảng Metadata (2 cột: Nhãn / Giá trị, ví dụ Tên đề tài, Phiên bản, Tên tệp + dung lượng, Thời gian) + Hộp Notice lưu ý + Nút CTA dẫn thẳng về Frontend (`PublicWebBaseUrl + ActionPath`).
  2. **Digest Layout (Tổng hợp 24h):** Gom nhiều đầu việc của cùng 1 nhân sự vào 1 thư duy nhất chia theo từng `DigestSection` (tiêu đề nhóm việc, số lượng, danh sách bullet tối đa ~10 dòng, nút đi thẳng trang xử lý).

### Lưu ý cực kỳ quan trọng về Tiếng Việt UTF-8 và XSS

> **Cạm bẫy của `WebUtility.HtmlEncode`:** Trong .NET, `WebUtility.HtmlEncode("Đề tài")` sẽ biến các ký tự tiếng Việt có dấu thành mã số `&#272;&#7873; t&#224;i` — gây khó đọc trong source HTML và có thể làm sai lệch các bộ test kiểm tra chuỗi tiếng Việt.

Hãy viết hàm escape riêng chỉ mã hóa 5 ký tự nguy hiểm của HTML, giữ nguyên UTF-8 tiếng Việt sạch sẽ:

```csharp
private static string Html(string value)
    => string.IsNullOrEmpty(value)
        ? string.Empty
        : value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);
```

Đồng thời **luôn chuẩn hóa URL nút CTA**: chỉ nối `ActionPath` dạng relative (`/thesis/proposal`) vào `PublicWebBaseUrl` đã cấu hình sẵn, hoặc kiểm tra `Uri.Scheme is "http" or "https"` để chặn `javascript:` injection.
