import { useEffect, useMemo } from 'react'
import { useAuthStore } from '@/features/auth/authStore'
import { ROLE_META, resolveRole } from '@/config/navigation'
import { authApi } from '@/features/auth/authApi'
import type { AuthUser } from "@/types/auth"

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
 * Lấy thông tin user hiện tại từ backend (GET /api/Auth/me)
 * và đồng bộ vào authStore.
 *
 * Gọi hook tại một vị trí cố định, ví dụ Header.
 * Chỉ fetch khi đã có accessToken nhưng chưa có fullName.
 */
export function useCurrentUser() {
    const user = useAuthStore((s) => s.user)
    const accessToken = useAuthStore((s) => s.accessToken)
    const setUser = useAuthStore((s) => s.setUser)

    const hasName = Boolean(user?.fullName?.trim())

    useEffect(() => {
        if (!accessToken || hasName) return

        let cancelled = false

        authApi
            .me()
            .then((currentUser: AuthUser) => {
                if (!cancelled) {
                    setUser(currentUser)
                }
            })
            .catch(() => {
                // Lỗi 401 được axiosClient xử lý.
                // Các lỗi khác không làm ứng dụng bị crash.
            })

        return () => {
            cancelled = true
        }
    }, [accessToken, hasName, setUser])

    return useMemo(() => {
        const role = resolveRole(user?.roles)
        const meta = role ? ROLE_META[role] : undefined
        const fullName =
            user?.fullName?.trim() || user?.username || ''

        return {
            user,
            role,
            fullName,
            displayName:
                fullName && meta
                    ? `${meta.prefix}. ${fullName}`
                    : fullName,
            roleLabel: meta?.label ?? '',
            initials: fullName ? getInitials(fullName) : '',
            loading: Boolean(accessToken) && !fullName,
        }
    }, [user, accessToken])
}