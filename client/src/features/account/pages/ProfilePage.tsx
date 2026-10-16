import { Breadcrumb } from 'antd'
import ProfileCard from '../components/ProfileCard'
import ActiveSessions from '../components/ActiveSessions'
import ChangePasswordFormCard from '../components/ChangePasswordForm'
import { useProfile } from '../hooks'
import './ProfilePage.css'

export default function ProfilePage() {
    const { data: profile, isLoading } = useProfile()

    return (
        <div className="pf">
            <Breadcrumb items={[{ title: 'Tài khoản' }, { title: 'Hồ sơ cá nhân' }]} />

            <div className="pf-title">
                <h1>Hồ sơ cá nhân</h1>
                <p>Xem thông tin cá nhân, đổi mật khẩu và quản lý phiên đăng nhập.</p>
            </div>

            <div className="pf-grid">
                <div className="pf-col">
                    <ProfileCard profile={profile} loading={isLoading} />
                    <ActiveSessions />
                </div>
                <div className="pf-col">
                    <ChangePasswordFormCard />
                </div>
            </div>
        </div>
    )
}
