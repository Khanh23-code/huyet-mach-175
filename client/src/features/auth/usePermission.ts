import { useMemo } from 'react'
import { useAuthStore } from './authStore'
import type { PermissionRequirement } from '@/config/permissions'

export interface PermissionApi {
    /** Có đúng quyền này không. */
    can: (code: string) => boolean
    /** Có ÍT NHẤT 1 quyền trong danh sách. Danh sách rỗng -> false. */
    canAny: (codes: readonly string[]) => boolean
    /** Có TOÀN BỘ quyền trong danh sách. Danh sách rỗng -> true. */
    canAll: (codes: readonly string[]) => boolean
    /**
     * Dạng gộp cho cấu hình route / `<Can>`:
     * undefined hoặc [] -> true; string -> can; string[] -> canAny.
     */
    canAccess: (required?: PermissionRequirement) => boolean
}

/**
 * Kiểm tra quyền của user hiện tại, chỉ dựa trên mảng `permissions`
 * do BE trả về (GET /api/Auth/me). KHÔNG xét tên vai trò.
 */
export function usePermission(): PermissionApi {
    const permissions = useAuthStore((s) => s.user?.permissions)

    return useMemo<PermissionApi>(() => {
        const owned = new Set(permissions ?? [])

        const can = (code: string) => owned.has(code)
        const canAny = (codes: readonly string[]) => codes.some(can)
        const canAll = (codes: readonly string[]) => codes.every(can)
        const canAccess = (required?: PermissionRequirement) => {
            if (required === undefined) return true
            if (typeof required === 'string') return can(required)
            return required.length === 0 || canAny(required)
        }

        return { can, canAny, canAll, canAccess }
    }, [permissions])
}