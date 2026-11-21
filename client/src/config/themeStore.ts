import { create } from 'zustand'

export type ThemeMode = 'light' | 'dark'

export const THEME_STORAGE_KEY = 'theme'

function readInitialTheme(): ThemeMode {
    if (typeof window === 'undefined') return 'light'
    return window.localStorage.getItem(THEME_STORAGE_KEY) === 'dark' ? 'dark' : 'light'
}

function syncThemeToDom(mode: ThemeMode) {
    if (typeof document !== 'undefined') {
        document.documentElement.dataset.theme = mode
    }
    if (typeof window !== 'undefined') {
        window.localStorage.setItem(THEME_STORAGE_KEY, mode)
    }
}

const initialMode = readInitialTheme()
syncThemeToDom(initialMode)

interface ThemeState {
    mode: ThemeMode
    setMode: (mode: ThemeMode) => void
    toggleMode: () => void
}

export const useThemeStore = create<ThemeState>((set) => ({
    mode: initialMode,
    setMode: (mode) => {
        syncThemeToDom(mode)
        set({ mode })
    },
    toggleMode: () =>
        set((state) => {
            const next: ThemeMode = state.mode === 'dark' ? 'light' : 'dark'
            syncThemeToDom(next)
            return { mode: next }
        }),
}))
