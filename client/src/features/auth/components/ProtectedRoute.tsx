import { Navigate, Outlet, useLocation } from 'react-router-dom'
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
 * Việc chờ GET /me đã do <AuthGate /> đảm nhiệm, nên route này phải nằm BÊN TRONG AuthGate.
 */
export default function ProtectedRoute({ requiredPermission }: Props) {
    const accessToken = useAuthStore((s) => s.accessToken)
    const { canAccess } = usePermission()
    const location = useLocation()

    if (!accessToken) return <Navigate to="/login" state={{ from: location }} replace />

    if (requiredPermission !== undefined && !canAccess(requiredPermission)) {
        return <Navigate to="/403-forbidden" replace />
    }

    return <Outlet />
}