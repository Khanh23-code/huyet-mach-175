import type { RoleCode } from '@/types/user'

export const ROLE_META: Record<RoleCode, { label: string; desc: string }> = {
    DON: { label: 'Nhân viên Tiếp nhận & Lấy máu', desc: 'Tiếp nhận tình nguyện viên, hướng dẫn và thực hiện lấy máu' },
    BBNK: { label: 'Nhân viên Ngân hàng máu', desc: 'Quản lý kho máu, xét nghiệm và cấp phát' },
    CLIN: { label: 'Bác sĩ / Nhân viên Lâm sàng', desc: 'Tạo y lệnh và yêu cầu truyền máu' },
    MGT: { label: 'Ban Giám đốc / Quản lý', desc: 'Xem báo cáo và phê duyệt chiến dịch' },
    SYS: { label: 'Quản trị hệ thống', desc: 'Quyền cao nhất toàn bộ hệ thống' },
}

export const ROLE_CODES = Object.keys(ROLE_META) as RoleCode[]

export const MODULES = [
    { code: 'W1', name: 'Tiếp đón & Sàng lọc', desc: 'Quản lý hàng chờ, khám sàng lọc, phiên lấy máu' },
    { code: 'W2', name: 'Túi máu & Kho', desc: 'Khởi tạo, theo dõi vòng đời, kiểm kê và xử lý túi máu' },
    { code: 'W3', name: 'Yêu cầu máu Lâm sàng', desc: 'Lập phiếu, theo dõi và nhận bàn giao túi máu' },
    { code: 'W4', name: 'Điều phối Kho máu', desc: 'Cung cầu, hàng đợi yêu cầu và cấp phát máu' },
    { code: 'W5', name: 'Phê duyệt & Giám sát', desc: 'Phê duyệt hủy, báo cáo và giám sát vận hành' },
    { code: 'W6', name: 'Quản trị hệ thống', desc: 'Người dùng, phân quyền, danh mục, tích hợp và audit log' },
] as const

export const ACTIONS = [
    { key: 'view', label: 'Xem' },
    { key: 'edit', label: 'Thao tác – Tạo/Sửa' },
    { key: 'approve', label: 'Duyệt' },
    { key: 'delete', label: 'Hủy / Xóa' },
] as const

export type ActionKey = (typeof ACTIONS)[number]['key']
export type ModuleCode = (typeof MODULES)[number]['code']
export type RoleMatrix = Record<ModuleCode, Record<ActionKey, boolean>>
