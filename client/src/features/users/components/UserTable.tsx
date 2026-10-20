import { useState } from 'react'
import { App, Button, Empty, Input, Popconfirm, Select, Table, Tooltip } from 'antd'
import type { TableColumnsType } from 'antd'
import { LockOutlined, SearchOutlined, SettingOutlined, UnlockOutlined } from '@ant-design/icons'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@/features/auth/authStore'
import { ROLE_CODES, ROLE_META } from '@/config/rbac'
import type { RoleCode, UserItem } from '@/types/user'
import { useDebounce, useUsers } from '../hooks'
import { usersApi } from '../usersApi'

const PAGE_SIZE = 10

const initials = (name: string) =>
    name.trim().split(/\s+/).slice(-2).map((w) => w[0]?.toUpperCase()).join('')

export default function UserTable() {
    const { message } = App.useApp()
    const qc = useQueryClient()
    const me = useAuthStore((s) => s.user) as { id?: string | number; username?: string } | null

    const [keyword, setKeyword] = useState('')
    const [roleCode, setRoleCode] = useState<RoleCode | undefined>()
    const [page, setPage] = useState(1)
    const debounced = useDebounce(keyword)

    const { data, isLoading, isFetching, isError } = useUsers({
        keyword: debounced || undefined,
        roleCode,
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
        onError: () => message.error('Không thể cập nhật trạng thái, vui lòng thử lại'),
    })

    const isSelf = (u: UserItem) =>
        !!me && ((me.id !== undefined && String(me.id) === String(u.id)) || (!!me.username && me.username === u.username))

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
              <small>{ROLE_META[r]?.label}</small>
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
                return (
                    <div className="um-actions">
                        <Tooltip title="Chỉnh sửa vai trò (chưa có API)">
                            <Button type="text" icon={<SettingOutlined />} disabled />
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
                        allowClear
                        placeholder="Tất cả vai trò"
                        style={{ minWidth: 170 }}
                        value={roleCode}
                        onChange={(v) => { setRoleCode(v); setPage(1) }}
                        options={ROLE_CODES.map((c) => ({ value: c, label: `${c} · ${ROLE_META[c].label}` }))}
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
        </section>
    )
}
