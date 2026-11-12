import { useEffect, useState } from 'react'
import { Input, Modal, Table } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import { useBags } from '../stockHooks'
import { fmtDay } from '../stockFormat'
import type { BloodBag } from '../stockTypes'
import type { StorageLocation } from '../types'
import { BagStatusTag, BloodGroupTag } from './StockBadges'

interface Props {
    location: StorageLocation | null
    onClose: () => void
}

const PAGE_SIZE = 10

/** Chi tiết các túi máu đang lưu trong 1 tủ: GET /api/blood-bags?locationId= */
export default function LocationBagsModal({ location, onClose }: Props) {
    const [keyword, setKeyword] = useState('')
    const [search, setSearch] = useState('')
    const [page, setPage] = useState(1)

    useEffect(() => {
        const t = setTimeout(() => { setSearch(keyword.trim()); setPage(1) }, 400)
        return () => clearTimeout(t)
    }, [keyword])

    const { data, isLoading, isFetching } = useBags(
        { locationId: location?.locationId, search: search || undefined, pageNumber: page, pageSize: PAGE_SIZE },
        location !== null,
    )

    const close = () => { setKeyword(''); setSearch(''); setPage(1); onClose() }

    return (
        <Modal
            open={location !== null}
            onCancel={close}
            footer={null}
            width={860}
            destroyOnHidden
            title={location ? `Túi máu trong ${location.refrigeratorName} - ${location.shelfNumber} (${location.targetTemperature}°C)` : ''}
        >
            <div className="disp-tools">
                <Input
                    style={{ maxWidth: 280 }}
                    prefix={<SearchOutlined />}
                    placeholder="Tìm mã túi máu..."
                    value={keyword}
                    onChange={(e) => setKeyword(e.target.value)}
                    allowClear
                />
            </div>
            <Table<BloodBag>
                size="small"
                rowKey="bagId"
                loading={isLoading || isFetching}
                dataSource={data?.items ?? []}
                scroll={{ x: 700 }}
                locale={{ emptyText: 'Tủ này đang trống' }}
                pagination={{ current: page, pageSize: PAGE_SIZE, total: data?.totalCount ?? 0, showSizeChanger: false, onChange: setPage }}
                columns={[
                    { title: 'Mã túi máu', dataIndex: 'barcode', render: (v: string) => <span className="inv-mono">{v}</span> },
                    { title: 'Nhóm máu', dataIndex: 'bloodGroup', width: 90, render: (v: string) => <BloodGroupTag value={v} /> },
                    { title: 'Sản phẩm', dataIndex: 'componentTypeName' },
                    { title: 'Thể tích', dataIndex: 'volumeMl', width: 90, render: (v: number) => `${v} ml` },
                    { title: 'Hết hạn', dataIndex: 'expiredAt', width: 110, render: fmtDay },
                    { title: 'Trạng thái', dataIndex: 'status', width: 130, render: (v: string) => <BagStatusTag status={v} /> },
                ]}
            />
        </Modal>
    )
}