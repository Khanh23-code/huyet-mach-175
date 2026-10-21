import { axiosClient } from '@/services/axiosClient'
import type {
    AuthUser,
    ChangePasswordRequest,
    LoginRequest,
    LoginResponse,
    LoginSession,
    UpdateProfileRequest,
} from '@/types/auth'

export const authApi = {
    login: (body: LoginRequest) =>
        axiosClient.post<LoginResponse>('/api/Auth/login', body).then((r) => r.data),
    me: () => axiosClient.get<AuthUser>('/api/Auth/me').then((r) => r.data),

    // ---- Đã có ở backend ----
    updateMe: (body: UpdateProfileRequest) =>
        axiosClient.put<AuthUser>('/api/Auth/me', body).then((r) => r.data),
    logout: (refreshToken: string) =>
        axiosClient.post('/api/Auth/logout', { refreshToken }).then((r) => r.data),

    // ---- TODO backend: chưa có, đang dùng mock (features/account/mock.ts) ----
    changePassword: (body: ChangePasswordRequest) =>
        axiosClient.post('/api/Auth/change-password', body).then((r) => r.data),
    getSessions: () =>
        axiosClient.get<LoginSession[]>('/api/Auth/sessions').then((r) => r.data),
    revokeSession: (id: string) =>
        axiosClient.delete(`/api/Auth/sessions/${id}`).then((r) => r.data),
}
