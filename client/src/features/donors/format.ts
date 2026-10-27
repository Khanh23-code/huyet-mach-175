import type { EnumValue } from './types'

export const fmtDate = (iso?: string | null) => {
    if (!iso) return '—'
    const [y, m, d] = iso.slice(0, 10).split('-')
    return `${d}/${m}/${y}`
}

export const GENDER_OPTIONS = [
    { value: 'Nam', label: 'Nam' },
    { value: 'Nữ', label: 'Nữ' },
    { value: 'Khác', label: 'Khác' },
]

// BloodType / RhFactor KHÔNG gắn JsonStringEnumConverter -> mặc định BE trả SỐ theo thứ tự khai báo trong Enums.cs.
// (Nếu sau này bật converter toàn cục thì nhánh chuỗi bên dưới vẫn chạy.)
const BLOOD_BY_NUMBER: Record<number, string> = { 0: 'A', 1: 'B', 2: 'AB', 3: 'O' }
const RH_BY_NUMBER: Record<number, string> = { 0: '+', 1: '-' }
const RH_BY_STRING: Record<string, string> = {
    POSITIVE: '+', POS: '+', '+': '+',
    NEGATIVE: '-', NEG: '-', '-': '-',
}

export const fmtBloodType = (bt?: EnumValue | null, rh?: EnumValue | null) => {
    if (bt === null || bt === undefined) return 'Chưa xác định'
    const group = typeof bt === 'number' ? BLOOD_BY_NUMBER[bt] ?? '?' : String(bt).toUpperCase()
    let sign = ''
    if (rh !== null && rh !== undefined) {
        sign = typeof rh === 'number' ? RH_BY_NUMBER[rh] ?? '' : RH_BY_STRING[String(rh).toUpperCase()] ?? ''
    }
    return `${group}${sign}`
}

export const ageFromIso = (iso: string): number => {
    const [y, m, d] = iso.split('-').map(Number)
    const t = new Date()
    let age = t.getFullYear() - y
    const month = t.getMonth() + 1
    if (month < m || (month === m && t.getDate() < d)) age--
    return age
}

export const initials = (name: string) =>
    name.trim().split(/\s+/).slice(-2).map((w) => w[0]).join('').toUpperCase()

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