# Phần 3: Background Workers, Frontend NotificationBell & Checklist Triển khai

Tài liệu này hướng dẫn cách vận hành 2 Worker nền trên Backend, tích hợp UI quả chuông trên Frontend React và kiểm thử toàn luồng End-to-End.

---

## 1. `NotificationDeliveryWorker` (Gửi Email nền & Retry thông minh)

Worker kế thừa `BackgroundService`, chạy theo chu kỳ `PeriodicTimer` (mặc định mỗi **5 giây**):

### 1.1. Claim lô an toàn bằng `FOR UPDATE SKIP LOCKED`

Trong `EfNotificationDeliveryQueueStore.ClaimBatchAsync`:

```sql
SELECT d.*
FROM notifications.notification_delivery d
WHERE (d.status IN ('Pending', 'RetryScheduled') AND d.next_attempt_at <= {nowUtc})
   OR (d.status = 'Processing' AND d.next_attempt_at <= {staleCutoffUtc})
ORDER BY d.next_attempt_at, d.id
LIMIT {batchSize}
FOR UPDATE SKIP LOCKED
```

- Khi lấy được tối đa `batchSize` (25) dòng, đổi ngay `status = 'Processing'` và set `next_attempt_at = nowUtc + 10 phút` (cơ chế **Lease Timeout**: nếu server bị sập giữa lúc đang gửi SMTP, sau 10 phút worker khác sẽ tự động nhặt lại).
- Commit transaction claim **trước** khi gọi SMTP để không giữ lock database trong lúc chờ mạng gửi mail.

### 1.2. Gửi theo lô & Cập nhật kết quả

1. Render từng `ClaimedNotificationDelivery` thành `EmailBatchItem` thông qua `NotificationEmailTemplateRenderer`.
2. Gọi `IEmailSender.SendBatchAsync(emailBatch)` (mở 1 kết nối SMTP gửi toàn bộ 25 thư).
3. Với mỗi kết quả trả về:
   - **Thành công:** `Status = Accepted`, `SentAt = nowUtc`.
   - **Lỗi vĩnh viễn (`IsPermanentFailure == true`) hoặc `AttemptCount >= MaxAttempts` (5 lần):** `Status = FailedPermanent`, ghi log warning.
   - **Lỗi tạm thời:** `Status = RetryScheduled`, `NextAttemptAt = nowUtc + backoff`:
     - Lần 1: `+1 phút`
     - Lần 2: `+5 phút`
     - Lần 3: `+15 phút`
     - Lần 4: `+30 phút`
     - Lần 5+: `+60 phút`

> **Tối ưu xả hàng tồn (Drain Loop):** Trong mỗi chu kỳ 5 giây, nếu `DispatchNextBatchAsync` xử lý đủ `batchSize` (25 thư), worker lặp tiếp tối đa 5 lượt liên tiếp (125 thư) thay vì phải đợi 5 giây cho mỗi lô.

---

## 2. Cách bắn sự kiện từ nghiệp vụ (Transactional Outbox)

Để đảm bảo **"Lưu dữ liệu thành công thì chắc chắn có thông báo; lưu thất bại/rollback thì không bao giờ gửi nhầm thông báo"**:

### Cách A (Đơn giản — Monolith 1 DbContext)
- Gọi `INotificationIngestionService` hoặc thêm trực tiếp `Notification` + `NotificationDelivery` vào cùng `DbContext` trước khi gọi `SaveChangesAsync()`.

### Cách B (Chuẩn Modular Monolith như UGTM — Khuyên dùng nếu tách module)
1. Module nghiệp vụ chỉ inject interface nhỏ `I<Module>NotificationOutbox` (có hàm `void Enqueue(NotificationOutboxEntry entry, DateTime occurredAtUtc)`).
2. Hàm `Enqueue` chỉ gọi `db.NotificationEventOutboxMessages.Add(...)` **đồng bộ**, không tự gọi `SaveChangesAsync()`. Khi `unitOfWork.SaveChangesAsync()` của handler chạy, bản ghi outbox được commit **cùng 1 DB transaction** với dữ liệu nghiệp vụ.
3. `NotificationEventRelayWorker` (chạy nền mỗi 5 giây) đọc các bản ghi outbox bằng `FOR UPDATE SKIP LOCKED`, dùng `INotificationRecipientDirectory` phân giải `UserId / Role / Permission` thành `(UserId, FullName, Email)` của các tài khoản đang `Active`, rồi đẩy sang `INotificationIngestionService`.

