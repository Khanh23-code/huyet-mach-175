import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuthStore } from '../authStore'

interface Props { allowedRoles?: string[] }

export default function ProtectedRoute({ allowedRoles }: Props) {
    const { accessToken, user } = useAuthStore()
    const location = useLocation()

    if (!accessToken) return <Navigate to="/login" state={{ from: location }} replace />

    if (allowedRoles && !user?.roles.some((r: string) => allowedRoles.includes(r))) {
        return <Navigate to="/403" replace />
    }
    return <Outlet />
}