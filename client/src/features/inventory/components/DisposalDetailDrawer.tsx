import type { ReactNode } from 'react'
import { Alert, Drawer, Skeleton, Table, Tag } from 'antd'
import { fmtDateTime } from '@/features/screening/format'
import { useDisposalDetail } from '../disposalsHooks'
import { STATUS_COLOR } from '../disposalsFormat'
import type { DisposalDetail, DisposalItem } from '../disposalsTypes'

interface Props {
    id: number | null
    onClose: () => void
    actions?: (detail: DisposalDetail) => ReactNode
}

export default function DisposalDetailDrawer({ id, onClose, actions }: Props) {
    const { data, isLoading } = useDisposalDetail(id)

    return (
        <Drawer
            open={id !== null}
            onClose={onClose}
            size={720}
            destroyOnHidden
            title={data ? `Biên bản ${data.disposalCode}` : 'Chi tiết biên bản'}
            footer={data && actions ? <div className="inv-actions">{actions(data)}</div> : undefined}
        >
            {isLoading || !data ? (
                <Skeleton active paragraph={{ rows: 8 }} />
            ) : (
                <div className="inv-result">
                    <dl className="inv-parent" style={{ gridTemplateColumns: 'repeat(3, 1fr)' }}>
                        <div>
                            <dt>Trạng thái</dt>
                            <dd><Tag color={STATUS_COLOR[data.status]}>{data.statusText}</Tag></dd>
                        </div>
                        <div><dt>Lý do</dt><dd>{data.reasonText}</dd></div>
                        <div><dt>Số túi</dt><dd>{data.totalBags}</dd></div>
                        <div><dt>Người lập</dt><dd>{data.requestedByName || '—'}</dd></div>
                        <div><dt>Lập lúc</dt><dd>{fmtDateTime(data.requestedAt)}</dd></div>
                        <div>
                            <dt>Người duyệt</dt>
                            <dd>
                                {data.approvedByName
                                    ? `${data.approvedByName} · ${data.approvedAt ? fmtDateTime(data.approvedAt) : ''}`
                                    : '—'}
                            </dd>
                        </div>
                    </dl>

                    {data.notes && <Alert type="info" showIcon title="Ghi chú" description={<span style={{ whiteSpace: 'pre-line' }}>{data.notes}</span>} />}
                    {data.rejectionReason && (
                        <Alert type="error" showIcon title="Lý do từ chối" description={data.rejectionReason} />
                    )}

                    <Table<DisposalItem>
                        size="small"
                        rowKey="itemId"
                        pagination={false}
                        dataSource={data.items}
                        scroll={{ x: 560 }}
                        columns={[
                            { title: 'Barcode', dataIndex: 'barcode', render: (v: string) => <span className="inv-mono">{v}</span> },
                            { title: 'Nhóm máu', dataIndex: 'bloodGroup', width: 90, render: (v: string) => <Tag color="red">{v}</Tag> },
                            { title: 'Chế phẩm', dataIndex: 'componentTypeCode', width: 90 },
                            { title: 'Thể tích', dataIndex: 'volumeMl', width: 90, render: (v: number) => `${v} ml` },
                            { title: 'Trước khi khóa', dataIndex: 'previousStatusText' },
                            { title: 'Hiện tại', dataIndex: 'currentBagStatusText' },
                        ]}
                    />
                </div>
            )}
        </Drawer>
    )
}