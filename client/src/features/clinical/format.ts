import type { StatusPillTone } from '@/shared/components'
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
        color: 'var(--c-info)',
        bg: 'var(--c-info-soft)',
        border: 'var(--c-info-border)',
    },
    URGENT: {
        label: 'Khẩn',
        color: 'var(--c-warning)',
        bg: 'var(--c-warning-soft)',
        border: 'var(--c-warning-border)',
    },
    EMERGENCY: {
        label: 'Cấp cứu',
        color: 'var(--c-danger)',
        bg: 'var(--c-danger-soft)',
        border: 'var(--c-danger-border)',
    },
}

export function getUrgencyMeta(level?: UrgencyLevel | null): UrgencyMeta {
    if (!level || !URGENCY_MAP[level]) {
        return {
            label: 'Kế hoạch',
            color: 'var(--c-text-secondary)',
            bg: 'var(--c-neutral-soft)',
            border: 'var(--c-neutral-border)',
        }
    }
    return URGENCY_MAP[level]
}

export interface StatusMeta {
    label: string
    color: string
    bg: string
    tone: StatusPillTone
}

export const STATUS_MAP: Record<RequestStatus, StatusMeta> = {
    DRAFT: { label: 'Bản nháp', color: 'default', bg: 'var(--c-neutral-soft)', tone: 'neutral' },
    PENDING: { label: 'Đã gửi', color: 'green', bg: 'var(--c-primary-soft)', tone: 'primary' },
    READY: { label: 'Sẵn sàng bàn giao', color: 'blue', bg: 'var(--c-info-soft)', tone: 'info' },
    COMPLETED: { label: 'Đã bàn giao hoàn tất', color: 'success', bg: 'var(--c-success-soft)', tone: 'success' },
    REJECTED: { label: 'Đã từ chối', color: 'error', bg: 'var(--c-danger-soft)', tone: 'danger' },
}

export function getStatusMeta(status?: RequestStatus | null): StatusMeta {
    if (!status || !STATUS_MAP[status]) {
        return { label: 'Chưa xác định', color: 'default', bg: 'var(--c-neutral-soft)', tone: 'neutral' }
    }
    return STATUS_MAP[status]
}

export function getStatusTone(status?: RequestStatus | string | null): StatusPillTone {
    if (!status) return 'neutral'
    const key = status as RequestStatus
    return STATUS_MAP[key]?.tone ?? 'neutral'
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