> **Mẹo viết nội dung (`Content`) gọn đẹp:** Giữ `Content` trong khoảng **15–20 từ tiếng Việt** đủ ý (VD: *"Ghi nhận tệp Đề cương v1 của nhóm. Khoa sẽ thẩm định và phản hồi sớm theo kế hoạch."*). Mọi thông tin dài (tên đề tài dài, lý do từ chối, tên file, dung lượng, giờ nộp) hãy đưa vào `EmailMetadata` dạng bảng 2 cột.

---

## 3. Tích hợp Frontend: Quả chuông `NotificationBell` (React + TypeScript)

### 3.1. Hook `useNotificationInbox`

Sử dụng `@tanstack/react-query` với 2 query chính:
1. `unreadCountQuery` (`GET /api/notifications/unread-count`):
   - `refetchInterval`: **60_000 ms** (60 giây), chỉ poll khi tab trình duyệt đang mở (`document.visibilityState === 'visible'` hoặc mặc định của React Query `refetchIntervalInBackground: false`).
   - `refetchOnWindowFocus: true`: Khi user chuyển từ tab khác quay lại web, số chưa đọc tự cập nhật ngay lập tức.
2. `listQuery` (`GET /api/notifications?unreadOnly=...&pageNumber=1&pageSize=12`):
   - Chỉ fetch (hoặc ưu tiên fetch) khi người dùng mở popover quả chuông hoặc đang mở danh sách.
3. **Mutations:**
   - `markRead(id)`: Optimistic update (trừ `unreadCount` đi 1 và set `isRead = true` ngay lập tức trên cache) rồi gọi `PATCH /api/notifications/{id}/read`, sau đó invalidate cache.
   - `markAllRead()`: Gọi `PATCH /api/notifications/read-all` và cập nhật toàn bộ danh sách về `isRead = true`.

### 3.2. Component `NotificationBell.tsx`

- Đặt trên thanh Header (`AppLayout.tsx`).
- Hiển thị Icon chuông + Badge số lượng chưa đọc (nếu `> 99` hiển thị `99+`).
- Khi mở Popover:
  - Header: Tiêu đề *"Thông báo"*, tab lọc **"Tất cả"** / **"Chưa đọc"**, nút **"Đọc tất cả"**.
  - Mỗi item hiển thị: Chấm xanh chưa đọc, dấu nhấn `Critical` (viền/nhãn cam-đỏ nếu là thông báo quan trọng), `Title`, `Content` (clamp 2 dòng), thời gian tương đối (*"5 phút trước"*, *"2 giờ trước"*).
  - Khi click vào item: gọi `markRead(item.id)` (nếu chưa đọc) → đóng popover → nếu có `item.actionPath`, điều hướng thẳng tới trang xử lý bằng `navigate(item.actionPath)`.

---

## 4. Checklist Triển khai & Test End-to-End cho Project mới

- [ ] **Bước 1 (Tài khoản Mail):** Tạo Gmail mới → bật 2FA → tạo App Password 16 ký tự → lưu vào `.env` (không commit Git).
- [ ] **Bước 2 (SMTP Service):** Cài `MailKit`, tạo `SmtpOptions` + `IEmailSender` + `SmtpEmailSender` (có `SendBatchAsync` reuse connection) + `NotificationEmailTemplateRenderer`.
- [ ] **Bước 3 (Database):** Tạo bảng `notification` và `notification_delivery` (kèm Partial Unique Index cho `deduplication_key` và Partial Index cho hàng đợi `Pending`/`RetryScheduled`).
- [ ] **Bước 4 (Ingestion & API):** Viết `NotificationIngestionService` và 4 endpoint `/api/notifications*` lọc chặt theo `currentUser.UserId`.
- [ ] **Bước 5 (Worker):** Đăng ký `AddHostedService<NotificationDeliveryWorker>()` (và `NotificationEventRelayWorker` nếu dùng Outbox).
- [ ] **Bước 6 (Frontend):** Gắn `NotificationBell` lên `AppLayout` và kiểm tra điều hướng `actionPath`.
- [ ] **Bước 7 (Test E2E thực tế):**
  1. Khởi động Backend với `.env` đã điền App Password.
  2. Kích hoạt 1 nghiệp vụ có bật `EnqueueEmail = true` (hoặc chèn 1 dòng test vào outbox/ingestion).
  3. Kiểm tra 3 kết quả:
     - Bảng `notification` có bản ghi mới → quả chuông Frontend sáng badge đỏ, bấm vào xem & điều hướng đúng `actionPath`.
     - Bảng `notification_delivery` chuyển `Pending` → `Processing` → `Accepted` (có `sent_at`).
     - Hộp thư Gmail nhận được email đúng format HTML, hiển thị chuẩn tiếng Việt UTF-8, nút CTA mở đúng trang web.
