import { useState } from 'react'
import { Button, Table } from 'antd'
import { useRecentLogs } from '../stockHooks'
import { fmtDateTimeSec } from '../stockFormat'
import type { StatusLog } from '../stockTypes'
import { BagStatusTag } from './StockBadges'

export default function RecentLogsPanel() {
    const [all, setAll] = useState(false)
    const { data, isLoading, isFetching } = useRecentLogs(all ? 100 : 10)

    return (
        <>
            <div className="disp-tools" style={{ justifyContent: 'flex-end' }}>
                <Button onClick={() => setAll((v) => !v)}>{all ? 'Chỉ xem 10 mục gần nhất' : 'Xem toàn bộ nhật ký'}</Button>
            </div>
            <Table<StatusLog>
                size="middle"
                rowKey="logId"
                loading={isLoading || isFetching}
                dataSource={data?.items ?? []}
                scroll={{ x: 760 }}
                locale={{ emptyText: 'Chưa có nhật ký chuyển trạng thái' }}
                pagination={all ? { pageSize: 10, showSizeChanger: false } : false}
                columns={[
                    { title: 'Mã túi máu', dataIndex: 'barcode', render: (v: string) => <b className="inv-mono">{v}</b> },
                    { title: 'Trạng thái trước', dataIndex: 'previousStatus', render: (v: string, r) => <BagStatusTag status={v} label={r.previousStatusName} /> },
                    { title: 'Trạng thái mới', dataIndex: 'newStatus', render: (v: string, r) => <BagStatusTag status={v} label={r.newStatusName} /> },
                    { title: 'Người thao tác', dataIndex: 'performedBy' },
                    { title: 'Thời điểm', dataIndex: 'timestamp', width: 190, render: fmtDateTimeSec },
                ]}
            />
        </>
    )
}