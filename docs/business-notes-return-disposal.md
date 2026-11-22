# Ghi chú Nghiệp vụ: Luồng Kiểm định Hoàn trả & Tiêu hủy theo lô (`BloodReturn` → `BloodDisposal`)

> Tài liệu tổng hợp các quyết định nghiệp vụ, cấu hình chế phẩm và các điểm cần chỉnh sửa cho luồng Kiểm định Hoàn trả – Tiêu hủy (không bao gồm chi tiết lập trình).

---

## 1. Khớp nối Hoàn trả → Tiêu hủy
- [ ] **Thống nhất 1 đường tiêu hủy duy nhất (không nhảy cóc qua `MGT`):**
  - Khi KTV kiểm định túi máu hoàn trả và kết luận **`DISCARD`** (không đạt tái nhập kho):
    - Phiếu hoàn trả chốt `FinalAction = DISCARD`, `Qualified = false`, `BloodAllocation` chuyển `RETURNED`.
    - Túi máu **giữ nguyên trạng thái `LOCKED`** (không chuyển thẳng sang `DISCARDED`), ghi rõ `DiscardReason` và xếp vào **tủ/vùng cách ly chờ hủy**.
    - Túi máu tự động chảy vào danh sách **Ứng viên tiêu hủy** để KTV gom lập Biên bản tiêu hủy theo lô → Ban Giám đốc (`MGT`) phê duyệt → KTV xác nhận tiêu hủy vật lý (`DISCARDED`).
- [ ] **Chặn va chạm giữa túi đang chờ kiểm định hoàn trả và danh sách Ứng viên tiêu hủy:**
  - Danh sách ứng viên tiêu hủy phải **loại trừ** các túi đang có phiếu hoàn trả ở trạng thái `PENDING_INSPECTION` (mới được Lâm sàng lập phiếu, chưa qua kiểm định).
  - Chỉ túi đã kiểm định hoàn trả xong với kết luận `DISCARD` mới được phép xuất hiện trong danh sách chờ lập biên bản tiêu hủy.

---

## 2. Kiểm định Tái nhập kho (`RESTOCK`) & Cấu hình động theo Loại chế phẩm

**Cơ sở chuyên môn & pháp lý (Thông tư 26/2013/TT-BYT):**
- **Khoản 3 Điều 32 & Khoản 3 Điều 33:** Nghiêm cấm đông lạnh lại huyết tương / huyết tương tươi đông lạnh (`FFP`) đã làm tan đông.
- **Điều 51:** Chỉ được tái sử dụng túi máu hoàn trả khi còn hạn, không có dấu hiệu bất thường và đảm bảo đúng điều kiện bảo quản/vận chuyển. Việc giới hạn số lần tái nhập kho (mặc định 1 lần) là quy chế nội bộ (SOP) để ngăn tích lũy thời gian ngoài tủ lạnh.

**Chỉnh sửa nghiệp vụ đã chốt:**
- [ ] **Kiểm tra nhiệt độ tủ đích theo loại chế phẩm khi `RESTOCK`:**
  - Bắt buộc đối chiếu nhiệt độ mục tiêu của ngăn tủ đích với dải nhiệt độ bảo quản chuẩn của loại chế phẩm đó (tránh tái nhập nhầm tủ đông/tủ lạnh/tủ lắc).
- [ ] **Áp dụng cấu hình động theo Loại chế phẩm (Data-driven Config trên `BloodComponentType`):**
  - Thay vì viết cứng quy tắc cho từng chế phẩm (`WB`, `PRBC`, `FFP`, `PLT`), xây dựng trang/chức năng cấu hình tham số cho từng loại chế phẩm máu (để mở rộng chế phẩm mới không cần sửa code):
    - **Dải nhiệt độ bảo quản chuẩn (`MinTemp` – `MaxTemp`):** Dùng chung cho Tiếp nhận, Điều chế, Điều chuyển tủ và Tái nhập kho (`RESTOCK`).
    - **Hạn sử dụng (`LifeDays`):** Đã có sẵn.
    - **Cho phép tái nhập kho (`AllowRestock`):** Ví dụ `WB/PRBC` = Có; `FFP` đã rã đông = Không.
    - **Thời gian tối đa ngoài tủ để tái nhập (phút):** Ví dụ `30` phút cho `WB/PRBC`.
    - **Số lần tái nhập kho tối đa (`MaxRestockCount`):** Mặc định `1` lần theo SOP nội bộ (có thể thay đổi qua cấu hình).
    - **Nhóm quy tắc tương thích nhóm máu (`CompatibilityRuleType`):** Gắn nhóm quy tắc miễn dịch (`Hồng cầu` / `Huyết tương` / `Tiểu cầu`).

