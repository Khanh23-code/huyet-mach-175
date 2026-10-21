import { Card, Typography } from 'antd'
import { useAuthStore } from '@/features/auth/authStore'

export default function DashboardPage() {
    const user = useAuthStore((s) => s.user)

    return (
        <Card>
            <Typography.Title level={4}>Tổng quan</Typography.Title>
            <Typography.Paragraph>
                Xin chào {user?.fullName ?? user?.username}. Đăng nhập thành công.
            </Typography.Paragraph>
        </Card>
    )
}