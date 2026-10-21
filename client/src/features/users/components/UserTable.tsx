import { useState } from 'react'
import { App, Button, Empty, Input, Popconfirm, Select, Table, Tooltip } from 'antd'
import type { TableColumnsType } from 'antd'
import { EditOutlined, LockOutlined, SearchOutlined, UnlockOutlined } from '@ant-design/icons'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@/features/auth/authStore'
import { ROLE_META } from '@/config/rbac'
import type { RoleCode, UserItem } from '@/types/user'
import { useDebounce, useRoles, useUsers } from '../hooks'
import { usersApi } from '../usersApi'
import { parseApiError } from '@/services/apiError'
import EditUserModal from './EditUserModal'

const PAGE_SIZE = 10

const initials = (name: string) =>
    name.trim().split(/\s+/).slice(-2).map((w) => w[0]?.toUpperCase()).join('')

interface UserTableProps {
    selectedRoleCodes?: RoleCode[]
    onRoleCodesChange?: (roles: RoleCode[]) => void
}

export default function UserTable({ selectedRoleCodes, onRoleCodesChange }: UserTableProps = {}) {
    const { message } = App.useApp()
    const qc = useQueryClient()
    const me = useAuthStore((s) => s.user) as
        | { id?: string | number; username?: string; roles?: string[] }
        | null

    const [keyword, setKeyword] = useState('')
    const [localRoleCodes, setLocalRoleCodes] = useState<RoleCode[]>([])
    const roleCodes = selectedRoleCodes !== undefined ? selectedRoleCodes : localRoleCodes
    const setRoleCodes = onRoleCodesChange ?? setLocalRoleCodes
    const [page, setPage] = useState(1)
    const [editing, setEditing] = useState<UserItem | null>(null)
    const debounced = useDebounce(keyword)
    const rolesQuery = useRoles()

    const [prevRoleCodes, setPrevRoleCodes] = useState(roleCodes)
    if (prevRoleCodes !== roleCodes) {
        setPrevRoleCodes(roleCodes)
        setPage(1)
    }

    const { data, isLoading, isFetching, isError } = useUsers({
        keyword: debounced || undefined,
        roleCodes: roleCodes.length > 0 ? roleCodes : undefined,
        pageNumber: page,
        pageSize: PAGE_SIZE,
    })

    const toggle = useMutation({
        mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) =>
            usersApi.updateStatus(id, isActive),
        onSuccess: (_, v) => {
            message.success(v.isActive ? 'Đã mở khóa tài khoản' : 'Đã khóa tài khoản')
            qc.invalidateQueries({ queryKey: ['users'] })
        },
        onError: (err) => message.error(parseApiError(err).message),
    })

    const isSelf = (u: UserItem) =>
        !!me && ((me.id !== undefined && String(me.id) === String(u.id)) || (!!me.username && me.username === u.username))

    const isSysAdmin = !!me?.roles?.some((r) => r.trim().toUpperCase() === 'SYS')
    // BE: chỉ SYS được sửa tài khoản SYS
    const canEdit = (u: UserItem) => isSysAdmin || !u.roles.includes('SYS')

    const columns: TableColumnsType<UserItem> = [
        {
            title: 'HỌ VÀ TÊN',
            dataIndex: 'fullName',
            render: (name: string) => (
                <div className="um-user">
                    <span className="um-avatar">{initials(name)}</span>
                    <strong>{name}</strong>
                </div>
            ),
        },
        { title: 'MÃ NHÂN VIÊN', dataIndex: 'staffCode', render: (v: string) => <strong>{v}</strong> },
        { title: 'KHOA / PHÒNG', dataIndex: 'departmentName' },
        {
            title: 'VAI TRÒ ĐƯỢC GÁN',
            dataIndex: 'roles',
            render: (roles: RoleCode[]) => (
                <div className="um-roles">
                    {roles.map((r) => (
                        <span className="um-role" key={r}>
                            <b className={`um-role__code um-role__code--${r}`}>{r}</b>
                            <small>{ROLE_META[r]?.label ?? r}</small>
                        </span>
                    ))}
                </div>
            ),
        },
        {
            title: 'TRẠNG THÁI',
            dataIndex: 'isActive',
            render: (a: boolean) => (
                <span className={`um-status ${a ? 'um-status--on' : 'um-status--off'}`}>
                    <i /> {a ? 'Active' : 'Locked'}
                </span>
            ),
        },
        {
            title: 'THAO TÁC',
            align: 'right',
            render: (_, u) => {
                const self = isSelf(u)
                const nextActive = !u.isActive
                const editable = canEdit(u)
                return (
                    <div className="um-actions">
                        <Tooltip title={editable ? 'Chỉnh sửa người dùng' : 'Chỉ quản trị hệ thống (SYS) được sửa tài khoản SYS'}>
                            <Button
                                type="text"
                                icon={<EditOutlined />}
                                disabled={!editable}
                                onClick={() => setEditing(u)}
                            />
                        </Tooltip>
                        <Popconfirm
                            title={nextActive ? 'Mở khóa tài khoản này?' : 'Khóa tài khoản này?'}
                            okText={nextActive ? 'Mở khóa' : 'Khóa'}
                            cancelText="Hủy"
                            disabled={self}
                            onConfirm={() => toggle.mutate({ id: u.id, isActive: nextActive })}
                        >
                            <Tooltip title={self ? 'Không thể tự khóa tài khoản của mình' : undefined}>
                                <Button
                                    type="text"
                                    disabled={self}
                                    loading={toggle.isPending && toggle.variables?.id === u.id}
                                    icon={u.isActive ? <LockOutlined /> : <UnlockOutlined style={{ color: '#1d4ed8' }} />}
                                />
                            </Tooltip>
                        </Popconfirm>
                    </div>
                )
            },
        },
    ]

    return (
        <section className="um-panel">
            <header className="um-panel__head">
                <div>
                    <h3>Danh sách người dùng</h3>
                    <p>Đồng bộ từ danh mục nhân sự bệnh viện</p>
                </div>
                <div className="um-filters">
                    <Input
                        allowClear
                        prefix={<SearchOutlined />}
                        placeholder="Tìm tên hoặc mã nhân viên..."
                        value={keyword}
                        onChange={(e) => { setKeyword(e.target.value); setPage(1) }}
                    />
                    <Select
                        mode="multiple"
                        allowClear
                        placeholder="Lọc theo vai trò"
                        style={{ minWidth: 200, maxWidth: 360 }}
                        maxTagCount="responsive"
                        value={roleCodes}
                        onChange={(v) => { setRoleCodes(v); setPage(1) }}
                        options={rolesQuery.data?.map((r) => ({ value: r.code, label: `${r.code} · ${r.name}` }))}
                    />
                </div>
            </header>

            <Table<UserItem>
                rowKey="id"
                columns={columns}
                dataSource={data?.items}
                loading={isLoading || isFetching}
                locale={{
                    emptyText: isError
                        ? <Empty description="Không tải được danh sách. Kiểm tra kết nối máy chủ rồi thử lại." />
                        : <Empty description="Không có người dùng phù hợp với bộ lọc" />,
                }}
                pagination={{
                    current: page,
                    pageSize: PAGE_SIZE,
                    total: data?.total ?? 0,
                    showSizeChanger: false,
                    onChange: setPage,
                    showTotal: (total, [from, to]) => `Hiển thị ${from}–${to} trong ${total} người dùng`,
                }}
                scroll={{ x: 900 }}
            />

            <EditUserModal
                user={editing}
                isSelf={!!editing && isSelf(editing)}
                canAssignSys={isSysAdmin}
                onClose={() => setEditing(null)}
            />
        </section>
    )
}