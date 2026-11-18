export { formatDateOnly, fmtDate } from '@/shared/formatters/date'

export const GENDER_OPTIONS = [
    { value: 'Nam', label: 'Nam' },
    { value: 'Nữ', label: 'Nữ' },
    { value: 'Khác', label: 'Khác' },
]

export { formatBloodGroup, formatBloodGroup as fmtBloodType } from '@/shared/formatters/medical'

export const ageFromIso = (iso: string): number => {
    const [y, m, d] = iso.split('-').map(Number)
    const t = new Date()
    let age = t.getFullYear() - y
    const month = t.getMonth() + 1
    if (month < m || (month === m && t.getDate() < d)) age--
    return age
}

export { getAvatarInitials as initials, getAvatarInitials } from '@/shared/utils/string'

export const SCREENING_LABEL: Record<string, string> = {
    PENDING: 'Chờ khám',
    QUALIFIED: 'Đạt',
    DEFERRED: 'Hoãn hiến',
    UNQUALIFIED: 'Không đạt',
}

export const SESSION_LABEL: Record<string, string> = {
    WAITING_COLLECTION: 'Chờ lấy máu',
    COMPLETED: 'Hoàn tất',
    ABORTED: 'Đã hủy',
}