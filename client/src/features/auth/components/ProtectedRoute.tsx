import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { Spin } from 'antd'
import { useAuthStore } from '../authStore'
import { usePermission } from '../usePermission'
import type { PermissionRequirement } from '@/config/permissions'

interface Props {
    /** Cần ÍT NHẤT 1 trong các quyền này. Bỏ trống = chỉ cần đã đăng nhập. */
    requiredPermission?: PermissionRequirement
}

/**
 * - Chưa đăng nhập                -> /login
 * - Đã đăng nhập nhưng thiếu quyền -> /403-forbidden
 *
 * Route có `requiredPermission` chỉ được quyết định sau khi GET /me đã trả về
 * (useCurrentUser ở Header trong AppLayout). Vì vậy route này phải nằm BÊN TRONG AppLayout.
 */
export default function ProtectedRoute({ requiredPermission }: Props) {
    const accessToken = useAuthStore((s) => s.accessToken)
    const permissionsSynced = useAuthStore((s) => s.permissionsSynced)
    const { canAccess } = usePermission()
    const location = useLocation()

    if (!accessToken) return <Navigate to="/login" state={{ from: location }} replace />

    if (requiredPermission !== undefined) {
        if (!permissionsSynced) {
            return (
                <div style={{ display: 'grid', placeItems: 'center', minHeight: 240 }}>
                    <Spin />
                </div>
            )
        }
        if (!canAccess(requiredPermission)) {
            return <Navigate to="/403-forbidden" replace />
        }
    }

    return <Outlet />
}