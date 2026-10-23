import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthUser } from '@/types/auth'

interface AuthState {
    accessToken: string | null
    refreshToken: string | null
    user: AuthUser | null
    permissionsSynced: boolean
    setTokens: (access: string, refresh: string) => void
    setUser: (user: AuthUser) => void
    markPermissionsSynced: () => void
    logout: () => void
}

export const useAuthStore = create<AuthState>()(
    persist(
        (set) => ({
            accessToken: null,
            refreshToken: null,
            user: null,
            permissionsSynced: false,
            setTokens: (accessToken, refreshToken) => set({ accessToken, refreshToken }),
            setUser: (user) => set({ user }),
            markPermissionsSynced: () => set({ permissionsSynced: true }),
            logout: () =>
                set({ accessToken: null, refreshToken: null, user: null, permissionsSynced: false }),
        }),
        {
            name: 'auth-storage',
            partialize: (s) => ({
                accessToken: s.accessToken,
                refreshToken: s.refreshToken,
                user: s.user,
            }),
        },
    ),
)