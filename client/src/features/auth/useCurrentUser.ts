import { useEffect, useMemo } from 'react'
import { useAuthStore } from '@/features/auth/authStore'
import { authApi } from '@/features/auth/authApi'
import type { AuthUser } from '@/types/auth'

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
 * Lấy thông tin user hiện tại từ backend (GET /api/Auth/me) và đồng bộ vào authStore,
 * kể cả `permissions`. Sau khi xong (thành công hoặc lỗi) đánh dấu `permissionsSynced`
 * để ProtectedRoute biết đã có thể quyết định theo quyền.
 *
 * Gọi hook này ở một vị trí cố định bên trong AppLayout (hiện là Header).
 */
export function useCurrentUser() {
    const user = useAuthStore((s) => s.user)
    const accessToken = useAuthStore((s) => s.accessToken)
    const setUser = useAuthStore((s) => s.setUser)
    const markPermissionsSynced = useAuthStore((s) => s.markPermissionsSynced)

    useEffect(() => {
        if (!accessToken) return
        let cancelled = false

        authApi
            .me()
            .then((currentUser: AuthUser) => {
                if (!cancelled) {
                    setUser({ ...currentUser, permissions: currentUser.permissions ?? [] })
                }
            })
            .catch(() => {})
            .finally(() => {
                if (!cancelled) markPermissionsSynced()
            })

        return () => {
            cancelled = true
        }
    }, [accessToken, setUser, markPermissionsSynced])

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