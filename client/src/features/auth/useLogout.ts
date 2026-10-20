import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { authApi } from './authApi'
import { useAuthStore } from './authStore'

/**
 * Đăng xuất:
 *  1. Gọi POST /api/Auth/logout để BE thu hồi refresh token.
 *  2. DÙ API thành công hay lỗi (mạng, access token hết hạn...) vẫn xóa phiên ở FE,
 *     để người dùng không bị kẹt ở trạng thái "bấm đăng xuất mà không ra".
 *  3. Xóa toàn bộ cache TanStack Query để người đăng nhập sau trên cùng trình duyệt
 *     không thấy dữ liệu của người trước.
 */
export function useLogout() {
    const navigate = useNavigate()
    const queryClient = useQueryClient()
    const clearAuth = useAuthStore((s) => s.logout)
    const [isLoggingOut, setIsLoggingOut] = useState(false)

    const logout = async () => {
        if (isLoggingOut) return
        setIsLoggingOut(true)

        const { refreshToken } = useAuthStore.getState()

        try {
            if (refreshToken) await authApi.logout(refreshToken)
        } catch {
            // Bỏ qua: vẫn đăng xuất ở phía FE.
        } finally {
            clearAuth()
            queryClient.clear()
            navigate('/login', { replace: true })
        }
    }

    return { logout, isLoggingOut }
}