import { useState } from 'react'
import { Table } from 'antd'
import type { TableColumnsType } from 'antd'
import { useMyProposals } from '../hooks'
import { SITE_TYPE_LABEL, STATUS_LABEL, fmtDateTime, proposalTitle, slotsText } from '../format'
import type { Proposal } from '../types'

const PAGE_SIZE = 10

export default function MyProposals() {
    const [page, setPage] = useState(1)
    const { data, isLoading, isFetching } = useMyProposals({ pageNumber: page, pageSize: PAGE_SIZE })

    const columns: TableColumnsType<Proposal> = [
        { title: 'MÃ ĐỀ XUẤT', dataIndex: 'proposalCode', render: (v: string) => <b>{v}</b>, width: 170 },
        {
            title: 'ĐIỂM HIẾN',
            width: 260,
            render: (_, p) => (
                <>
                    <b>{p.siteName}</b>
                    <div className="ds-muted ds-small">{SITE_TYPE_LABEL[p.siteType]}</div>
                </>
            ),
        },
        {
            title: 'NỘI DUNG',
            render: (_, p) => (
                <>
                    <div>{p.proposalType === 'NEW_SITE' ? proposalTitle(p) : slotsText(p.slots)}</div>
                    {p.status === 'REJECTED' && p.rejectionReason && (
                        <div className="ds-reason"><b>Lý do:</b> {p.rejectionReason}</div>
                    )}
                </>
            ),
        },
        { title: 'NGÀY GỬI', dataIndex: 'createdAt', width: 170, render: (v: string) => fmtDateTime(v) },
        {
            title: 'TRẠNG THÁI',
            dataIndex: 'status',
            width: 160,
            render: (s: Proposal['status']) => (
                <span className={`ds-status ds-status--${s}`}><i />{STATUS_LABEL[s]}</span>
            ),
        },
    ]

    return (
        <section className="ds-card">
            <div className="ds-card__head">
                <h3>Đề xuất của tôi</h3>
                <p>Theo dõi trạng thái xử lý và phản hồi từ SYS.</p>
            </div>
            <Table<Proposal>
                rowKey="proposalId"
                columns={columns}
                dataSource={data?.items ?? []}
                loading={isLoading || isFetching}
                scroll={{ x: 860 }}
                locale={{ emptyText: 'Chưa có đề xuất nào' }}
                pagination={{
                    current: page,
                    pageSize: PAGE_SIZE,
                    total: data?.total ?? 0,
                    showSizeChanger: false,
                    hideOnSinglePage: true,
                    onChange: setPage,
                }}
            />
        </section>
    )
}
