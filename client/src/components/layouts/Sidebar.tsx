import { useEffect, useMemo, useState } from 'react'
import { Layout } from 'antd'
import { AppstoreOutlined, RightOutlined } from '@ant-design/icons'
import { NavLink, useLocation } from 'react-router-dom'
import { useAuthStore } from '@/features/auth/authStore'
import { getNavForRoles } from '@/config/navigation'
import './Sidebar.css'

const isPathActive = (pathname: string, path: string) =>
    pathname === path || pathname.startsWith(`${path}/`)

const cx = (...parts: Array<string | false | undefined>) => parts.filter(Boolean).join(' ')

export default function Sidebar() {
    const { pathname } = useLocation()
    const roles = useAuthStore((s) => s.user?.roles)

    const nav = useMemo(() => getNavForRoles(roles), [roles])
    const role = nav?.role

    // Nhóm đang mở. Mặc định mở các nhóm nghiệp vụ của role (trừ "Tài khoản").
    const [openKeys, setOpenKeys] = useState<string[]>([])

    useEffect(() => {
        if (!nav) return
        setOpenKeys(nav.groups.filter((g) => g.key !== 'account').map((g) => g.key))
        // chỉ chạy lại khi đổi role
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [role])

    // Điều hướng thẳng tới 1 trang con (vd. từ chuông thông báo) -> tự mở nhóm chứa nó.
    const activeGroupKey = nav?.groups.find((g) =>
        g.children.some((c) => isPathActive(pathname, c.path))
    )?.key

    useEffect(() => {
        if (!activeGroupKey) return
        setOpenKeys((prev) => (prev.includes(activeGroupKey) ? prev : [...prev, activeGroupKey]))
    }, [activeGroupKey])

    const toggle = (key: string) =>
        setOpenKeys((prev) => (prev.includes(key) ? prev.filter((k) => k !== key) : [...prev, key]))

    return (
        <Layout.Sider
            width={240}
            theme="light"
            style={{
                position: 'sticky',
                top: 'var(--header-h)',
                height: 'calc(100vh - var(--header-h))',
                background: 'var(--c-surface)',
                overflow: 'hidden',
            }}
        >
            <div className="sider">
                <nav className="sider__nav" aria-label="Điều hướng chính">
                    <div className="sider__section">Không gian làm việc</div>

                    {nav && (
                        <NavLink
                            to={nav.home.path}
                            end
                            className={({ isActive }) => cx('sider__home', isActive && 'is-active')}
                        >
                            <AppstoreOutlined className="sider__home-icon" />
                            <span>{nav.home.label}</span>
                        </NavLink>
                    )}

                    {nav?.groups.map((group) => {
                        const open = openKeys.includes(group.key)
                        const panelId = `sider-group-${group.key}`

                        return (
                            <div className="sider__group" key={group.key}>
                                <button
                                    type="button"
                                    className="sider__group-btn"
                                    aria-expanded={open}
                                    aria-controls={panelId}
                                    onClick={() => toggle(group.key)}
                                >
                                    <span className="sider__code">{group.code}</span>
                                    <span className="sider__group-icon">{group.icon}</span>
                                    <span className="sider__group-label">{group.label}</span>
                                    <RightOutlined className={cx('sider__chevron', open && 'is-open')} />
                                </button>

                                {open && (
                                    <ul className="sider__children" id={panelId}>
                                        {group.children.map((child) => (
                                            <li key={child.key}>
                                                <NavLink
                                                    to={child.path}
                                                    className={({ isActive }) =>
                                                        cx('sider__child', isActive && 'is-active')
                                                    }
                                                >
                                                    {child.label}
                                                </NavLink>
                                            </li>
                                        ))}
                                    </ul>
                                )}
                            </div>
                        )
                    })}
                </nav>

                {/* TODO: nối với API health-check để đổi trạng thái / thời gian cập nhật */}
                <div className="sider__status" role="status">
                    <span className="sider__status-dot" aria-hidden />
                    <div>
                        <div className="sider__status-title">Hệ thống ổn định</div>
                        <div className="sider__status-sub">Cập nhật 1 phút trước</div>
                    </div>
                </div>
            </div>
        </Layout.Sider>
    )
}
