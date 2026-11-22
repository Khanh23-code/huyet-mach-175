# Ghi chú Nghiệp vụ: Luồng Cấp phát & Vòng đời Túi máu sau Xuất kho (`ISSUED`)

> Tài liệu tổng hợp các quyết định nghiệp vụ và các điểm cần chỉnh sửa cho luồng Cấp phát – Xác nhận sử dụng – Hoàn trả đầu vào (không bao gồm chi tiết lập trình).

---

## 1. Quy ước vận hành nền tảng đã chốt
- **Mô hình giao nhận cấp phát:** Khoa lâm sàng cử nhân sự xuống lĩnh máu trực tiếp tại quầy Ngân hàng máu (bàn giao đồng thời 2 phía).
- **Ranh giới trách nhiệm của `BloodRequest`:** Chỉ quản lý phạm vi **Yêu cầu & Cấp phát**. Khi toàn bộ đơn vị máu yêu cầu đã được xuất kho và phía Lâm sàng xác nhận nhận đủ, phiếu yêu cầu chốt trạng thái `COMPLETED`. Các diễn biến sau khi giao (truyền máu hoặc hoàn trả) được theo dõi độc lập trên từng túi máu (`BloodBag`) và lượt phân bổ (`BloodAllocation`).

---

## 2. Cơ chế Bàn giao đồng thời tại quầy (Synchronous Handover & Timeout 3–5 phút)

**Mục tiêu nghiệp vụ:** Triệt tiêu "vùng mù" thời gian giữa thời điểm Ngân hàng máu xuất kho và thời điểm Lâm sàng nhận máu, đảm bảo cả hai bên đối chiếu trực tiếp tại quầy trước khi túi máu rời tủ bảo quản.

- [ ] **Áp dụng quy trình bàn giao 2 pha (Commit-on-Confirm) theo lô:**
  - **Pha 1 – Khởi tạo phiên bàn giao (Phía Ngân hàng máu):**
    - KTV Ngân hàng máu quét barcode một hoặc nhiều túi máu (gom lô theo phiếu yêu cầu) để mở phiên bàn giao chờ xác nhận.
    - Hệ thống gửi thông báo in-app (hoặc hiển thị mã xác nhận phiên) để thúc đẩy phía Lâm sàng quét xác nhận ngay tại quầy.
    - Trong thời gian chờ xác nhận, túi máu **chưa chuyển sang `ISSUED`** và **chưa bắt đầu chạy đồng hồ chuỗi lạnh** (vẫn giữ nguyên vị trí tủ và liên kết giữ chỗ).
  - **Pha 2 – Xác nhận nhận máu (Phía Lâm sàng):**
    - Nhân sự Lâm sàng (cùng khoa với bác sĩ lập phiếu; nếu nhận hộ bác sĩ khác bắt buộc đối chiếu mã người bệnh) quét barcode xác nhận trong khung thời gian cho phép.
    - Khi xác nhận thành công: túi máu chính thức chuyển sang **`ISSUED`** (giải phóng vị trí tủ, bắt đầu tính mốc thời gian chuỗi lạnh), cập nhật `BloodAllocation`, và nếu đủ toàn bộ đơn vị của phiếu thì chốt `BloodRequest = COMPLETED`.
    - **Quy tắc vận hành tại quầy (SOP):** Chỉ khi màn hình báo hoàn tất Pha 2 thì KTV mới trao túi máu vào thùng bảo ôn cho người lĩnh.
- [ ] **Giới hạn thời gian chờ (Timeout) & Cơ chế Rollback:**
  - Thiết lập thời gian chờ tối đa cho mỗi phiên bàn giao là **3–5 phút** (tạm chốt khoảng này, đồng thời cho phép KTV chủ động bấm hủy phiên sớm nếu có sự cố tại quầy).
  - Nếu hết **3–5 phút** mà phía Lâm sàng chưa quét xác nhận thành công → phiên bàn giao **tự động hủy (rollback)**:
    - Toàn bộ túi máu trong phiên **quay về trạng thái `RESERVED`** (tuyệt đối **không** thả về `AVAILABLE`).
    - Giữ nguyên `BloodAllocation`, giữ nguyên vị trí ngăn tủ lưu trữ và giữ nguyên trạng thái phiếu `BloodRequest` (`READY` / `PENDING`) để tránh mất công giữ chỗ và phản ứng chéo (crossmatch); hai bên có thể thao tác mở lại phiên bàn giao ngay sau đó.

---

## 3. Nghiệp vụ vòng đời túi máu ở trạng thái `ISSUED`

**Nguyên tắc cốt lõi:** Túi máu sau khi đã xuất kho giao cho Lâm sàng (`ISSUED`) chỉ được phép chuyển sang **2 nhánh trạng thái chính thống**:
1. `ISSUED` → `TRANSFUSED` (khi bắt đầu sử dụng thực tế cho bệnh nhân).
2. `ISSUED` → `LOCKED` (tự động khi túi được đưa vào phiếu hoàn trả mới).

### Điểm 1: Bổ sung & chuẩn hóa luồng xác nhận sử dụng thực tế (`ISSUED` → `TRANSFUSED`)
- [ ] **Bổ sung thao tác xác nhận truyền máu cho phía Lâm sàng:**
  - Người thực hiện (Bác sĩ/Điều dưỡng) quét mã vạch (barcode) túi máu để chuyển trạng thái túi từ `ISSUED` sang `TRANSFUSED`.
