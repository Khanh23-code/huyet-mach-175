import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { message } from 'antd'
import { authApi } from '@/features/auth/authApi'
import type { ChangePasswordRequest, UserProfile } from '@/types/auth'
import {
    MOCK_PROFILE_EXTRA,
    USE_MOCK_CHANGE_PASSWORD,
    USE_MOCK_PROFILE,
    USE_MOCK_SESSIONS,
    mockChangePassword,
    mockGetSessions,
    mockRevokeSession,
} from './mock'

const KEYS = {
    profile: ['auth', 'me'] as const,
    sessions: ['auth', 'sessions'] as const,
}

export function useProfile() {
    return useQuery<UserProfile>({
        queryKey: KEYS.profile,
        queryFn: async () => {
            const me = await authApi.me()
            return USE_MOCK_PROFILE ? { ...MOCK_PROFILE_EXTRA, ...me } : me
        },
    })
}

export function useSessions() {
    return useQuery({
        queryKey: KEYS.sessions,
        queryFn: USE_MOCK_SESSIONS ? mockGetSessions : authApi.getSessions,
    })
}

export function useRevokeSession() {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: (id: string) => (USE_MOCK_SESSIONS ? mockRevokeSession(id) : authApi.revokeSession(id)),
        onSuccess: () => {
            message.success('Đã đăng xuất khỏi thiết bị')
            qc.invalidateQueries({ queryKey: KEYS.sessions })
        },
        onError: () => message.error('Không thể đăng xuất thiết bị này. Vui lòng thử lại'),
    })
}

export function useChangePassword() {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: (body: ChangePasswordRequest) =>
            USE_MOCK_CHANGE_PASSWORD ? mockChangePassword() : authApi.changePassword(body),
        onSuccess: () => {
            message.success('Đã cập nhật mật khẩu. Các thiết bị khác đã bị đăng xuất')
            qc.invalidateQueries({ queryKey: KEYS.sessions })
        },
    })
}
