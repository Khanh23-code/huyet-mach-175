import { Typography } from 'antd'
import { useAuthStore } from '@/features/auth/authStore'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { PageContainer, PageHeader, SectionCard } from '@/shared/components'
import ReceptionDashboard from '@/features/dashboard/components/ReceptionDashboard'
import BbnkDashboard from '@/features/dashboard/components/BbnkDashboard'

export default function DashboardPage() {
    const user = useAuthStore((s) => s.user)
    const { can } = usePermission()

    // Dashboard tiếp đón & sàng lọc cho người có quyền xem lịch hẹn (DON, CLIN, MGT)
    if (can(PERMISSIONS.APPOINTMENTS_VIEW)) return <ReceptionDashboard />
    if (can(PERMISSIONS.INVENTORY_VIEW)) return <BbnkDashboard />

    return (
        <PageContainer>
            <PageHeader breadcrumb={[{ title: 'Dashboard' }]} title="Tổng quan" />
            <SectionCard>
                <Typography.Paragraph style={{ marginBottom: 0 }}>
                    Xin chào {user?.fullName ?? user?.username}. Đăng nhập thành công.
                </Typography.Paragraph>
            </SectionCard>
        </PageContainer>
    )
}