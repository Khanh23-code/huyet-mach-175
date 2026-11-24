import { Skeleton } from 'antd'
import { SectionCard, StatusPill } from '@/shared/components'
import { useCurrentUser } from '@/features/auth/useCurrentUser'
import { getInitials } from '@/shared/utils/string'
import { useRoles } from '@/features/users/hooks'
import type { UserProfile } from '@/types/auth'
import { formatDate } from '../utils/device'

interface Props {
    profile?: UserProfile
    loading: boolean
}

export default function ProfileCard({ profile, loading }: Props) {
    const { displayName } = useCurrentUser()
    const rolesQuery = useRoles()

    if (loading || !profile) {
        return (
            <SectionCard>
                <div className="pf-loading">
                    <Skeleton avatar active paragraph={{ rows: 6 }} />
                </div>
            </SectionCard>
        )
    }

    const name = profile.fullName?.trim() || profile.username
    const isActive = profile.isActive !== false

    // Mọi vai trò của tài khoản (mã lấy từ /me, tên lấy từ danh sách vai trò; chưa có tên thì hiện mã)
    const roles = (profile.roles ?? []).map((code) => ({
        code,
        name: rolesQuery.data?.find((r) => r.code === code)?.name,
    }))
    const roleSummary = roles.map((r) => r.name ?? r.code).join(', ')

    const rows: { label: string; value: React.ReactNode }[] = [
        { label: 'Mã nhân viên', value: profile.staffCode ?? '—' },
        { label: 'Khoa / Phòng', value: profile.departmentName ?? '—' },
        {
            label: 'Vai trò',
            value: roles.length ? (
                <div className="pf-roles">
                    {roles.map((r) => (
                        <StatusPill tone="info" key={r.code}>
                            {r.name ? `${r.name} (${r.code})` : r.code}
                        </StatusPill>
                    ))}
                </div>
            ) : '—',
        },
        { label: 'Email nội bộ', value: profile.email || '—' },
        { label: 'Số điện thoại', value: profile.phoneNumber || '—' },
        { label: 'Ngày vào làm', value: formatDate(profile.hireDate) },
    ]

    return (
        <SectionCard noPadding>
            <header className="pf-id">
                <div className="pf-id__avatar" aria-hidden>{getInitials(name)}</div>
                <div className="pf-id__body">
                    <StatusPill tone={isActive ? 'success' : 'danger'}>
                        {isActive ? 'Hoạt động' : 'Đã khóa'}
                    </StatusPill>
                    <h2>{displayName || name}</h2>
                    <p>{[roleSummary, profile.departmentName].filter(Boolean).join(' - ')}</p>
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
        </SectionCard>
    )
}