import type { CampaignSlot, CampaignStatus, CampaignType } from './types'

export const CAMPAIGN_TYPE_LABEL: Record<CampaignType, string> = {
    EVENT: 'Chiến dịch sự kiện',
    RECURRING: 'Điểm hiến định kỳ',
}

export const CAMPAIGN_STATUS_LABEL: Record<CampaignStatus, string> = {
    DRAFT: 'Bản nháp',
    PENDING_APPROVAL: 'Chờ phê duyệt',
    APPROVED: 'Đã phê duyệt',
    IN_PROGRESS: 'Đang diễn ra',
    COMPLETED: 'Đã hoàn thành',
    CANCELLED: 'Đã hủy',
    REJECTED: 'Bị từ chối',
}

export const CAMPAIGN_STATUS_COLOR: Record<CampaignStatus, string> = {
    DRAFT: 'default',
    PENDING_APPROVAL: 'processing',
    APPROVED: 'cyan',
    IN_PROGRESS: 'success',
    COMPLETED: 'purple',
    CANCELLED: 'error',
    REJECTED: 'error',
}

const STATUS_NUMBER_MAP: Record<number, CampaignStatus> = {
    0: 'DRAFT',
    1: 'PENDING_APPROVAL',
    2: 'APPROVED',
    3: 'REJECTED',
    4: 'IN_PROGRESS',
    5: 'COMPLETED',
    6: 'CANCELLED',
}

const TYPE_NUMBER_MAP: Record<number, CampaignType> = {
    0: 'EVENT',
    1: 'RECURRING',
}

export const normalizeCampaignStatus = (status: unknown): CampaignStatus => {
    if (typeof status === 'number') {
        return STATUS_NUMBER_MAP[status] ?? 'DRAFT'
    }
    if (typeof status === 'string') {
        const num = Number(status)
        if (!Number.isNaN(num) && num in STATUS_NUMBER_MAP) {
            return STATUS_NUMBER_MAP[num]
        }
        const upper = status.toUpperCase().replace(/\s+/g, '_') as CampaignStatus
        if (upper in CAMPAIGN_STATUS_LABEL) {
            return upper
        }
    }
    return 'DRAFT'
}

export const normalizeCampaignType = (type: unknown): CampaignType => {
    if (typeof type === 'number') {
        return TYPE_NUMBER_MAP[type] ?? 'EVENT'
    }
    if (typeof type === 'string') {
        const num = Number(type)
        if (!Number.isNaN(num) && num in TYPE_NUMBER_MAP) {
            return TYPE_NUMBER_MAP[num]
        }
        const upper = type.toUpperCase().replace(/\s+/g, '_') as CampaignType
        if (upper in CAMPAIGN_TYPE_LABEL) {
            return upper
        }
    }
    return 'EVENT'
}

const pad = (n: number) => String(n).padStart(2, '0')

/** Format Date: "YYYY-MM-DD" -> "DD/MM/YYYY" */
export const fmtDate = (d?: string | null) => {
    if (!d) return ''
    const parts = d.split('T')[0].split('-')
    if (parts.length === 3) {
        return `${parts[2]}/${parts[1]}/${parts[0]}`
    }
    const dt = new Date(d)
    return `${pad(dt.getDate())}/${pad(dt.getMonth() + 1)}/${dt.getFullYear()}`
}

/** Format Date & Time: "DD/MM/YYYY · HH:mm" */
export const fmtDateTime = (iso?: string | null) => {
    if (!iso) return ''
    const d = new Date(iso)
    if (Number.isNaN(d.getTime())) return ''
    return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} · ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

/** Format operating days: "2,4,6" -> "T2, T4, T6" */
export const fmtOperatingDays = (days?: string | null) => {
    if (!days) return ''
    const map: Record<string, string> = {
        '2': 'T2',
        '3': 'T3',
        '4': 'T4',
        '5': 'T5',
        '6': 'T6',
        '7': 'T7',
        '8': 'CN',
        'CN': 'CN',
        'SUN': 'CN',
    }
    return days
        .split(',')
        .map((d) => d.trim())
        .map((d) => map[d] ?? `Thứ ${d}`)
        .join(', ')
}

/** 07:00–09:00: 20 · 09:00–11:00: 20 */
export const slotsText = (slots: CampaignSlot[]) =>
    slots
        .map((s) => `${s.startTime.slice(0, 5)}–${s.endTime.slice(0, 5)}: ${s.capacity}`)
        .join(' · ')
