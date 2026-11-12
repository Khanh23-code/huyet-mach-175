import { Alert, Button, Popconfirm, Table, Tag } from 'antd'
import { ReloadOutlined, SendOutlined } from '@ant-design/icons'
import { removeOffline } from '../offlineQueue'
import { fmtDateTimeSec, statusMeta } from '../stockFormat'
import type { useOfflineSync } from '../stockHooks'
import type { OfflineTransition } from '../stockTypes'
import { BagStatusTag } from './StockBadges'

type Sync = ReturnType<typeof useOfflineSync>

export default function OfflineQueuePanel({ sync, canEdit }: { sync: Sync; canEdit: boolean }) {
    const { items, busyId, syncing, syncOne, syncAll } = sync

    return (
        <>
            <div className="disp-tools" style={{ justifyContent: 'space-between', alignItems: 'center' }}>
                <span className="stk-muted">Lệnh bị gián đoạn do mất mạng được lưu trên trình duyệt này.</span>
                <Button type="primary" icon={<SendOutlined />} loading={syncing} disabled={!canEdit || items.length === 0} onClick={() => syncAll()}>
                    Gửi tất cả ({items.length})
                </Button>
            </div>
            {!canEdit && <Alert type="warning" showIcon title="Bạn chỉ có quyền xem, không thể gửi lại lệnh." style={{ marginBottom: 12 }} />}
            <Table<OfflineTransition>
                size="middle"
                rowKey="id"
                dataSource={items}
                pagination={false}
                scroll={{ x: 720 }}
                locale={{ emptyText: 'Không có lệnh nào đang chờ gửi' }}
                columns={[
                    { title: 'Mã túi máu', dataIndex: 'barcode', render: (v: string) => <b className="inv-mono">{v}</b> },
                    { title: 'Chuyển sang', dataIndex: 'targetStatus', render: (v: string) => <BagStatusTag status={v} label={statusMeta(v).label} /> },
                    { title: 'Ghi nhận lúc', dataIndex: 'createdAt', width: 190, render: fmtDateTimeSec },
                    {
                        title: 'Tình trạng',
                        render: (_: unknown, r) => (r.error ? <Tag color="red" title={r.error}>Bị từ chối: {r.error}</Tag> : <Tag color="gold">Chờ gửi</Tag>),
                    },
                    {
                        title: '', width: 170, align: 'right',
                        render: (_: unknown, r) => (
                            <div className="inv-actions" style={{ justifyContent: 'flex-end', flexWrap: 'nowrap' }}>
                                <Button size="small" icon={<ReloadOutlined />} disabled={!canEdit} loading={busyId === r.id} onClick={() => syncOne(r)}>Gửi lại</Button>
                                <Popconfirm title="Xóa lệnh này khỏi hàng đợi?" okText="Xóa" cancelText="Hủy" onConfirm={() => removeOffline(r.id)}>
                                    <Button size="small" danger>Xóa</Button>
                                </Popconfirm>
                            </div>
                        ),
                    },
                ]}
            />
        </>
    )
}