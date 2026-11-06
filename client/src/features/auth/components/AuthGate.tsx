import { useEffect } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { Spin } from 'antd'
import { useAuthStore } from '../authStore'
import { authApi } from '../authApi'

/**
 * Route gốc của khu vực đã đăng nhập.
 * - Không có token               -> /login
 * - Có token nhưng chưa xác thực -> gọi GET /me, hiện spinner toàn trang
 * - /me lỗi (401, timeout, backend chưa lên) -> logout, về /login ngay
 * - /me xong                     -> render layout + trang
 */
export default function AuthGate() {
    const accessToken = useAuthStore((s) => s.accessToken)
    const synced = useAuthStore((s) => s.permissionsSynced)
    const location = useLocation()

    useEffect(() => {
        if (!accessToken || synced) return
        let cancelled = false

        authApi
            .me()
            .then((u) => {
                if (cancelled) return
                const { setUser, markPermissionsSynced } = useAuthStore.getState()
                setUser({ ...u, permissions: u.permissions ?? [] })
                markPermissionsSynced()
            })
            .catch(() => {
                if (!cancelled) useAuthStore.getState().logout()
            })

        return () => {
            cancelled = true
        }
    }, [accessToken, synced])

    if (!accessToken) return <Navigate to="/login" state={{ from: location }} replace />

    if (!synced) {
        return (
            <div style={{ display: 'grid', placeItems: 'center', height: '100vh' }}>
                <Spin size="large" />
            </div>
        )
    }

    return <Outlet />
}