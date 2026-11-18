/**
 * Lấy chữ cái viết tắt chuẩn cho người dùng Việt Nam:
 * - Rỗng: '?'
 * - 1 từ: "Mai" -> "MA"
 * - 2 từ trở lên: chữ cái đầu của từ đầu + từ cuối ("Lê Quốc Anh" -> "LA", "Nguyễn Văn" -> "NV")
 */
export function getInitials(name?: string | null): string {
    if (!name || !name.trim()) return '?'
    const words = name.trim().split(/\s+/).filter(Boolean)
    if (words.length === 0) return '?'
    if (words.length === 1) return words[0].slice(0, 2).toLocaleUpperCase('vi')
    return (words[0][0] + words[words.length - 1][0]).toLocaleUpperCase('vi')
}

/**
 * Lấy chữ cái viết tắt từ 2 từ cuối của họ tên (tên đệm + tên chính):
 * Ví dụ: "Nguyễn Văn An" -> "VA", "Mai" -> "M"
 * Dùng cho avatar bubble trong các bảng danh sách và hàng chờ.
 */
export const getAvatarInitials = (name?: string | null): string => {
    if (!name || !name.trim()) return ''
    return name.trim().split(/\s+/).slice(-2).map((w) => w[0]?.toUpperCase() ?? '').join('')
}

export const initials = getAvatarInitials

