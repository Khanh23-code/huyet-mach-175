import { useEffect, useState } from 'react'
import { App, Button, Input, Popconfirm, Select, Table, Tag, Tooltip } from 'antd'
import { DeleteOutlined, EditOutlined, EyeOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons'
import Can from '@/features/auth/components/Can'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { useDeleteLocation, useManagedLocations } from '../stockHooks'
import type { StorageLocation } from '../types'
import StorageLocationFormModal from './StorageLocationFormModal'
import LocationBagsModal from './LocationBagsModal'

const PAGE_SIZE = 10

export default function StorageLocationsPanel() {
    const { message } = App.useApp()
    const [keyword, setKeyword] = useState('')
    const [search, setSearch] = useState('')
    const [isActive, setIsActive] = useState<boolean | undefined>()
    const [page, setPage] = useState(1)
    const [editing, setEditing] = useState<StorageLocation | null>(null)
    const [formOpen, setFormOpen] = useState(false)
    const [viewing, setViewing] = useState<StorageLocation | null>(null)
    const del = useDeleteLocation()

    useEffect(() => {
        const t = setTimeout(() => { setSearch(keyword.trim()); setPage(1) }, 400)
        return () => clearTimeout(t)
    }, [keyword])

    const { data, isLoading, isFetching } = useManagedLocations({
        search: search || undefined, isActive, pageNumber: page, pageSize: PAGE_SIZE,
    })

    const openForm = (l: StorageLocation | null) => { setEditing(l); setFormOpen(true) }

    const remove = async (l: StorageLocation) => {
        try {
            await del.mutateAsync(l.locationId)
            message.success(`Đã xử lý yêu cầu xóa "${l.refrigeratorName} - ${l.shelfNumber}". Tủ từng có lịch sử sẽ chuyển sang ngừng sử dụng.`)
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    return (
        <section className="inv-card inv-card__body">
            <div className="disp-tools" style={{ justifyContent: 'space-between' }}>
                <div className="disp-tools" style={{ marginBottom: 0 }}>
                    <Input
                        style={{ width: 280 }}
                        prefix={<SearchOutlined />}
                        placeholder="Tìm tên tủ, mã ngăn..."
                        value={keyword}
                        onChange={(e) => setKeyword(e.target.value)}
                        allowClear
                    />
                    <Select
                        style={{ width: 170 }}
                        allowClear
                        placeholder="Mọi trạng thái"
                        value={isActive}
                        onChange={(v) => { setIsActive(v); setPage(1) }}
                        options={[{ value: true, label: 'Đang sử dụng' }, { value: false, label: 'Ngừng sử dụng' }]}
                    />
                </div>
                <Can do={PERMISSIONS.INVENTORY_EDIT}>
                    <Button type="primary" icon={<PlusOutlined />} onClick={() => openForm(null)}>Thêm tủ / ngăn</Button>
                </Can>
            </div>

            <Table<StorageLocation>
                size="middle"
                rowKey="locationId"
                loading={isLoading || isFetching}
                dataSource={data?.items ?? []}
                scroll={{ x: 820 }}
                locale={{ emptyText: 'Chưa có tủ lưu trữ nào' }}
                pagination={{ current: page, pageSize: PAGE_SIZE, total: data?.totalCount ?? 0, showSizeChanger: false, onChange: setPage }}
                columns={[
                    { title: 'Tên tủ', dataIndex: 'refrigeratorName', render: (v: string) => <b>{v}</b> },
                    { title: 'Ngăn / kệ', dataIndex: 'shelfNumber', width: 110 },
                    { title: 'Nhiệt độ', dataIndex: 'targetTemperature', width: 100, render: (v: number) => `${v}°C` },
                    { title: 'Số túi', dataIndex: 'currentBagsCount', width: 80, align: 'right' },
                    {
                        title: 'Cận hạn', dataIndex: 'expiringSoonBagsCount', width: 90, align: 'right',
                        render: (v: number) => (v > 0 ? <b style={{ color: 'var(--c-danger)' }}>{v}</b> : v),
                    },
                    {
                        title: 'Trạng thái', width: 160,
                        render: (_: unknown, l) => (
                            <>
                                <Tag color={l.isActive ? 'green' : 'default'}>{l.isActive ? 'Đang dùng' : 'Ngừng dùng'}</Tag>
                                {l.isFull && <Tag color="orange">Đầy</Tag>}
                            </>
                        ),
                    },
                    {
                        title: '', width: 150, align: 'right',
                        render: (_: unknown, l) => (
                            <div className="inv-actions" style={{ justifyContent: 'flex-end', flexWrap: 'nowrap' }}>
                                <Tooltip title="Xem túi máu trong tủ">
                                    <Button size="small" icon={<EyeOutlined />} onClick={() => setViewing(l)} />
                                </Tooltip>
                                <Can do={PERMISSIONS.INVENTORY_EDIT}>
                                    <Tooltip title="Sửa">
                                        <Button size="small" icon={<EditOutlined />} onClick={() => openForm(l)} />
                                    </Tooltip>
                                    <Popconfirm
                                        title={`Xóa ${l.refrigeratorName} - ${l.shelfNumber}?`}
                                        description="Tủ chưa từng có túi sẽ bị xóa; tủ từng có lịch sử sẽ chuyển sang ngừng sử dụng."
                                        okText="Xóa" cancelText="Hủy" okButtonProps={{ danger: true }}
                                        disabled={l.currentBagsCount > 0}
                                        onConfirm={() => remove(l)}
                                    >
                                        <Tooltip title={l.currentBagsCount > 0 ? 'Tủ đang có túi máu, không thể xóa' : 'Xóa'}>
                                            <Button size="small" danger icon={<DeleteOutlined />} disabled={l.currentBagsCount > 0} loading={del.isPending && del.variables === l.locationId} />
                                        </Tooltip>
                                    </Popconfirm>
                                </Can>
                            </div>
                        ),
                    },
                ]}
            />

            <StorageLocationFormModal open={formOpen} location={editing} onClose={() => setFormOpen(false)} />
            <LocationBagsModal location={viewing} onClose={() => setViewing(null)} />
        </section>
    )
}