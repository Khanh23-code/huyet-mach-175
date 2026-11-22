import { PageContainer, PageHeader } from '@/shared/components'
import ProfileCard from '../components/ProfileCard'
import ActiveSessions from '../components/ActiveSessions'
import ChangePasswordFormCard from '../components/ChangePasswordForm'
import { useProfile } from '../hooks'
import './ProfilePage.css'

export default function ProfilePage() {
    const { data: profile, isLoading } = useProfile()

    return (
        <PageContainer>
            <PageHeader
                breadcrumb={[{ title: 'Tài khoản' }, { title: 'Hồ sơ cá nhân' }]}
                title="Hồ sơ cá nhân"
                subtitle="Xem thông tin cá nhân, đổi mật khẩu và quản lý phiên đăng nhập."
            />

            <div className="pf-grid">
                <div className="pf-col">
                    <ProfileCard profile={profile} loading={isLoading} />
                    <ActiveSessions />
                </div>
                <div className="pf-col">
                    <ChangePasswordFormCard />
                </div>
            </div>
        </PageContainer>
    )
}
