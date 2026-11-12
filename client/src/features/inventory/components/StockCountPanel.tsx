import { useMemo, useState } from 'react'
import { Alert, Button, InputNumber, Table, Tag } from 'antd'
import { EyeOutlined, ReloadOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import { useManagedLocations } from '../stockHooks'
import type { StorageLocation } from '../types'
import LocationBagsModal from './LocationBagsModal'

/**
 * Kiểm kê: "Hệ thống" = currentBagsCount của tủ (BE). "Thực tế" do người dùng nhập tại FE.
 * TODO(BE): chưa có API lưu phiên kiểm kê -> số đếm thực tế chưa được lưu.
 */
export default function StockCountPanel() {
    const { data, isLoading, isFetching, refetch, dataUpdatedAt } = useManagedLocations({
        isActive: true, pageNumber: 1, pageSize: 100,
    })
    const [actual, setActual] = useState<Record<number, number | null>>({})
    const [viewing, setViewing] = useState<StorageLocation | null>(null)

    const rows = data?.items ?? []
    const diffs = useMemo(
        () =>
            rows
                .filter((r) => actual[r.locationId] != null && actual[r.locationId] !== r.currentBagsCount)
                .map((r) => ({ r, d: (actual[r.locationId] as number) - r.currentBagsCount })),
        [rows, actual],
    )

    const refresh = () => { setActual({}); refetch() }

    return (
        <section className="inv-card">
            <div className="inv-card__head stk-head">
                <div>
                    <h2>Đối soát tồn kho theo vị trí</h2>
                    <p>
                        So sánh số liệu hệ thống với số đếm thực tế
                        {dataUpdatedAt ? `. Cập nhật ${dayjs(dataUpdatedAt).format('DD/MM/YYYY HH:mm')}` : ''}.
                    </p>
                </div>
                <Button icon={<ReloadOutlined />} loading={isFetching} onClick={refresh}>Làm mới</Button>
            </div>

            <Table<StorageLocation>
                rowKey="locationId"
                loading={isLoading}
                dataSource={rows}
                pagination={false}
                scroll={{ x: 640 }}
                locale={{ emptyText: 'Chưa có vị trí lưu trữ' }}
                rowClassName={(r) => (actual[r.locationId] != null && actual[r.locationId] !== r.currentBagsCount ? 'stk-row-diff' : '')}
                columns={[
                    { title: 'Vị trí kho', render: (_: unknown, r) => `${r.refrigeratorName} - ${r.shelfNumber}` },
                    { title: 'Hệ thống', dataIndex: 'currentBagsCount', width: 120, align: 'right' },
                    {
                        title: 'Thực tế', width: 140, align: 'right',
                        render: (_: unknown, r) => (
                            <InputNumber
                                min={0} style={{ width: 90 }} placeholder="Nhập"
                                value={actual[r.locationId] ?? null}
                                onChange={(v) => setActual((p) => ({ ...p, [r.locationId]: v }))}
                            />
                        ),
                    },
                    {
                        title: 'Sai lệch', width: 120, align: 'center',
                        render: (_: unknown, r) => {
                            const a = actual[r.locationId]
                            if (a == null) return <span className="stk-muted">—</span>
                            const d = a - r.currentBagsCount
                            return d === 0 ? <Tag color="green">Khớp</Tag> : <Tag color="orange">{d > 0 ? `+${d}` : d}</Tag>
                        },
                    },
                    {
                        title: '', width: 90, align: 'right',
                        render: (_: unknown, r) => <Button size="small" icon={<EyeOutlined />} onClick={() => setViewing(r)}>Xem túi</Button>,
                    },
                ]}
            />

            <div className="inv-card__body">
                {diffs.length > 0 ? (
                    <Alert
                        type="warning" showIcon
                        title={`${diffs.length} vị trí có sai lệch`}
                        description={`${diffs.map(({ r, d }) => `${r.refrigeratorName} - ${r.shelfNumber} ${d < 0 ? `thiếu ${-d}` : `thừa ${d}`} túi`).join('; ')}. Vui lòng xác minh trước khi điều chỉnh.`}
                    />
                ) : (
                    <Alert type="info" showIcon title="Nhập số đếm thực tế ở từng vị trí để đối soát. Số liệu nhập chưa được lưu vào hệ thống." />
                )}
            </div>
            <LocationBagsModal location={viewing} onClose={() => setViewing(null)} />
        </section>
    )
}