import { useState } from 'react'
import { Button, Input, Modal, Popconfirm, Select, Space, Table, message } from 'antd'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { fmtDateTime } from '@/shared/formatters/date'
import { PageContainer, PageHeader, SectionCard, StatusPill } from '@/shared/components'
import { useApproveDisposal, useDisposals } from '../disposalsHooks'
import { DISPOSAL_STATUS_TONE, STATUS_OPTIONS } from '../disposalsFormat'
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
    const [openId, setOpenId] = useState<string | null>(null)
    const [rejectId, setRejectId] = useState<string | null>(null)
    const [reason, setReason] = useState('')

    const { data, isLoading, isFetching } = useDisposals(
        { status, pageNumber: page, pageSize: PAGE_SIZE },
        { refetchInterval: REFRESH_MS },
    )
    const approve = useApproveDisposal()

    const doApprove = async (id: string) => {
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
        <PageContainer>
            <PageHeader
                breadcrumb={[{ title: 'Túi máu & Kho' }, { title: 'Phê duyệt biên bản hủy' }]}
                title="Phê duyệt biên bản hủy"
                subtitle="Xem danh sách túi trong từng biên bản, sau đó duyệt hoặc từ chối kèm lý do."
            />

            <SectionCard>
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
                            render: (_: unknown, r) => (
                                <StatusPill tone={DISPOSAL_STATUS_TONE[r.status]}>{r.statusText}</StatusPill>
                            ),
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
            </SectionCard>

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
        </PageContainer>
    )
}