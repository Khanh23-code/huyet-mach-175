import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthUser } from '@/types/auth'

interface AuthState {
    accessToken: string | null
    refreshToken: string | null
    user: AuthUser | null
    setTokens: (access: string, refresh: string) => void
    setUser: (user: AuthUser) => void
    logout: () => void
}

export const useAuthStore = create<AuthState>()(
    persist(
        (set) => ({
            accessToken: null,
            refreshToken: null,
            user: null,
            setTokens: (accessToken, refreshToken) => set({ accessToken, refreshToken }),
            setUser: (user) => set({ user }),
            logout: () => set({ accessToken: null, refreshToken: null, user: null }),
        }),
        { name: 'auth-storage' }
    )
)