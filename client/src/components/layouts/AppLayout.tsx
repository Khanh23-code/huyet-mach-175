import { useCallback, useState, useSyncExternalStore } from 'react'
import { Layout, Spin } from 'antd'
import { Outlet, useNavigation } from 'react-router-dom'
import Header from './Header'
import Sidebar from './Sidebar'
import './AppLayout.css'

const { Content } = Layout

const SIDEBAR_COLLAPSED_STORAGE_KEY = 'ui.sidebar.collapsed'
const MOBILE_MEDIA_QUERY = '(max-width: 768px)'

function readInitialCollapsed(): boolean {
    if (typeof window === 'undefined') return false
    try {
        return window.localStorage.getItem(SIDEBAR_COLLAPSED_STORAGE_KEY) === 'true'
    } catch {
        return false
    }
}

function subscribeMobileBreakpoint(callback: () => void): () => void {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
        return () => {}
    }
    const mq = window.matchMedia(MOBILE_MEDIA_QUERY)
    mq.addEventListener('change', callback)
    return () => mq.removeEventListener('change', callback)
}

function getIsMobileSnapshot(): boolean {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return false
    return window.matchMedia(MOBILE_MEDIA_QUERY).matches
}

export default function AppLayout() {
    const navigation = useNavigation()
    const isMobile = useSyncExternalStore(
        subscribeMobileBreakpoint,
        getIsMobileSnapshot,
        () => false,
    )
    const [collapsed, setCollapsed] = useState<boolean>(readInitialCollapsed)
    const [mobileOpen, setMobileOpen] = useState<boolean>(false)

    const handleToggleSidebar = useCallback(() => {
        if (isMobile) {
            setMobileOpen((prev) => !prev)
            return
        }
        setCollapsed((prev) => {
            const next = !prev
            try {
                window.localStorage.setItem(SIDEBAR_COLLAPSED_STORAGE_KEY, String(next))
            } catch {
                // Bỏ qua nếu storage bị chặn
            }
            return next
        })
    }, [isMobile])

    const handleCloseMobile = useCallback(() => {
        setMobileOpen(false)
    }, [])

    return (
        <Layout className="app-layout">
            <Header
                sidebarCollapsed={!isMobile && collapsed}
                isMobile={isMobile}
                mobileSidebarOpen={mobileOpen}
                onToggleSidebar={handleToggleSidebar}
            />

            <Layout className="app-layout__body">
                <Sidebar
                    collapsed={collapsed}
                    isMobile={isMobile}
                    mobileOpen={mobileOpen}
                    onCloseMobile={handleCloseMobile}
                />

                <Content className="app-layout__content">
                    {/* delay 150ms: chuyển trang nhanh thì không nháy spinner */}
                    <Spin spinning={navigation.state === 'loading'} delay={150}>
                        <Outlet />
                    </Spin>
                </Content>
            </Layout>
        </Layout>
    )
}