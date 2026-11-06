import { useMemo } from 'react'
import { useAuthStore } from '@/features/auth/authStore'

/** "Lê Quốc Anh" -> "LA", "Mai" -> "MA" */
export function getInitials(name: string): string {
    const words = name.trim().split(/\s+/).filter(Boolean)

    if (words.length === 0) return '?'

    const first = Array.from(words[0])

    if (words.length === 1) {
        return first.slice(0, 2).join('').toLocaleUpperCase('vi')
    }

    const last = Array.from(words[words.length - 1])

    return (first[0] + last[0]).toLocaleUpperCase('vi')
}

/**
 * Đọc thông tin user hiện tại từ authStore.
 * Việc gọi GET /api/Auth/me đã chuyển sang <AuthGate /> (chạy 1 lần khi mở app).
 */
export function useCurrentUser() {
    const user = useAuthStore((s) => s.user)
    const accessToken = useAuthStore((s) => s.accessToken)

    return useMemo(() => {
        const fullName = user?.fullName?.trim() || user?.username || ''

        return {
            user,
            fullName,
            displayName: fullName,
            // Hiển thị mã các vai trò của user, không dùng để quyết định quyền
            roleLabel: user?.roles?.join(' · ') ?? '',
            initials: fullName ? getInitials(fullName) : '',
            loading: Boolean(accessToken) && !fullName,
        }
    }, [user, accessToken])
}