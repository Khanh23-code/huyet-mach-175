import { fmtDate } from '@/features/donors/format'
import type { Conclusion, ScreeningStatus } from './types'

const pad = (n: number) => String(n).padStart(2, '0')

/** "YYYY-MM-DD" + n ngày -> "YYYY-MM-DD" (theo giờ máy) */
export const addDaysIso = (iso: string, days: number) => {
    const [y, m, d] = iso.slice(0, 10).split('-').map(Number)
    const t = new Date(y, m - 1, d + days)
    return `${t.getFullYear()}-${pad(t.getMonth() + 1)}-${pad(t.getDate())}`
}

export const fmtDateTime = (iso?: string | null) => {
    if (!iso) return '—'
    const d = new Date(iso)
    if (Number.isNaN(d.getTime())) return '—'
    return `${pad(d.getHours())}:${pad(d.getMinutes())} ${fmtDate(
        `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`,
    )}`
}

/** bloodType "O" + rhFactor "Rh+" -> "O+" */
export const fmtBlood = (bloodType?: string | null, rh?: string | null) => {
    if (!bloodType) return 'Chưa xác định'
    const sign = rh === 'Rh+' ? '+' : rh === 'Rh-' ? '-' : ''
    return `${bloodType}${sign}`
}

export const SCREENING_META: Record<ScreeningStatus, { label: string; color: string }> = {
    PENDING: { label: 'Chờ khám', color: 'blue' },
    QUALIFIED: { label: 'Đạt tiêu chuẩn', color: 'green' },
    DEFERRED: { label: 'Tạm hoãn', color: 'orange' },
    UNQUALIFIED: { label: 'Không đạt', color: 'red' },
}

export const CONCLUSIONS: Conclusion[] = ['QUALIFIED', 'DEFERRED', 'UNQUALIFIED']

export const VOLUME_OPTIONS = [
    { value: 'VOL_250', label: '250 ml' },
    { value: 'VOL_350', label: '350 ml' },
    { value: 'VOL_450', label: '450 ml' },
]

/**
 * 9 câu của bảng hỏi online (khóa khớp BE: SurveysController / DbInitializer).
 * q1 chỉ là thông tin (đã từng hiến máu), q2..q9 trả lời "Có" sẽ tính là cảnh báo.
 * TODO: BE chưa trả nội dung câu hỏi. Đối chiếu nhãn dưới đây với bảng hỏi trên app người hiến.
 */
export const QUESTIONS: { key: string; label: string; risk: boolean }[] = [
    { key: 'q1_previous_donation', label: 'Đã từng hiến máu', risk: false },
    { key: 'q2_chronic_diseases', label: 'Bệnh mãn tính / tiền sử bệnh lý', risk: true },
    { key: 'q3_last_12_months', label: 'Vấn đề sức khỏe trong 12 tháng gần đây', risk: true },
    { key: 'q4_last_6_months', label: 'Vấn đề sức khỏe trong 6 tháng gần đây', risk: true },
    { key: 'q5_last_14_days', label: 'Vấn đề sức khỏe trong 14 ngày gần đây', risk: true },
    { key: 'q6_last_7_days', label: 'Vấn đề sức khỏe trong 7 ngày gần đây', risk: true },
    { key: 'q7_infectious_risk', label: 'Nguy cơ lây nhiễm', risk: true },
    { key: 'q8_unexplained_symptoms', label: 'Triệu chứng bất thường chưa rõ nguyên nhân', risk: true },
    { key: 'q9_female_specific', label: 'Câu hỏi dành riêng cho nữ giới', risk: true },
]

export type Answers = Record<string, boolean>

export const toBool = (v: unknown) =>
    v === true || (typeof v === 'string' && v.toLowerCase() === 'true')

export const answersFrom = (raw?: Record<string, unknown> | null): Answers => {
    const out: Answers = Object.fromEntries(QUESTIONS.map((q) => [q.key, false]))
    if (raw) for (const [k, v] of Object.entries(raw)) out[k] = toBool(v)
    return out
}

export const questionMeta = (key: string) =>
    QUESTIONS.find((q) => q.key === key) ?? { key, label: key, risk: true }