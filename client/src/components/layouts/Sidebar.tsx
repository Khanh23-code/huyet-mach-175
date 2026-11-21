import { useMemo, useState } from 'react'
import { Button, Drawer, Layout, Menu, Tooltip } from 'antd'
import type { MenuProps } from 'antd'
import { AppstoreOutlined, CloseOutlined } from '@ant-design/icons'
import { useLocation, useNavigate } from 'react-router-dom'
import { usePermission } from '@/features/auth/usePermission'
import { HOME_PATH, ROUTE_GROUPS, filterRouteGroups } from '@/config/routeConfig'
import './Sidebar.css'

export interface SidebarProps {
    collapsed?: boolean
    isMobile?: boolean
    mobileOpen?: boolean
    onCloseMobile?: () => void
}

const isPathActive = (pathname: string, path: string) =>
    pathname === path || pathname.startsWith(`${path}/`)

const cx = (...parts: Array<string | false | undefined>) => parts.filter(Boolean).join(' ')

interface SidebarContentProps {
    isCollapsed: boolean
    isMobile: boolean
    onCloseMobile?: () => void
}

function SidebarContent({ isCollapsed, isMobile, onCloseMobile }: SidebarContentProps) {
    const { pathname } = useLocation()
    const navigate = useNavigate()
    const { canAccess } = usePermission()

    const groups = useMemo(() => filterRouteGroups(ROUTE_GROUPS, canAccess), [canAccess])
    const [openOverrides, setOpenOverrides] = useState<Record<string, boolean>>({})
    const [collapsedOpenKeys, setCollapsedOpenKeys] = useState<string[]>([])

    // Tìm mục con khớp đường dẫn hiện tại (ưu tiên path dài nhất nếu lồng nhau)
    const activeChild = useMemo(() => {
        let bestMatch: { groupKey: string; path: string } | null = null
        for (const group of groups) {
            for (const child of group.children) {
                if (isPathActive(pathname, child.path)) {
                    if (!bestMatch || child.path.length > bestMatch.path.length) {
                        bestMatch = { groupKey: group.key, path: child.path }
                    }
                }
            }
        }
        return bestMatch
    }, [groups, pathname])

    const selectedKey = pathname === HOME_PATH ? HOME_PATH : (activeChild?.path ?? '')
    const activeGroupKey = activeChild?.groupKey

    const openKeys = useMemo(
        () =>
            groups
                .filter((g) =>
                    g.key in openOverrides
                        ? openOverrides[g.key]
                        : Boolean(g.defaultOpen || g.key === activeGroupKey),
                )
                .map((g) => g.key),
        [groups, openOverrides, activeGroupKey],
    )

    const handleOpenChange: MenuProps['onOpenChange'] = (nextOpenKeys) => {
        const nextSet = new Set(nextOpenKeys)
        setOpenOverrides(() => {
            const next: Record<string, boolean> = {}
            for (const group of groups) {
                next[group.key] = nextSet.has(group.key)
            }
            return next
        })
    }

    const menuItems = useMemo<MenuProps['items']>(
        () => [
            {
                key: HOME_PATH,
                icon: <AppstoreOutlined />,
                label: 'Trang chủ',
            },
            ...groups.map((group) => ({
                key: group.key,
                icon: group.icon,
                label: group.label,
                children: group.children.map((child) => ({
                    key: child.path,
                    label: child.label,
                })),
            })),
        ],
        [groups],
    )

    const handleMenuClick: MenuProps['onClick'] = ({ key }) => {
        navigate(key)
        if (isCollapsed) {
            setCollapsedOpenKeys([])
        }
        if (isMobile) {
            onCloseMobile?.()
        }
    }

    const statusBadge = (
        <div
            className={cx('sider__status', isCollapsed && 'is-collapsed')}
            role="status"
            aria-label="Hệ thống ổn định · Cập nhật 1 phút trước"
        >
            <span className="sider__status-dot" aria-hidden />
            {!isCollapsed && (
                <div>
                    <div className="sider__status-title">Hệ thống ổn định</div>
                    <div className="sider__status-sub">Cập nhật 1 phút trước</div>
                </div>
            )}
        </div>
    )

    return (
        <div className={cx('sider', isCollapsed && 'sider--collapsed', isMobile && 'sider--mobile')}>
            {isMobile && (
                <div className="sider__mobile-header">
                    <div className="sider__mobile-brand">
                        <span className="sider__mobile-logo" aria-hidden>
                            <svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor">
                                <path d="M12 2.5c-.4 0-.8.2-1 .5C8.2 6.7 5.5 10 5.5 13.8a6.5 6.5 0 0 0 13 0c0-3.8-2.7-7.1-5.5-10.8-.2-.3-.6-.5-1-.5z" />
                            </svg>
                        </span>
                        <span className="sider__mobile-title">Điều Phối Máu 175</span>
                    </div>
                    <Button
                        type="text"
                        icon={<CloseOutlined />}
                        onClick={onCloseMobile}
                        aria-label="Đóng menu điều hướng"
                    />
                </div>
            )}

            <nav className="sider__nav" aria-label="Điều hướng chính">
                {!isCollapsed && <div className="sider__section">Không gian làm việc</div>}

                <Menu
                    className="sider__menu"
                    mode="inline"
                    inlineCollapsed={isCollapsed}
                    selectedKeys={selectedKey ? [selectedKey] : []}
                    openKeys={isCollapsed ? collapsedOpenKeys : openKeys}
                    onOpenChange={isCollapsed ? setCollapsedOpenKeys : handleOpenChange}
                    items={menuItems}
                    onClick={handleMenuClick}
                />
            </nav>

            {/* TODO: nối với API health-check để đổi trạng thái / thời gian cập nhật */}
            {isCollapsed ? (
                <Tooltip title="Hệ thống ổn định · Cập nhật 1 phút trước" placement="right">
                    {statusBadge}
                </Tooltip>
            ) : (
                statusBadge
            )}
        </div>
    )
}

/** Sidebar động: chỉ hiện các mục (và nhóm) mà user có quyền tương ứng. */
export default function DynamicSidebar({
    collapsed = false,
    isMobile = false,
    mobileOpen = false,
    onCloseMobile,
}: SidebarProps) {
    if (isMobile) {
        return (
            <Drawer
                placement="left"
                size={260}
                open={mobileOpen}
                onClose={onCloseMobile}
                closable={false}
                styles={{ body: { padding: 0 } }}
                className="app-layout__mobile-drawer"
            >
                <SidebarContent
                    isCollapsed={false}
                    isMobile
                    onCloseMobile={onCloseMobile}
                />
            </Drawer>
        )
    }

    return (
        <Layout.Sider
            collapsible
            collapsed={collapsed}
            trigger={null}
            width={260}
            collapsedWidth={76}
            theme="light"
            className="app-layout__sider"
            style={{
                position: 'sticky',
                top: 'var(--header-h)',
                height: 'calc(100vh - var(--header-h))',
                background: 'var(--c-surface)',
                overflow: 'hidden',
            }}
        >
            <SidebarContent isCollapsed={collapsed} isMobile={false} />
        </Layout.Sider>
    )
}