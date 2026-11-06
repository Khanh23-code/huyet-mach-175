import type { DisposalReason, DisposalStatus } from './disposalsTypes'

export const REASON_OPTIONS: { value: DisposalReason; label: string }[] = [
    { value: 'INFECTIOUS_POSITIVE', label: 'Dương tính bệnh truyền nhiễm' },
    { value: 'EXPIRED', label: 'Hết hạn sử dụng' },
    { value: 'HEMOLYSIS', label: 'Máu tán huyết / biến chất' },
    { value: 'DAMAGED_BAG', label: 'Hỏng túi / rò rỉ' },
    { value: 'OTHER', label: 'Lý do khác' },
]

export const STATUS_OPTIONS: { value: DisposalStatus; label: string }[] = [
    { value: 'PENDING_APPROVAL', label: 'Chờ phê duyệt' },
    { value: 'APPROVED', label: 'Đã phê duyệt (chờ tiêu hủy)' },
    { value: 'DISPOSED', label: 'Đã tiêu hủy' },
    { value: 'REJECTED', label: 'Từ chối tiêu hủy' },
    { value: 'CANCELLED', label: 'Đã hủy yêu cầu' },
]

export const STATUS_COLOR: Record<DisposalStatus, string> = {
    PENDING_APPROVAL: 'gold',
    APPROVED: 'blue',
    DISPOSED: 'green',
    REJECTED: 'red',
    CANCELLED: 'default',
}