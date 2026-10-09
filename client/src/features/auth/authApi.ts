import { axiosClient } from '@/services/axiosClient'
import type { AuthUser, LoginRequest, LoginResponse } from '@/types/auth'

export const authApi = {
    login: (body: LoginRequest) =>
        axiosClient.post<LoginResponse>('/api/Auth/login', body).then((r) => r.data),
    me: () => axiosClient.get<AuthUser>('/api/Auth/me').then((r) => r.data),
}