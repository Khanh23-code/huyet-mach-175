import { useEffect, useState } from 'react'
import { Button, Input, Radio, Select, Table } from 'antd'
import { BarcodeOutlined, SearchOutlined, SwapOutlined } from '@ant-design/icons'
import Can from '@/features/auth/components/Can'
import { PERMISSIONS } from '@/config/permissions'
import { useBags } from '../stockHooks'
import { BLOOD_GROUPS, STATUS_FILTER_OPTIONS, bloodGroupParams, fmtDay } from '../stockFormat'
import type { BloodBag, RelocatableBag } from '../stockTypes'
import { BagStatusTag, BloodGroupTag } from './StockBadges'
import RelocateBagModal from './RelocateBagModal'

const PAGE_SIZE = 20
const NO_MOVE = ['DISCARDED', 'TRANSFUSED', 'ISSUED']
const NEAR_EXPIRY_HOURS = 72

export default function BagInventoryPanel() {
    const [group, setGroup] = useState('ALL')
    const [status, setStatus] = useState<string | undefined>()
    const [keyword, setKeyword] = useState('')
    const [search, setSearch] = useState('')
    const [page, setPage] = useState(1)
    const [moving, setMoving] = useState<RelocatableBag | null>(null)

    useEffect(() => {
        const t = setTimeout(() => { setSearch(keyword.trim()); setPage(1) }, 400)
        return () => clearTimeout(t)
    }, [keyword])

    const { data, isLoading, isFetching } = useBags({
        ...bloodGroupParams(group), status, search: search || undefined, pageNumber: page, pageSize: PAGE_SIZE,
    })

    const nearExpiry = (iso: string) => new Date(iso).getTime() - Date.now() < NEAR_EXPIRY_HOURS * 3_600_000

    return (
        <section className="inv-card inv-card__body">
            <div className="stk-toolbar">
                <Radio.Group
                    optionType="button"
                    buttonStyle="solid"
                    value={group}
                    onChange={(e) => { setGroup(e.target.value); setPage(1) }}
                    options={[{ value: 'ALL', label: 'Tất cả' }, ...BLOOD_GROUPS.map((g) => ({ value: g, label: g }))]}
                />
                <div className="disp-tools" style={{ marginBottom: 0 }}>
                    <Select
                        style={{ width: 160 }} allowClear placeholder="Mọi trạng thái"
                        value={status} onChange={(v) => { setStatus(v); setPage(1) }} options={STATUS_FILTER_OPTIONS}
                    />
                    <Input
                        style={{ width: 240 }} prefix={<SearchOutlined />} placeholder="Tìm mã túi máu..."
                        value={keyword} onChange={(e) => setKeyword(e.target.value)} allowClear
                    />
                </div>
            </div>

            <Table<BloodBag>
                size="middle"
                rowKey="bagId"
                loading={isLoading || isFetching}
                dataSource={data?.items ?? []}
                scroll={{ x: 980 }}
                locale={{ emptyText: 'Không có túi máu phù hợp' }}
                pagination={{
                    current: page, pageSize: PAGE_SIZE, total: data?.totalCount ?? 0, showSizeChanger: false, onChange: setPage,
                    showTotal: (t, [a, b]) => `Hiển thị ${a}–${b} / ${t} túi máu`,
                }}
                columns={[
                    { title: 'Mã túi máu', dataIndex: 'barcode', render: (v: string) => <span className="inv-mono"><BarcodeOutlined /> <b>{v}</b></span> },
                    { title: 'Nhóm máu', dataIndex: 'bloodGroup', width: 100, render: (v: string) => <BloodGroupTag value={v} /> },
                    { title: 'Sản phẩm', dataIndex: 'componentTypeName' },
                    { title: 'Thể tích', dataIndex: 'volumeMl', width: 90, render: (v: number) => `${v} ml` },
                    { title: 'Ngày thu', dataIndex: 'collectedAt', width: 110, render: fmtDay },
                    {
                        title: 'Hết hạn', dataIndex: 'expiredAt', width: 110,
                        render: (v: string, r) => (
                            <span style={nearExpiry(v) && !NO_MOVE.includes(r.status) ? { color: 'var(--c-danger)', fontWeight: 600 } : undefined}>
                                {fmtDay(v)}
                            </span>
                        ),
                    },
                    { title: 'Vị trí', dataIndex: 'storageLocationName', width: 130, render: (v?: string | null) => v || '—' },
                    { title: 'Trạng thái', dataIndex: 'status', width: 130, render: (v: string) => <BagStatusTag status={v} /> },
                    {
                        title: '', width: 120, align: 'right',
                        render: (_: unknown, r) => (
                            <Can do={PERMISSIONS.INVENTORY_EDIT}>
                                <Button size="small" icon={<SwapOutlined />} disabled={NO_MOVE.includes(r.status)} onClick={() => setMoving(r)}>
                                    Đổi vị trí
                                </Button>
                            </Can>
                        ),
                    },
                ]}
            />
            <RelocateBagModal bag={moving} onClose={() => setMoving(null)} />
        </section>
    )
}