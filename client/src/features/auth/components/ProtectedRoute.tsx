import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuthStore } from '../authStore'
import { useCan } from '../usePermission'

interface Props {
    allowedRoles?: string[]
    /** Cần ÍT NHẤT 1 trong các permission code này (SYS luôn qua). Chỉ dùng khi BE đã trả `permissions`. */
    requiredPermission?: string | string[]
}

export default function ProtectedRoute({ allowedRoles, requiredPermission }: Props) {
    const { accessToken, user } = useAuthStore()
    const can = useCan()
    const location = useLocation()

    if (!accessToken) return <Navigate to="/login" state={{ from: location }} replace />

    if (allowedRoles && !user?.roles.some((r: string) => allowedRoles.includes(r))) {
        return <Navigate to="/403" replace />
    }

    if (requiredPermission && !can(requiredPermission)) {
        return <Navigate to="/403" replace />
    }

    return <Outlet />
}