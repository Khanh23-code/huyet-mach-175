import { useState } from 'react'
import { Breadcrumb, Button, Input, Modal, Popconfirm, Select, Space, Table, Tag, message } from 'antd'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { fmtDateTime } from '@/features/screening/format'
import { useApproveDisposal, useDisposals } from '../disposalsHooks'
import { STATUS_COLOR, STATUS_OPTIONS } from '../disposalsFormat'
import DisposalDetailDrawer from '../components/DisposalDetailDrawer'
import type { DisposalRecord } from '../disposalsTypes'
import './InventoryPages.css'
import './Disposal.css'

const PAGE_SIZE = 20
const REFRESH_MS = 30_000

export default function DisposalApprovalPage() {
    const { can } = usePermission()
    const canApprove = can(PERMISSIONS.DISPOSAL_APPROVE)

    const [status, setStatus] = useState<string>('PENDING_APPROVAL')
    const [page, setPage] = useState(1)
    const [openId, setOpenId] = useState<number | null>(null)
    const [rejectId, setRejectId] = useState<number | null>(null)
    const [reason, setReason] = useState('')

    const { data, isLoading, isFetching } = useDisposals(
        { status, pageNumber: page, pageSize: PAGE_SIZE },
        { refetchInterval: REFRESH_MS },
    )
    const approve = useApproveDisposal()

    const doApprove = async (id: number) => {
        try {
            await approve.mutateAsync({ id, body: { isApproved: true } })
            message.success('Đã phê duyệt biên bản')
            setOpenId(null)
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    const doReject = async () => {
        if (rejectId === null || !reason.trim()) return
        try {
            await approve.mutateAsync({ id: rejectId, body: { isApproved: false, rejectionReason: reason.trim() } })
            message.success('Đã từ chối biên bản, các túi được khôi phục trạng thái trước đó')
            setRejectId(null)
            setReason('')
            setOpenId(null)
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    return (
        <div className="inv">
            <Breadcrumb items={[{ title: 'Túi máu & Kho' }, { title: 'Phê duyệt biên bản hủy' }]} />
            <div className="inv-title">
                <h1>Phê duyệt biên bản hủy</h1>
                <p>Xem danh sách túi trong từng biên bản, sau đó duyệt hoặc từ chối kèm lý do.</p>
            </div>

            <section className="inv-card inv-card__body">
                <div className="disp-tools">
                    <Select
                        style={{ minWidth: 260 }}
                        options={STATUS_OPTIONS}
                        value={status}
                        onChange={(v) => {
                            setStatus(v)
                            setPage(1)
                        }}
                    />
                </div>
                <Table<DisposalRecord>
                    size="middle"
                    rowKey="disposalId"
                    loading={isLoading || isFetching}
                    dataSource={data?.items ?? []}
                    scroll={{ x: 880 }}
                    locale={{ emptyText: 'Không có biên bản nào ở trạng thái này' }}
                    pagination={{
                        current: page,
                        pageSize: PAGE_SIZE,
                        total: data?.totalCount ?? 0,
                        showSizeChanger: false,
                        onChange: setPage,
                    }}
                    columns={[
                        { title: 'Mã biên bản', dataIndex: 'disposalCode', render: (v: string) => <span className="inv-mono">{v}</span> },
                        { title: 'Số túi', dataIndex: 'totalBags', width: 80 },
                        { title: 'Túi máu', dataIndex: 'barcodesSummary', ellipsis: true },
                        { title: 'Lý do', dataIndex: 'reasonText' },
                        { title: 'Người lập', dataIndex: 'requestedByName' },
                        {
                            title: 'Trạng thái',
                            dataIndex: 'status',
                            render: (_: unknown, r) => <Tag color={STATUS_COLOR[r.status]}>{r.statusText}</Tag>,
                        },
                        { title: 'Lập lúc', dataIndex: 'requestedAt', width: 150, render: (v: string) => fmtDateTime(v) },
                        {
                            title: '',
                            key: 'act',
                            width: 90,
                            render: (_: unknown, r) => (
                                <Button type="link" onClick={() => setOpenId(r.disposalId)}>
                                    {r.status === 'PENDING_APPROVAL' && canApprove ? 'Xem & duyệt' : 'Chi tiết'}
                                </Button>
                            ),
                        },
                    ]}
                />
            </section>

            <DisposalDetailDrawer
                id={openId}
                onClose={() => setOpenId(null)}
                actions={(d) =>
                    canApprove && d.status === 'PENDING_APPROVAL' ? (
                        <Space>
                            <Button danger onClick={() => setRejectId(d.disposalId)}>Từ chối</Button>
                            <Popconfirm
                                title="Phê duyệt biên bản này?"
                                description={`${d.totalBags} túi sẽ chờ BBNK xác nhận tiêu hủy vật lý.`}
                                okText="Phê duyệt"
                                cancelText="Không"
                                onConfirm={() => doApprove(d.disposalId)}
                            >
                                <Button type="primary" loading={approve.isPending}>Phê duyệt</Button>
                            </Popconfirm>
                        </Space>
                    ) : null
                }
            />

            <Modal
                open={rejectId !== null}
                title="Từ chối biên bản"
                okText="Từ chối biên bản"
                cancelText="Hủy"
                okButtonProps={{ danger: true, disabled: !reason.trim(), loading: approve.isPending }}
                onOk={doReject}
                onCancel={() => {
                    setRejectId(null)
                    setReason('')
                }}
                destroyOnHidden
            >
                <Input.TextArea
                    rows={4}
                    maxLength={500}
                    showCount
                    placeholder="Nhập lý do từ chối (bắt buộc)"
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                />
            </Modal>
        </div>
    )
}