import type { StatusPillTone } from '@/shared/components'
import type { AppointmentStatus } from './types'


export { getTodayIso, todayIso } from '@/shared/utils/date'
export { formatTime, fmtTime } from '@/shared/formatters/date'

export const waitingMinutes = (iso?: string | null): number | null => {
    if (!iso) return null
    const t = new Date(iso).getTime()
    return Number.isNaN(t) ? null : Math.max(0, Math.floor((Date.now() - t) / 60000))
}

// Thứ tự theo Enums.cs: BOOKED=0, CHECKED_IN=1, CANCELLED=2, NO_SHOW=3
const STATUS_BY_NUMBER: AppointmentStatus[] = ['BOOKED', 'CHECKED_IN', 'CANCELLED', 'NO_SHOW']

export const normalizeStatus = (s: AppointmentStatus | number | string): AppointmentStatus =>
    typeof s === 'number' ? STATUS_BY_NUMBER[s] ?? 'BOOKED' : (s.toUpperCase() as AppointmentStatus)

export const STATUS_LABEL: Record<AppointmentStatus, string> = {
    BOOKED: 'Đã đặt lịch',
    CHECKED_IN: 'Đã check-in',
    CANCELLED: 'Đã hủy',
    NO_SHOW: 'Vắng mặt',
}

export const STATUS_COLOR: Record<AppointmentStatus, string> = {
    BOOKED: 'blue',
    CHECKED_IN: 'orange',
    CANCELLED: 'default',
    NO_SHOW: 'red',
}

export const STATUS_TONE: Record<AppointmentStatus, StatusPillTone> = {
    BOOKED: 'info',
    CHECKED_IN: 'warning',
    CANCELLED: 'neutral',
    NO_SHOW: 'danger',
}