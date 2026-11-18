import type { AboGroup, RhValue } from '../types/medical'

const BLOOD_BY_NUM: Record<number, AboGroup> = { 0: 'A', 1: 'B', 2: 'AB', 3: 'O' }
const RH_BY_NUM: Record<number, RhValue> = { 0: 'POSITIVE', 1: 'NEGATIVE' }
const RH_BY_STR: Record<string, string> = {
    POSITIVE: '+', POS: '+', '+': '+', 'RH+': '+',
    NEGATIVE: '-', NEG: '-', '-': '-', 'RH-': '-',
}

/**
 * Hiển thị nhóm máu kèm yếu tố Rh: "A+", "O-", "Chưa xác định"
 * Nhận cả số (0..3, 0..1 từ Backend Enum) lẫn chuỗi
 */
export function formatBloodGroup(
    abo?: AboGroup | number | string | null,
    rh?: RhValue | number | string | null,
): string {
    if (abo === null || abo === undefined || abo === '') return 'Chưa xác định'
    const aboStr = typeof abo === 'number' ? BLOOD_BY_NUM[abo] ?? '?' : String(abo).toUpperCase()
    let sign = ''
    if (rh !== null && rh !== undefined && rh !== '') {
        sign = typeof rh === 'number'
            ? (RH_BY_NUM[rh] === 'POSITIVE' ? '+' : '-')
            : (RH_BY_STR[String(rh).toUpperCase()] ?? '')
    }
    return `${aboStr}${sign}`
}

/**
 * Alias tương thích ngược cho fmtBlood và fmtBloodType
 */
export const fmtBlood = formatBloodGroup
export const fmtBloodType = formatBloodGroup

/**
 * Tách chuỗi nhóm máu dạng "A+", "O-" thành { bloodType, rhFactor }
 */
export function parseBloodGroup(value: string): { bloodType: AboGroup; rhFactor: RhValue } {
    const clean = value.trim()
    const sign = clean.slice(-1)
    return {
        bloodType: clean.slice(0, -1) as AboGroup,
        rhFactor: sign === '+' ? 'POSITIVE' : 'NEGATIVE',
    }
}
