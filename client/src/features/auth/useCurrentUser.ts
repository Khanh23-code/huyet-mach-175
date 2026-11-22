import { useMemo } from 'react'
import { useAuthStore } from '@/features/auth/authStore'
import { getInitials } from '@/shared/utils/string'

export { getInitials } from '@/shared/utils/string'

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