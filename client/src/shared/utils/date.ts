/**
 * Đệm số bằng '0' phía trước
 */
export const pad = (n: number | string, len = 2): string => String(n).padStart(len, '0')

/**
 * Lấy ngày hiện tại dạng ISO 'YYYY-MM-DD' cố định theo múi giờ Việt Nam ('Asia/Ho_Chi_Minh' / UTC+7).
 * Giải quyết triệt để lỗi sai lệch 1 ngày khi dùng toISOString() vào khung giờ 00:00 - 06:59 sáng.
 */
export const getTodayIso = (): string => {
    try {
        const formatter = new Intl.DateTimeFormat('en-CA', {
            timeZone: 'Asia/Ho_Chi_Minh',
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
        })
        return formatter.format(new Date()) // Format 'en-CA' luôn là YYYY-MM-DD
    } catch {
        const d = new Date()
        return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
    }
}

/**
 * Alias cho getTodayIso: lấy ngày hiện tại định dạng 'YYYY-MM-DD' theo múi giờ Việt Nam.
 */
export const todayIso = getTodayIso

