import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Dropdown } from 'antd'
import type { MenuProps } from 'antd'
import {
    BellOutlined,
    DownOutlined,
    LinkOutlined,
    LogoutOutlined,
    MoonOutlined,
    ScanOutlined,
    SearchOutlined,
    SunOutlined,
    UserOutlined,
} from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/features/auth/authStore'
import { useCurrentUser } from '@/features/auth/useCurrentUser'
import { getNavForRoles } from '@/config/navigation'
import './Header.css'

interface HeaderProps {
    /** Gọi khi nhấn Enter ở ô tìm kiếm */
    onSearch?: (keyword: string) => void
    /** Gọi khi bấm nút quét mã vạch */
    onScan?: () => void
}

type Theme = 'light' | 'dark'

const THEME_KEY = 'theme'

export default function Header({ onSearch, onScan }: HeaderProps) {
    const navigate = useNavigate()
    const logout = useAuthStore((s) => s.logout)
    const { displayName, roleLabel, initials, loading, user } = useCurrentUser()

    const homePath = getNavForRoles(user?.roles)?.home.path ?? '/'

    /* ---------------- Theme ---------------- */
    const [theme, setTheme] = useState<Theme>(() =>
        localStorage.getItem(THEME_KEY) === 'dark' ? 'dark' : 'light'
    )

    useEffect(() => {
        document.documentElement.dataset.theme = theme
        localStorage.setItem(THEME_KEY, theme)
    }, [theme])

    /* ---------------- Search + Ctrl/Cmd+K ---------------- */
    const searchRef = useRef<HTMLInputElement>(null)
    const [keyword, setKeyword] = useState('')

    useEffect(() => {
        const onKeyDown = (e: KeyboardEvent) => {
            if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
                e.preventDefault()
                searchRef.current?.focus()
            }
        }
        window.addEventListener('keydown', onKeyDown)
        return () => window.removeEventListener('keydown', onKeyDown)
    }, [])

    const handleSearch = (e: FormEvent) => {
        e.preventDefault()
        const q = keyword.trim()
        if (q) onSearch?.(q)
    }

    /* ---------------- Trạng thái tích hợp & thông báo ---------------- */
    // TODO: thay bằng dữ liệu thật (API health-check / API đếm thông báo chưa đọc)
    const hisLisOnline = true
    const unreadCount = 0

    /* ---------------- User menu ---------------- */
    const handleLogout = () => {
        logout()
        navigate('/login', { replace: true })
    }

    const menuItems: MenuProps['items'] = [
        {
            key: 'profile',
            icon: <UserOutlined />,
            label: 'Hồ sơ cá nhân',
            onClick: () => navigate('/account/profile'),
        },
        {
            key: 'notifications',
            icon: <BellOutlined />,
            label: 'Trung tâm thông báo',
            onClick: () => navigate('/account/notifications'),
        },
        { type: 'divider' },
        {
            key: 'logout',
            icon: <LogoutOutlined />,
            label: 'Đăng xuất',
            danger: true,
            onClick: handleLogout,
        },
    ]

    return (
        <header className="app-header">
            {/* Brand */}
            <button
                type="button"
                className="app-header__brand"
                onClick={() => navigate(homePath)}
                aria-label="Về trang chủ"
            >
                <span className="app-header__logo" aria-hidden>
                    <svg viewBox="0 0 24 24" width="20" height="20" fill="currentColor">
                        <path d="M12 2.5c-.4 0-.8.2-1 .5C8.2 6.7 5.5 10 5.5 13.8a6.5 6.5 0 0 0 13 0c0-3.8-2.7-7.1-5.5-10.8-.2-.3-.6-.5-1-.5z" />
                    </svg>
                </span>
                <span className="app-header__brand-text">Điều Phối Máu 175</span>
            </button>

            {/* Search */}
            <form className="app-header__search" role="search" onSubmit={handleSearch}>
                <SearchOutlined className="app-header__search-icon" />
                <input
                    ref={searchRef}
                    type="search"
                    value={keyword}
                    onChange={(e) => setKeyword(e.target.value)}
                    placeholder="Tìm mã túi máu, phiếu yêu cầu, người hiến…"
                    aria-label="Tìm kiếm"
                />
            </form>

            {/* Actions */}
            <div className="app-header__actions">
                <button type="button" className="app-header__icon-btn" onClick={onScan} aria-label="Quét mã vạch">
                    <ScanOutlined />
                </button>

                <span
                    className={`app-header__pill ${hisLisOnline ? 'is-online' : 'is-offline'}`}
                    title={hisLisOnline ? 'Kết nối HIS/LIS ổn định' : 'Mất kết nối HIS/LIS'}
                >
                    <span className="app-header__pill-dot" aria-hidden />
                    <LinkOutlined />
                    <span>HIS/LIS</span>
                </span>

                <button
                    type="button"
                    className="app-header__icon-btn"
                    onClick={() => setTheme((t) => (t === 'dark' ? 'light' : 'dark'))}
                    aria-label={theme === 'dark' ? 'Chuyển sang giao diện sáng' : 'Chuyển sang giao diện tối'}
                >
                    {theme === 'dark' ? <SunOutlined /> : <MoonOutlined />}
                </button>

                <button
                    type="button"
                    className="app-header__icon-btn app-header__bell"
                    onClick={() => navigate('/account/notifications')}
                    aria-label={unreadCount > 0 ? `Thông báo (${unreadCount} chưa đọc)` : 'Thông báo'}
                >
                    <BellOutlined />
                    {unreadCount > 0 && <span className="app-header__bell-dot" aria-hidden />}
                </button>

                <span className="app-header__divider" aria-hidden />

                <Dropdown menu={{ items: menuItems }} trigger={['click']} placement="bottomRight">
                    <button type="button" className="app-header__user" aria-label="Menu tài khoản">
                        <span className="app-header__avatar" aria-hidden>
                            {initials || <UserOutlined />}
                        </span>
                        <span className="app-header__user-text">
                            {loading ? (
                                <>
                                    <span className="app-header__skeleton" style={{ width: 120 }} />
                                    <span className="app-header__skeleton" style={{ width: 80 }} />
                                </>
                            ) : (
                                <>
                                    <span className="app-header__user-name">{displayName || 'Người dùng'}</span>
                                    <span className="app-header__user-role">{roleLabel}</span>
                                </>
                            )}
                        </span>
                        <DownOutlined className="app-header__user-chevron" />
                    </button>
                </Dropdown>
            </div>
        </header>
    )
}
