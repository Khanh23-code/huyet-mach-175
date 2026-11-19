import type { RequestStatus, UrgencyLevel } from './types'

export interface UrgencyMeta {
    label: string
    color: string
    bg: string
    border: string
}

export const URGENCY_MAP: Record<UrgencyLevel, UrgencyMeta> = {
    ROUTINE: {
        label: 'Kế hoạch',
        color: '#1F5FA6',
        bg: '#E7F0FA',
        border: '#B4D2F5',
    },
    URGENT: {
        label: 'Khẩn',
        color: '#A85F00',
        bg: '#FFF3DC',
        border: '#FAD59B',
    },
    EMERGENCY: {
        label: 'Cấp cứu',
        color: '#C4261B',
        bg: '#FDECEA',
        border: '#F7A7A3',
    },
}

export function getUrgencyMeta(level?: UrgencyLevel | null): UrgencyMeta {
    if (!level || !URGENCY_MAP[level]) {
        return {
            label: 'Kế hoạch',
            color: '#555',
            bg: '#f0f0f0',
            border: '#d9d9d9',
        }
    }
    return URGENCY_MAP[level]
}

export interface StatusMeta {
    label: string
    color: string
    bg: string
}

export const STATUS_MAP: Record<RequestStatus, StatusMeta> = {
    DRAFT: { label: 'Bản nháp', color: '#666666', bg: '#f5f5f5' },
    PENDING: { label: 'Đã gửi', color: '#1A7534', bg: '#E9F5ED' },
    READY: { label: 'Sẵn sàng bàn giao', color: '#1F5FA6', bg: '#E7F0FA' },
    COMPLETED: { label: 'Đã bàn giao hoàn tất', color: '#15803d', bg: '#dcfce7' },
    REJECTED: { label: 'Đã từ chối', color: '#C4261B', bg: '#FDECEA' },
}

export function getStatusMeta(status?: RequestStatus | null): StatusMeta {
    if (!status || !STATUS_MAP[status]) {
        return { label: 'Chưa xác định', color: '#888', bg: '#fafafa' }
    }
    return STATUS_MAP[status]
}

/** Định dạng số lượng túi: "04 túi", "02 túi" */
export function formatUnits(count: number): string {
    const formatted = String(count).padStart(2, '0')
    return `${formatted} túi`
}

/**
 * 4 bước xử lý chuẩn của phiếu yêu cầu máu phía lâm sàng
 */
export const CLINICAL_PROGRESS_STEPS = [
    { title: 'Đã gửi', stepNumber: 1 },
    { title: 'Đã tiếp nhận', stepNumber: 2 },
    { title: 'Đã chuẩn bị', stepNumber: 3 },
    { title: 'Sẵn sàng', stepNumber: 4 },
]
