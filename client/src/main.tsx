import React from 'react'
import ReactDOM from 'react-dom/client'
import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router-dom'
import { App as AntApp, ConfigProvider } from 'antd'
import { router } from './router'
import { queryClient } from './services/queryClient'
import { getAntdTheme } from './config/themeConfig'
import { useThemeStore } from './config/themeStore'
import './styles/variables.css'
import './styles/global.css'

export function RootApp() {
    const mode = useThemeStore((s) => s.mode)

    return (
        <ConfigProvider theme={getAntdTheme(mode)}>
            <AntApp>
                <QueryClientProvider client={queryClient}>
                    <RouterProvider router={router} />
                </QueryClientProvider>
            </AntApp>
        </ConfigProvider>
    )
}

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <RootApp />
    </React.StrictMode>,
)