- [ ] **Quy ước thời điểm quét:**
  - Quét xác nhận **ngay tại thời điểm chuẩn bị mở túi / cắm dây truyền tại giường bệnh** (trước khi truyền), nhằm khóa ngay khả năng hoàn trả túi đã mở.
- [ ] **Ràng buộc đối chiếu an toàn khi quét:**
  - Chỉ cho phép nhân sự **thuộc đúng khoa** của phiếu yêu cầu thực hiện xác nhận.
  - Khi quét barcode túi máu, hệ thống phải hiển thị thông tin đối chiếu bắt buộc: **Tên bệnh nhân, Mã bệnh nhân, Nhóm máu (ABO/Rh), Loại chế phẩm** gắn với lượt phân bổ của túi đó để người thực hiện kiểm tra khớp trước khi xác nhận.
  - Túi đã chuyển sang `TRANSFUSED` là trạng thái kết thúc vĩnh viễn: **chặn hoàn toàn** việc lập phiếu hoàn trả hoặc đổi trạng thái ngược lại.

### Điểm 2: Hoàn thiện chốt kiểm soát cho nhánh hoàn trả đầu vào (`ISSUED` → `LOCKED`)
- [ ] **Cho phép hủy phiếu hoàn trả khi chưa kiểm định (đường lui khi quét nhầm):**
  - Nếu Lâm sàng lập nhầm phiếu hoàn trả cho một túi máu (túi đã bị khóa `LOCKED`) nhưng Ngân hàng máu **chưa thực hiện kiểm định**, cho phép Lâm sàng hủy phiếu hoàn trả đó.
  - Khi hủy thành công, trạng thái túi máu tự động khôi phục về `ISSUED` để tiếp tục sử dụng cho bệnh nhân.
- [ ] **Cho phép lập phiếu hoàn trả cho cả túi không đạt điều kiện tái nhập kho (túi hỏng / quá hạn chuỗi lạnh tại khoa):**
  - Bỏ ràng buộc chặn tạo phiếu khi vi phạm 4 điều kiện hoàn trả (chưa dùng, túi nguyên vẹn, nhãn rõ, ngoài tủ < 30 phút).
  - Mọi túi ở trạng thái `ISSUED` chưa truyền (kể cả túi bị rách, mờ nhãn, quá 30 phút ngoài tủ) đều được phép lập phiếu hoàn trả để chuyển `ISSUED` → `LOCKED` và bàn giao về Ngân hàng máu xử lý.
  - 4 điều kiện đầu vào chỉ đóng vai trò **phân loại hướng xử lý** (đủ điều kiện kiểm định tái nhập kho vs. gắn cờ bắt buộc tiêu hủy), không chặn quyền lập phiếu.

### Điểm 3: Khóa quyền chuyển trạng thái thủ công đối với túi đang `ISSUED` từ phía Kho
- [ ] **Đóng "cửa hậu" chuyển trạng thái tay (`BloodBagTransitionRules`):**
  - Vô hiệu hóa các chuyển đổi thủ công từ phía Ngân hàng máu đối với túi đang ở trạng thái `ISSUED`:
    - Không cho phép chuyển tay `ISSUED` → `TRANSFUSED`.
    - Không cho phép chuyển tay `ISSUED` → `AVAILABLE`.
- [ ] **Đảm bảo tính nhất quán luồng:**
  - Mọi thay đổi trạng thái của túi `ISSUED` bắt buộc phải phát sinh từ 1 trong 2 nghiệp vụ chính thống của Lâm sàng: **Xác nhận sử dụng (`TRANSFUSED`)** hoặc **Lập phiếu hoàn trả (`LOCKED`)**.

---

## 4. Tác động Cơ sở dữ liệu & Migration (Phạm vi luồng Cấp phát)

- **Không cần sửa DB:**
  - Luồng xác nhận truyền máu (`ISSUED` → `TRANSFUSED`) — trạng thái `TRANSFUSED` đã có sẵn.
  - Bỏ chặn 4 checkbox khi Lâm sàng lập phiếu hoàn trả.
  - Khóa chuyển trạng thái thủ công đối với túi `ISSUED` từ phía Kho.
- **Cần bổ sung cấu trúc lưu trữ / mở rộng Enum (gộp chung 1 lần migration với luồng Hoàn trả – Tiêu hủy):**
  - **Lưu thông tin Phiên bàn giao chờ xác nhận (Handover Session):** Lưu trạng thái phiên, danh sách túi trong lô, thời điểm tạo và thời điểm hết hạn (3–5 phút) để phục vụ xác nhận đồng thời và tự động rollback khi timeout.
  - **Mở rộng Enum `ReturnFinalAction`:** Thêm giá trị `CANCELLED` để lưu vết lịch sử khi Lâm sàng hủy phiếu hoàn trả lập nhầm (thay vì xóa cứng bản ghi).

---

## 5. Ghi chú tạm hoãn (Sẽ rà soát ở các phiên sau)
- **Edge case `BloodRequest` cấp phát từng phần:** Trường hợp yêu cầu nhiều túi, đã xuất một phần và Lâm sàng muốn hủy phần còn lại chưa xuất để đóng phiếu sớm.