---

## 3. Nội bộ luồng Tiêu hủy theo lô (`BloodDisposal`)
- [ ] **Chặn đưa túi đang `RESERVED` vào biên bản tiêu hủy:**
  - Không cho phép lập biên bản tiêu hủy đối với túi đang giữ chỗ cho phiếu yêu cầu máu (`RESERVED`). Nếu túi giữ chỗ bị hỏng đột xuất, phải hủy giữ chỗ / đổi túi thay thế cho phiếu yêu cầu trước rồi mới khóa đưa đi hủy.
- [ ] **Làm sạch dữ liệu & nối mạch truy vết khi thực thi tiêu hủy (`DISPOSED` → `DISCARDED`):**
  - Khi xác nhận tiêu hủy vật lý, bắt buộc **giải phóng vị trí ngăn tủ** (`LocationId = null`).
  - Ghi nhận đầy đủ lịch sử trạng thái từng túi (`BloodBagStatusLog`) xuyên suốt các bước của biên bản tiêu hủy (lập biên bản, rút/từ chối biên bản, xác nhận tiêu hủy vật lý) để không bị đứt đoạn timeline vòng đời túi máu.
- [ ] **Bổ sung nhóm lý do tiêu hủy cho lỗi chuỗi lạnh:**
  - Bổ sung mã lý do chuyên biệt `COLD_CHAIN_BREACH` (Vi phạm chuỗi lạnh / Quá thời gian ngoài tủ) vào danh mục lý do tiêu hủy để phục vụ thống kê, báo cáo lãng phí máu cho Ban Giám đốc.

---

## 4. Tác động Cơ sở dữ liệu & Migration (Phạm vi luồng Hoàn trả – Tiêu hủy & Kho)

> Khuyến nghị gom toàn bộ các thay đổi cộng thêm (additive) thành **1 lần migration duy nhất** sau khi chốt xong toàn bộ nghiệp vụ.

- **Không cần sửa DB:**
  - Hoàn trả không đạt (`DISCARD`) giữ nguyên `LOCKED` để chảy sang `BloodDisposal`.
  - Loại trừ túi đang `PENDING_INSPECTION` khỏi danh sách ứng viên tiêu hủy.
  - Chặn đưa túi `RESERVED` vào biên bản tiêu hủy.
  - Giải phóng vị trí tủ (`LocationId = null`) và bổ sung ghi `BloodBagStatusLog` khi tiêu hủy.
  - Đếm số lần tái nhập kho của 1 túi (tính trực tiếp từ lịch sử `BloodReturns` có `FinalAction = RESTOCK`).
- **Cần thêm migration (chỉ cộng thêm cột / mở rộng enum, không phá bảng cũ):**
  - **Bảng `BloodComponentType`:** Thêm các cột cấu hình (`MinTemp`, `MaxTemp`, `AllowRestock`, `MaxOutsideMinutesForRestock`, `MaxRestockCount`, `CompatibilityRuleType`).
  - **Bảng `StorageLocation`:** Thêm cột phân vùng kho (`Zone`: Biệt trữ chờ xét nghiệm / Sẵn sàng cấp phát / Cách ly chờ hủy).
  - **Mở rộng Enum `DisposalReason`:** Thêm `COLD_CHAIN_BREACH`.
  - **Mở rộng Enum `ReturnFinalAction`:** Thêm `CANCELLED` (dùng chung với luồng hủy phiếu hoàn trả lập nhầm).
