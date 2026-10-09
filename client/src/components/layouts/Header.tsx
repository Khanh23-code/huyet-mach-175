import { Layout, Dropdown, Avatar, Space, Typography } from 'antd'
import { UserOutlined, LogoutOutlined, DownOutlined } from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/features/auth/authStore'

export default function Header() {
    const navigate = useNavigate()
    const { user, logout } = useAuthStore()

    const handleLogout = () => {
        logout()
        navigate('/login', { replace: true })
    }

    return (
        <Layout.Header
            style={{
                background: '#fff',
                padding: '0 24px',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                borderBottom: '1px solid #f0f0f0',
            }}
        >
            <Typography.Text strong>Điều Phối Máu 175</Typography.Text>

            <Dropdown
                menu={{
                    items: [
                        {
                            key: 'logout',
                            icon: <LogoutOutlined />,
                            label: 'Đăng xuất',
                            onClick: handleLogout,
                        },
                    ],
                }}
            >
                <Space style={{ cursor: 'pointer' }}>
                    <Avatar icon={<UserOutlined />} />
                    <span>{user?.fullName ?? user?.username ?? 'Người dùng'}</span>
                    {user?.roles?.length ? (
                        <Typography.Text type="secondary">({user.roles.join(', ')})</Typography.Text>
                    ) : null}
                    <DownOutlined />
                </Space>
            </Dropdown>
        </Layout.Header>
    )
}
//TODO: cần sửa lại theo bản des