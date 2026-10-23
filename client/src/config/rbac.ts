import type { RoleCode } from '@/types/user'

export const ROLE_META: Record<RoleCode, { label: string; desc: string }> = {
    DON: { label: 'Nhân viên Tiếp nhận & Lấy máu', desc: 'Tiếp nhận tình nguyện viên, hướng dẫn và thực hiện lấy máu' },
    BBNK: { label: 'Nhân viên Ngân hàng máu', desc: 'Quản lý kho máu, xét nghiệm và cấp phát' },
    CLIN: { label: 'Bác sĩ / Nhân viên Lâm sàng', desc: 'Tạo y lệnh và yêu cầu truyền máu' },
    MGT: { label: 'Ban Giám đốc / Quản lý', desc: 'Xem báo cáo và phê duyệt chiến dịch' },
    SYS: { label: 'Quản trị hệ thống', desc: 'Quyền cao nhất toàn bộ hệ thống' },
}

export const ROLE_CODES = Object.keys(ROLE_META) as RoleCode[]

export const SYSTEM_ROLE_CODES: readonly string[] = ['SYS']

/**
 * Chỉ dùng để đặt nhãn + thứ tự cột. `key` phải khớp enum PermissionAction ở BE
 * (VIEW / EDIT / APPROVE / CANCEL). Module KHÔNG còn hard-code ở FE:
 * danh sách module lấy hoàn toàn từ bảng `permissions` qua API.
 */
export const ACTIONS = [
    { key: 'VIEW', label: 'Xem' },
    { key: 'EDIT', label: 'Tạo / Sửa' },
    { key: 'APPROVE', label: 'Duyệt' },
    { key: 'CANCEL', label: 'Hủy / Từ chối' },
] as const

export type ActionKey = (typeof ACTIONS)[number]['key']