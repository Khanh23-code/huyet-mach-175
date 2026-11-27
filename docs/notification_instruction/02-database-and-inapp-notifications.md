# Phần 2: Thiết kế Database, Ingestion Service & REST API (In-App + Email)

Tài liệu này mô tả thiết kế dữ liệu tối giản nhất để hỗ trợ **In-App Notification** và **hàng đợi gửi Email SMTP** (đã loại bỏ hoàn toàn bảng `device_registration` và kênh FCM).

---

## 1. Thiết kế Database (PostgreSQL / EF Core)

Bạn chỉ cần **2 bảng chính** (và thêm bảng thứ 3 nếu dự án theo kiến trúc Modular Monolith cần Outbox riêng cho từng module):

### 1.1. Bảng `notification` (Hộp thư chính cho UI quả chuông)

| Cột | Kiểu dữ liệu | Ghi chú |
| --- | --- | --- |
| `id` | `uuid` PK | Khóa chính |
| `recipient_id` | `uuid` NOT NULL | ID người nhận |
| `title` | `varchar(200)` NOT NULL | Tiêu đề ngắn |
| `content` | `text` NOT NULL | Nội dung ngắn gọn (~15–20 từ) |
| `notification_type` | `varchar(100)` NOT NULL | Mã loại sự kiện (VD: `SUBMISSION_CONFIRMED`, `ORDER_PAID`) |
| `is_read` | `boolean` NOT NULL DEFAULT `false` | Trạng thái đã đọc |
| `created_at` | `timestamptz` NOT NULL | Thời điểm tạo (UTC) |
| `read_at` | `timestamptz` NULL | Thời điểm đánh dấu đã đọc |
| `source_module` | `varchar(64)` NOT NULL | Tên module/service phát sinh |
| `source_event_id` | `uuid` NULL | ID sự kiện nguồn để truy vết |
| `deduplication_key` | `varchar(200)` NULL | Khóa chống trùng lặp (VD: `REMINDER:ORDER_123:24h`) |
| `resource_type` | `varchar(64)` NULL | Loại đối tượng (`Order`, `Ticket`, `ThesisGroup`...) |
| `resource_id` | `uuid` NULL | ID đối tượng liên quan |
| `action_path` | `varchar(500)` NULL | Route nội bộ Frontend khi user click vào thông báo |
| `priority` | `varchar(20)` NOT NULL DEFAULT `'Normal'` | `'Normal'` hoặc `'Critical'` |
| `expires_at` | `timestamptz` NULL | Hết hạn hiển thị hành động (nếu có) |

**3 Index bắt buộc trên `notification`:**
1. `IX (recipient_id, created_at DESC, id DESC)`: Phân trang hộp thư ổn định, không bị nhảy dòng khi có thông báo mới chèn vào.
2. Partial `IX (recipient_id, created_at DESC) WHERE is_read = false`: Đếm số chưa đọc (`unread-count`) và lọc tab "Chưa đọc" cực nhanh.
3. Partial `UNIQUE (recipient_id, deduplication_key) WHERE deduplication_key IS NOT NULL`: Chốt chặn cuối cùng ở DB chống tạo trùng thông báo.

### 1.2. Bảng `notification_delivery` (Hàng đợi gửi Email + theo dõi kết quả)

| Cột | Kiểu dữ liệu | Ghi chú |
| --- | --- | --- |
| `id` | `uuid` PK | Khóa chính |
| `notification_id` | `uuid` FK → `notification.id` | Liên kết thông báo cha |
| `channel` | `varchar(20)` NOT NULL | Mặc định `'Email'` (dự phòng mở rộng kênh khác về sau) |
| `destination` | `varchar(256)` NOT NULL | Địa chỉ email người nhận |
| `status` | `varchar(30)` NOT NULL | `Pending`, `Processing`, `Accepted`, `RetryScheduled`, `FailedPermanent`, `Skipped` |
| `attempt_count` | `int` NOT NULL DEFAULT `0` | Số lần đã thử gửi |
| `next_attempt_at` | `timestamptz` NOT NULL | Thời điểm được phép xử lý tiếp (hoặc hết hạn lease nếu đang `Processing`) |
| `last_error_code` | `varchar(100)` NULL | Mã lỗi gần nhất |
| `last_error_message` | `text` NULL | Chi tiết lỗi (phục vụ debug khi mail không tới) |
| `payload_json` | `jsonb` NULL | Chứa `subject`, `recipientFullName`, `categoryLabel`, `ctaLabel`, `notice`, `metadata[]`, `digestSections[]` |
| `created_at` | `timestamptz` NOT NULL | Thời điểm tạo |
| `sent_at` | `timestamptz` NULL | Thời điểm SMTP chấp nhận gửi |

