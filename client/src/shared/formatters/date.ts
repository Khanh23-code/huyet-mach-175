import { pad } from '../utils/date'

/**
 * Định dạng ngày Date-Only ("YYYY-MM-DD" -> "DD/MM/YYYY").
 * Xử lý an toàn:
 * - Null / undefined / rỗng -> trả về fallback
 * - Tách chuỗi theo phần tử ngày, kiểm tra tính hợp lệ của ngày, tháng, năm nhuận
 * - Không chuyển đổi múi giờ, bảo đảm không bị lệch ngày sinh, ngày hẹn
 */
export function formatDateOnly(d?: string | null, fallback = '—'): string {
    if (!d) return fallback
    const raw = d.trim()
    if (!raw) return fallback

    // Tách lấy phần ngày trước ký tự 'T' hoặc khoảng trắng
    const datePart = raw.split(/[T\s]/)[0]
    const parts = datePart.split('-')
    if (parts.length === 3) {
        const y = Number(parts[0])
        const m = Number(parts[1])
        const day = Number(parts[2])

        // Kiểm tra hợp lệ cơ bản
        if (
            Number.isInteger(y) && y > 1900 && y < 2200 &&
            Number.isInteger(m) && m >= 1 && m <= 12 &&
            Number.isInteger(day) && day >= 1 && day <= 31
        ) {
            // Kiểm tra ngày trong tháng và năm nhuận
            const daysInMonth = new Date(y, m, 0).getDate()
            if (day <= daysInMonth) {
                return `${pad(day)}/${pad(m)}/${y}`
            }
            return fallback
        }
    }

    // Fallback thử Date nếu chuỗi định dạng khác
    const dt = new Date(raw)
    if (Number.isNaN(dt.getTime())) return fallback
    return `${pad(dt.getDate())}/${pad(dt.getMonth() + 1)}/${dt.getFullYear()}`
}

/**
 * Alias tương thích ngược cho fmtDate
 */
export const fmtDate = formatDateOnly

/**
 * Định dạng Date & Time: "DD/MM/YYYY · HH:mm"
 * Hiển thị theo múi giờ client
 */
export function formatDateTime(iso?: string | null, fallback = '—'): string {
    if (!iso) return fallback
    const d = new Date(iso)
    if (Number.isNaN(d.getTime())) return fallback
    return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} · ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

/**
 * Định dạng thời gian y tế / tem in: "HH:mm DD/MM/YYYY" (ví dụ: "08:30 18/11/2026").
 * Hiển thị theo múi giờ client trình duyệt / máy in (local getters).
 */
export function formatDateTimeMedical(iso?: string | null, fallback = '—'): string {
    if (!iso) return fallback
    const d = new Date(iso)
    if (Number.isNaN(d.getTime())) return fallback
    return `${pad(d.getHours())}:${pad(d.getMinutes())} ${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`
}

/**
 * Alias tương thích ngược cho fmtDateTime (chuẩn y tế / in tem: "HH:mm DD/MM/YYYY")
 */
export const fmtDateTime = formatDateTimeMedical

/**
 * Định dạng Date & Time có giây: "DD/MM/YYYY · HH:mm:ss"
 */
export function formatDateTimeSec(iso?: string | null, fallback = '—'): string {
    if (!iso) return fallback
    const d = new Date(iso)
    if (Number.isNaN(d.getTime())) return fallback
    return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} · ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}

/**
 * Định dạng giờ phút: "HH:mm"
 */
export function formatTime(iso?: string | null, fallback = '—'): string {
    if (!iso) return fallback
    const d = new Date(iso)
    if (Number.isNaN(d.getTime())) return fallback
    return `${pad(d.getHours())}:${pad(d.getMinutes())}`
}

/**
 * Alias tương thích ngược cho fmtTime
 */
export const fmtTime = formatTime

/**
 * Cộng trừ ngày cho chuỗi Date-Only: "YYYY-MM-DD" + days -> "YYYY-MM-DD"
 */
export function addDaysDateOnly(isoDateOnly: string, days: number): string {
    const parts = isoDateOnly.slice(0, 10).split('-').map(Number)
    if (parts.length < 3 || parts.some(Number.isNaN)) return isoDateOnly
    const [y, m, d] = parts
    const t = new Date(y, m - 1, d + days)
    return `${t.getFullYear()}-${pad(t.getMonth() + 1)}-${pad(t.getDate())}`
}

/**
 * Alias tương thích ngược cho addDaysIso
 */
export const addDaysIso = addDaysDateOnly

/**
 * Cộng trừ ngày cho chuỗi ISO đầy đủ (dùng cho hạn sử dụng túi máu)
 */
export function addDaysUtcIso(isoFull: string, days: number): string {
    const t = new Date(isoFull).getTime()
    if (Number.isNaN(t)) return isoFull
    return new Date(t + days * 86_400_000).toISOString()
}
