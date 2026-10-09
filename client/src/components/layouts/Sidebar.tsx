import { Layout, Menu } from 'antd'
import {
    DashboardOutlined,
    HeartOutlined,
    DatabaseOutlined,
    FileTextOutlined,
    AuditOutlined,
} from '@ant-design/icons'
import type { ReactNode } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/features/auth/authStore'

interface MenuItem {
    key: string 
    label: string
    icon: ReactNode
    roles?: string[] 
}

const MENU_ITEMS: MenuItem[] = [
    { key: '/', label: 'Tổng quan', icon: <DashboardOutlined /> },
    { key: '/donation', label: 'Tiếp nhận hiến máu', icon: <HeartOutlined />, roles: ['BTD', 'AD'] },
    { key: '/inventory', label: 'Kho chế phẩm', icon: <DatabaseOutlined />, roles: ['BBNK', 'AD'] },
    { key: '/clinical', label: 'Yêu cầu máu', icon: <FileTextOutlined />, roles: ['LS', 'QL', 'AD'] },
    { key: '/audit', label: 'Kiểm toán', icon: <AuditOutlined />, roles: ['QL', 'AD'] },
]

export default function Sidebar() {
    const navigate = useNavigate()
    const { pathname } = useLocation()
    const roles = useAuthStore((s) => s.user?.roles ?? [])

    const items = MENU_ITEMS.filter(
        (m) => !m.roles || m.roles.some((r) => roles.includes(r))
    ).map(({ key, label, icon }) => ({ key, label, icon }))

    return (
        <Layout.Sider breakpoint="lg" collapsedWidth={64} theme="light" width={240}>
            <div style={{ padding: 16, fontWeight: 700, color: '#1b7a3a' }}>Huyết Mạch 175</div>
            <Menu
                mode="inline"
                selectedKeys={[pathname]}
                items={items}
                onClick={({ key }) => navigate(key)}
            />
        </Layout.Sider>
    )
}
//TODO: cần sửa lại theo bản des