**2 Index bắt buộc trên `notification_delivery`:**
1. `UNIQUE (notification_id, channel, destination)`: Chống xếp trùng 2 email giống nhau cho cùng 1 thông báo.
2. Partial `IX (next_attempt_at) WHERE status IN ('Pending', 'RetryScheduled', 'Processing')`: Giúp Worker quét hàng đợi chỉ đọc phần dữ liệu đang chờ.

### 1.3. Bảng `notification_event_outbox` (Tùy chọn cho dự án Modular Monolith)

Nếu dự án mới của bạn chia nhiều module/DbContext độc lập (hoặc muốn tách biệt nghiệp vụ và bảng `notification`), tạo thêm bảng `notification_event_outbox` ở module phát sinh sự kiện:
- Cột: `id`, `event_id` (UNIQUE), `event_type`, `payload_version`, `payload_json` (`jsonb`), `created_at`, `processed_at` (NULL), `attempt_count`, `next_attempt_at`, `last_error_code`.
- Partial Index: `IX (next_attempt_at) WHERE processed_at IS NULL`.

*(Nếu dự án mới là Monolith đơn giản dùng chung 1 `DbContext`, bạn có thể bỏ qua bảng này và gọi thẳng `INotificationIngestionService` hoặc dùng `DbContext` chung trong cùng transaction.)*

---

## 2. `INotificationIngestionService` — Điểm đầu vào duy nhất

Mọi thông báo (từ nghiệp vụ hay từ cron nhắc hạn) đều đi qua 1 service duy nhất `INotificationIngestionService`:

```csharp
public sealed record NotificationIngestionRequest(
    Guid RecipientId,
    string Title,
    string Content,
    string NotificationType,
    string SourceModule = "Notifications",
    Guid? SourceEventId = null,
    string? DeduplicationKey = null,
    string? ResourceType = null,
    Guid? ResourceId = null,
    string? ActionPath = null,
    string Priority = "Normal",
    DateTime? ExpiresAtUtc = null,
    bool EnqueueEmail = false,
    string? RecipientEmail = null,
    string? EmailSubject = null,
    string? RecipientFullName = null,
    string? EmailCategoryLabel = null,
    string? EmailCtaLabel = null,
    string? EmailNotice = null,
    IReadOnlyList<NotificationEmailMetaItem>? EmailMetadata = null,
    IReadOnlyList<NotificationEmailDigestSection>? EmailDigestSections = null);
```

**Luồng xử lý bên trong `IngestAsync`:**
1. Nếu `DeduplicationKey != null`: kiểm tra nhanh `db.Notifications.AnyAsync(x => x.RecipientId == req.RecipientId && x.DeduplicationKey == dedupKey)`. Nếu đã có → trả về `Deduplicated = true` ngay.
2. Tạo bản ghi `Notification` (`IsRead = false`, `CreatedAt = nowUtc`).
3. Nếu `req.EnqueueEmail == true` và `!string.IsNullOrWhiteSpace(req.RecipientEmail)`:
   - Serialize thông tin render email (`subject`, `recipientFullName`, `categoryLabel`, `actionPath`, `ctaLabel`, `notice`, `metadata`, `digestSections`) thành `PayloadJson`.
   - Thêm 1 bản ghi `NotificationDelivery` với `Channel = "Email"`, `Status = "Pending"`, `NextAttemptAt = nowUtc`.
4. Gọi `SaveChangesAsync()`. Bắt thêm `PostgresException` mã `23505` (Unique Violation trên `deduplication_key`) để an toàn tuyệt đối khi 2 request chạy song song cùng 1ms.

---

## 3. Bộ 4 REST API cho In-App Notification

Tất cả endpoint đều yêu cầu xác thực (`RequireAuthorization`) và **luôn lọc theo `currentUser.UserId`** (không bao giờ tin `userId` từ query/body):

| Method & Route | Chức năng | Query / Response chính |
| --- | --- | --- |
| `GET /api/notifications` | Lấy danh sách thông báo của user | Query: `unreadOnly` (`bool`), `pageNumber`, `pageSize` (giới hạn 1–50). Trả danh sách sắp xếp `CreatedAt DESC, Id DESC` + `TotalCount`. |
| `GET /api/notifications/unread-count` | Đếm số thông báo chưa đọc | Trả `{ count: int }` (dùng cho badge đỏ trên quả chuông). |
| `PATCH /api/notifications/{id}/read` | Đánh dấu 1 thông báo đã đọc | Chỉ update nếu `n.Id == id && n.RecipientId == currentUserId`. Set `IsRead = true`, `ReadAt ??= nowUtc`. |
| `PATCH /api/notifications/read-all` | Đánh dấu tất cả là đã đọc | Dùng `ExecuteUpdateAsync` hàng loạt cho `RecipientId == currentUserId && !IsRead`. |
