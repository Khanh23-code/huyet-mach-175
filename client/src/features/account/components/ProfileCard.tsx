import { Skeleton } from 'antd'
import { getInitials, useCurrentUser } from '@/features/auth/useCurrentUser'
import type { UserProfile } from '@/types/auth'
import { formatDate } from '../utils/device'

interface Props {
    profile?: UserProfile
    loading: boolean
}

export default function ProfileCard({ profile, loading }: Props) {
    const { displayName, role, roleLabel } = useCurrentUser()

    if (loading || !profile) {
        return (
            <section className="pf-card">
                <div className="pf-card__pad"><Skeleton avatar active paragraph={{ rows: 6 }} /></div>
            </section>
        )
    }

    const name = profile.fullName?.trim() || profile.username
    const isActive = profile.isActive !== false

    const rows: { label: string; value: React.ReactNode }[] = [
        { label: 'Mã nhân viên', value: profile.staffCode ?? '—' },
        { label: 'Khoa / Phòng', value: profile.departmentName ?? '—' },
        {
            label: 'Vai trò',
            value: role ? <span className="pf-pill"><i />{roleLabel} ({role})</span> : '—',
        },
        { label: 'Email nội bộ', value: profile.email || '—' },
        { label: 'Số điện thoại', value: profile.phoneNumber || '—' },
        { label: 'Ngày vào làm', value: formatDate(profile.hireDate) },
    ]

    return (
        <section className="pf-card">
            <header className="pf-id">
                <div className="pf-id__avatar" aria-hidden>{getInitials(name)}</div>
                <div className="pf-id__body">
                    <span className={`pf-status ${isActive ? 'pf-status--on' : 'pf-status--off'}`}>
                        <i />{isActive ? 'Hoạt động' : 'Đã khóa'}
                    </span>
                    <h2>{displayName || name}</h2>
                    <p>{[roleLabel, profile.departmentName].filter(Boolean).join(' · ')}</p>
                </div>
            </header>

            <dl className="pf-info">
                {rows.map((r) => (
                    <div className="pf-info__row" key={r.label}>
                        <dt>{r.label}</dt>
                        <dd>{r.value}</dd>
                    </div>
                ))}
            </dl>
        </section>
    )
}
