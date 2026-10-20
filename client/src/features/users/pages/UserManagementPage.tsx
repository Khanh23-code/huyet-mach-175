import { useState } from 'react'
import { Breadcrumb, Button } from 'antd'
import { AppstoreOutlined, PlusOutlined, TeamOutlined } from '@ant-design/icons'
import UserStatsCards from '../components/UserStatsCards'
import UserTable from '../components/UserTable'
import PermissionMatrix from '../components/PermissionMatrix'
import CreateUserModal from '../components/CreateUserModal'
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
        <div className="um">
            <Breadcrumb items={[{ title: 'Quản trị hệ thống' }, { title: 'Người dùng & Phân quyền' }]} />

            <div className="um-title">
                <div>
                    <h1>Người dùng &amp; Phân quyền</h1>
                    <p>Quản lý tài khoản nhân viên và phạm vi truy cập theo vai trò.</p>
                </div>
                {tab === 'users' && (
                    <Button type="primary" size="large" icon={<PlusOutlined />} onClick={() => setCreating(true)}>
                        Thêm người dùng
                    </Button>
                )}
            </div>

            <div className="um-tabs" role="tablist">
                <button role="tab" aria-selected={tab === 'users'} className={tab === 'users' ? 'is-active' : ''} onClick={() => setTab('users')}>
                    <TeamOutlined /> Danh sách tài khoản
                </button>
                <button role="tab" aria-selected={tab === 'matrix'} className={tab === 'matrix' ? 'is-active' : ''} onClick={() => setTab('matrix')}>
                    <AppstoreOutlined /> Ma trận phân quyền
                </button>
            </div>

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
        </div>
    )
}