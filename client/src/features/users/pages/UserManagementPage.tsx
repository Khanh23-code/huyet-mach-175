import { useState } from 'react'
import { Button } from 'antd'
import { AppstoreOutlined, PlusOutlined, TeamOutlined } from '@ant-design/icons'
import { PageContainer, PageHeader } from '@/shared/components'
import UserStatsCards from '../components/UserStatsCards'
import UserTable from '../components/UserTable'
import PermissionMatrix from '../components/PermissionMatrix'
import CreateUserModal from '../components/CreateUserModal'
import Can from '@/features/auth/components/Can'
import { PERMISSIONS } from '@/config/permissions'
import type { RoleCode } from '@/types/user'
import './UserManagementPage.css'

type Tab = 'users' | 'matrix'

export default function UserManagementPage() {
    const [tab, setTab] = useState<Tab>('users')
    const [creating, setCreating] = useState(false)
    const [selectedRoleCodes, setSelectedRoleCodes] = useState<RoleCode[]>([])

    const handleNavigateToUsers = (roleCode: RoleCode) => {
        setSelectedRoleCodes([roleCode])
        setTab('users')
    }

    return (
        <PageContainer>
            <PageHeader
                breadcrumb={[{ title: 'Quản trị hệ thống' }, { title: 'Người dùng & Phân quyền' }]}
                title="Người dùng & Phân quyền"
                subtitle="Quản lý tài khoản nhân viên và phạm vi truy cập theo vai trò."
                extra={
                    tab === 'users' && (
                        <Can do={PERMISSIONS.USERS_EDIT}>
                            <Button
                                type="primary"
                                size="large"
                                icon={<PlusOutlined />}
                                onClick={() => setCreating(true)}
                            >
                                Thêm người dùng
                            </Button>
                        </Can>
                    )
                }
                bottom={
                    <div className="um-tabs" role="tablist">
                        <button
                            type="button"
                            role="tab"
                            aria-selected={tab === 'users'}
                            className={tab === 'users' ? 'is-active' : ''}
                            onClick={() => setTab('users')}
                        >
                            <TeamOutlined /> Danh sách tài khoản
                        </button>
                        <Can do={PERMISSIONS.ROLES_VIEW}>
                            <button
                                type="button"
                                role="tab"
                                aria-selected={tab === 'matrix'}
                                className={tab === 'matrix' ? 'is-active' : ''}
                                onClick={() => setTab('matrix')}
                            >
                                <AppstoreOutlined /> Ma trận phân quyền
                            </button>
                        </Can>
                    </div>
                }
            />

            {tab === 'users' ? (
                <>
                    <UserStatsCards />
                    <UserTable
                        selectedRoleCodes={selectedRoleCodes}
                        onRoleCodesChange={setSelectedRoleCodes}
                    />
                </>
            ) : (
                <PermissionMatrix onNavigateToUsers={handleNavigateToUsers} />
            )}

            <CreateUserModal open={creating} onClose={() => setCreating(false)} />
        </PageContainer>
    )
}