import { Card, Typography } from 'antd'
import { useAuthStore } from '@/features/auth/authStore'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import ReceptionDashboard from '@/features/dashboard/components/ReceptionDashboard'

export default function DashboardPage() {
    const user = useAuthStore((s) => s.user)
    const { can } = usePermission()

    // Dashboard tiếp đón & sàng lọc cho người có quyền xem lịch hẹn (DON, CLIN, MGT)
    if (can(PERMISSIONS.APPOINTMENTS_VIEW)) return <ReceptionDashboard />

    return (
        <Card>
            <Typography.Title level={4}>Tổng quan</Typography.Title>
            <Typography.Paragraph>
                Xin chào {user?.fullName ?? user?.username}. Đăng nhập thành công.
            </Typography.Paragraph>
        </Card>
    )
}