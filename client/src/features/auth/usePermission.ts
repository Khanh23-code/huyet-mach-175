import { useCallback } from 'react'
import { useAuthStore } from './authStore'

const SUPER_ROLE = 'SYS'

/**
 * Kiểm tra quyền của user hiện tại.
 *
 *   const can = useCan()
 *   can('CAMPAIGN_APPROVE')                 // có quyền này?
 *   can(['DONORS_EDIT', 'DONORS_VIEW'])     // có ÍT NHẤT 1 trong các quyền?
 *
 * SYS luôn được qua (BE khóa không cho sửa quyền của SYS).
 * `permissions` do GET /me trả về; khi BE chưa trả thì coi như rỗng.
 */
export function useCan() {
    const roles = useAuthStore((s) => s.user?.roles)
    const permissions = useAuthStore((s) => s.user?.permissions)

    return useCallback(
        (code: string | string[]): boolean => {
            if (roles?.some((r) => r.trim().toUpperCase() === SUPER_ROLE)) return true

            const owned = new Set(permissions ?? [])
            const required = Array.isArray(code) ? code : [code]
            return required.some((c) => owned.has(c))
        },
        [roles, permissions],
    )
}