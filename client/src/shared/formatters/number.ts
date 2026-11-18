/**
 * Định dạng số theo chuẩn Việt Nam (vd: 1.000.000)
 */
export function formatNum(n?: number | null, fallback = '0'): string {
    if (n === null || n === undefined) return fallback
    return n.toLocaleString('vi-VN')
}

/**
 * Định dạng thời gian còn lại (giờ -> ngày/giờ)
 */
export function formatRemainingHours(hours: number): string {
    if (hours <= 0) return 'Đã hết hạn'
    if (hours < 24) return `${Math.floor(hours)} giờ ${Math.round((hours % 1) * 60)} phút`
    return `${Math.floor(hours / 24)} ngày ${Math.floor(hours % 24)} giờ`
}
