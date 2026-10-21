import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { message } from 'antd'
import { useAuthStore } from '@/features/auth/authStore'

export const axiosClient = axios.create({
    baseURL: 'http://localhost:5208',
    headers: {
        'Content-Type': 'application/json',
    },
})

// ---- Request: gắn token ----
axiosClient.interceptors.request.use((config) => {
    const token = useAuthStore.getState().accessToken
    if (token) config.headers.Authorization = `Bearer ${token}`
    return config
})

// ---- Response ----
let refreshing: Promise<string> | null = null

const forceLogout = () => {
    useAuthStore.getState().logout()
    if (window.location.pathname !== '/login') window.location.href = '/login'
}

axiosClient.interceptors.response.use(
    (res) => res,
    async (error: AxiosError) => {
        const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean }
        const status = error.response?.status

        if (status === 401 && original && !original._retry) {
            const isAuthCall = original.url?.includes('/api/Auth/login') || original.url?.includes('/api/Auth/refresh-token')
            const { refreshToken } = useAuthStore.getState()

            // Login sai mật khẩu cũng trả 401 -> để LoginPage tự báo lỗi
            if (isAuthCall && original.url?.includes('/login')) return Promise.reject(error)

            if (!refreshToken || isAuthCall) {
                forceLogout()
                return Promise.reject(error)
            }

            original._retry = true
            try {
                // nhiều request 401 cùng lúc chỉ refresh 1 lần
                refreshing ??= axios
                    .post(`${import.meta.env.VITE_API_BASE_URL}/api/Auth/refresh-token`, { refreshToken })
                    .then((r) => {
                        const { accessToken, refreshToken: newRefresh } = r.data // sửa theo response thật
                        useAuthStore.getState().setTokens(accessToken, newRefresh ?? refreshToken)
                        return accessToken as string
                    })
                    .finally(() => { refreshing = null })

                const newToken = await refreshing
                original.headers.Authorization = `Bearer ${newToken}`
                return axiosClient(original)
            } catch {
                forceLogout()
                return Promise.reject(error)
            }
        }

        if (status === 403) message.error('Bạn không có quyền thực hiện thao tác này')

        return Promise.reject(error)
    }
)