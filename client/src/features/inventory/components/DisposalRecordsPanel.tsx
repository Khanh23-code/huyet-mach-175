import { useState } from 'react'
import { Button, Popconfirm, Select, Space, Table, Tag, message } from 'antd'
import { parseApiError } from '@/services/apiError'
import { fmtDateTime } from '@/features/screening/format'
import { useCancelDisposal, useDisposals, useExecuteDisposal } from '../disposalsHooks'
import { STATUS_COLOR, STATUS_OPTIONS } from '../disposalsFormat'
import DisposalDetailDrawer from './DisposalDetailDrawer'
import type { DisposalRecord } from '../disposalsTypes'

interface Props {
    canEdit: boolean
}

const PAGE_SIZE = 20

export default function DisposalRecordsPanel({ canEdit }: Props) {
    const [status, setStatus] = useState<string | undefined>()
    const [page, setPage] = useState(1)
    const [openId, setOpenId] = useState<number | null>(null)

    const { data, isLoading, isFetching } = useDisposals({ status, pageNumber: page, pageSize: PAGE_SIZE })
    const cancel = useCancelDisposal()
    const execute = useExecuteDisposal()

    const doCancel = async (id: number) => {
        try {
            await cancel.mutateAsync({ id })
            message.success('Đã rút biên bản, các túi được khôi phục trạng thái trước đó')
            setOpenId(null)
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    const doExecute = async (id: number) => {
        try {
            await execute.mutateAsync(id)
            message.success('Đã ghi nhận tiêu hủy, các túi chuyển sang DISCARDED')
            setOpenId(null)
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    const actionsFor = (r: Pick<DisposalRecord, 'disposalId' | 'status'>) => (
        <>
            {canEdit && r.status === 'PENDING_APPROVAL' && (
                <Popconfirm
                    title="Rút biên bản này?"
                    description="Các túi trong lô được khôi phục trạng thái trước khi khóa."
                    okText="Rút biên bản"
                    cancelText="Không"
                    onConfirm={() => doCancel(r.disposalId)}
                >
                    <Button danger loading={cancel.isPending}>Rút biên bản</Button>
                </Popconfirm>
            )}
            {canEdit && r.status === 'APPROVED' && (
                <Popconfirm
                    title="Xác nhận đã tiêu hủy vật lý?"
                    description="Các túi sẽ chuyển sang DISCARDED và không thể hoàn tác."
                    okText="Xác nhận"
                    cancelText="Không"
                    onConfirm={() => doExecute(r.disposalId)}
                >
                    <Button type="primary" loading={execute.isPending}>Xác nhận đã tiêu hủy</Button>
                </Popconfirm>
            )}
        </>
    )

    return (
        <section className="inv-card inv-card__body">
            <div className="disp-tools">
                <Select
                    style={{ minWidth: 240 }}
                    placeholder="Mọi trạng thái"
                    allowClear
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
                locale={{ emptyText: 'Chưa có biên bản nào' }}
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
                    {
                        title: 'Trạng thái',
                        dataIndex: 'status',
                        render: (_: unknown, r) => <Tag color={STATUS_COLOR[r.status]}>{r.statusText}</Tag>,
                    },
                    { title: 'Lập lúc', dataIndex: 'requestedAt', width: 150, render: (v: string) => fmtDateTime(v) },
                    {
                        title: '',
                        key: 'act',
                        width: 80,
                        render: (_: unknown, r) => <Button type="link" onClick={() => setOpenId(r.disposalId)}>Chi tiết</Button>,
                    },
                ]}
            />
            <DisposalDetailDrawer
                id={openId}
                onClose={() => setOpenId(null)}
                actions={(d) => <Space>{actionsFor(d)}</Space>}
            />
        </section>
    )
}