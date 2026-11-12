import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import dayjs from 'dayjs'
import { App, Breadcrumb, Button, Input, Select, Skeleton, Table } from 'antd'
import {
    BellOutlined, DownloadOutlined, ExclamationOutlined, HourglassOutlined, ReloadOutlined, SearchOutlined,
} from '@ant-design/icons'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { COMPONENT_LABEL } from '../format'
import { useExpiringSoon, useTransitionBag } from '../stockHooks'
import { fmtRemaining } from '../stockFormat'
import type { ExpiringSoonBag } from '../stockTypes'
import { BloodGroupTag } from '../components/StockBadges'
import './InventoryPages.css'
import './Disposal.css'
import './Stock.css'
import './ExpiryAlerts.css'

const REFRESH_MS = 30_000
const FETCH_SIZE = 200
const PAGE_SIZE = 10
const DAY_OPTIONS = [1, 3, 7, 14, 30].map((d) => ({ value: d, label: `${d} ngày` }))
const COMPONENT_OPTIONS = Object.entries(COMPONENT_LABEL).map(([value, label]) => ({ value, label }))
const two = (n: number) => String(n).padStart(2, '0')

const tone = (h: number) => (h < 24 ? 'red' : h <= 72 ? 'amber' : 'blue')

export default function ExpiryAlertsPage() {
    const { message, modal } = App.useApp()
    const navigate = useNavigate()
    const { can } = usePermission()
    const lock = useTransitionBag()

    const [days, setDays] = useState(7)
    const [component, setComponent] = useState<string | undefined>()
    const [keyword, setKeyword] = useState('')
    const [page, setPage] = useState(1)

    const { data, isLoading, isFetching, refetch, dataUpdatedAt } = useExpiringSoon(
        { daysThreshold: days, pageNumber: 1, pageSize: FETCH_SIZE },
        REFRESH_MS,
    )

    // Cận hạn nhất lên đầu (FEFO)
    const all = useMemo(() => [...(data?.items ?? [])].sort((a, b) => a.hoursRemaining - b.hoursRemaining), [data])
    const rows = useMemo(() => {
        const k = keyword.trim().toLowerCase()
        return all.filter(
            (r) => (!component || r.componentTypeCode === component) && (!k || r.barcode.toLowerCase().includes(k)),
        )
    }, [all, component, keyword])

    const under24 = all.filter((r) => r.hoursRemaining < 24).length
    const d1to3 = all.filter((r) => r.hoursRemaining >= 24 && r.hoursRemaining <= 72).length

    const canDispose = (r: ExpiringSoonBag) =>
        can(PERMISSIONS.DISPOSAL_EDIT) && (r.status === 'LOCKED' || can(PERMISSIONS.INVENTORY_EDIT))

    const dispose = (r: ExpiringSoonBag) =>
        modal.confirm({
            title: 'Lập biên bản hủy',
            content:
                r.status === 'LOCKED'
                    ? `Chuyển sang trang lập biên bản hủy cho túi ${r.barcode}?`
                    : `Túi ${r.barcode} sẽ được tạm khóa (LOCKED) rồi chuyển sang trang lập biên bản hủy. Tiếp tục?`,
            okText: 'Tiếp tục',
            cancelText: 'Hủy',
            onOk: async () => {
                try {
                    if (r.status !== 'LOCKED') {
                        await lock.mutateAsync({ barcode: r.barcode, targetStatus: 'LOCKED', notes: 'Cận hạn - đề xuất hủy' })
                    }
                    navigate(`/inventory/disposals?barcode=${encodeURIComponent(r.barcode)}`)
                } catch (e) {
                    message.error(parseApiError(e).message)
                    throw e // giữ hộp thoại mở
                }
            },
        })

    /** Xuất CSV (BOM để Excel đọc đúng tiếng Việt). */
    const exportCsv = () => {
        const head = ['Mã túi máu', 'Nhóm máu', 'Loại chế phẩm', 'Hạn sử dụng', 'Thời gian còn lại', 'Vị trí']
        const esc = (v: string) => `"${v.replace(/"/g, '""')}"`
        const lines = rows.map((r) =>
            [r.barcode, r.bloodGroup, r.componentTypeName, dayjs(r.expiredAt).format('DD/MM/YYYY HH:mm'), fmtRemaining(r.hoursRemaining), r.storageLocationName ?? '']
                .map(esc).join(','),
        )
        const blob = new Blob(['\uFEFF' + [head.map(esc).join(','), ...lines].join('\n')], { type: 'text/csv;charset=utf-8' })
        const a = document.createElement('a')
        a.href = URL.createObjectURL(blob)
        a.download = `tui-mau-can-han-${dayjs().format('YYYYMMDD-HHmm')}.csv`
        a.click()
        URL.revokeObjectURL(a.href)
    }

    return (
        <div className="inv">
            <Breadcrumb items={[{ title: 'Túi máu & Kho' }, { title: 'Cảnh báo cận hạn' }]} />
            <div className="stk-head">
                <div className="inv-title">
                    <h1>Cảnh báo túi máu cận hạn</h1>
                    <p>Danh sách được tự động sắp xếp theo mức độ khẩn cấp và thời gian còn lại.</p>
                </div>
                <Button size="large" icon={<DownloadOutlined />} disabled={rows.length === 0} onClick={exportCsv}>
                    Xuất danh sách
                </Button>
            </div>

            <div className="exp-top">
                <div className="inv-card exp-stat">
                    <span className="exp-stat__icon exp-stat__icon--red"><ExclamationOutlined /></span>
                    <div><b>{isLoading ? '—' : two(under24)}</b><small>Dưới 24 giờ</small></div>
                </div>
                <div className="inv-card exp-stat">
                    <span className="exp-stat__icon exp-stat__icon--amber"><HourglassOutlined /></span>
                    <div><b>{isLoading ? '—' : two(d1to3)}</b><small>Từ 1–3 ngày</small></div>
                </div>
                <div className="exp-threshold">
                    <BellOutlined className="exp-threshold__bell" />
                    <div>
                        <b>Ngưỡng cảnh báo đang áp dụng: {days} ngày</b>
                        <small>Ưu tiên phân bổ theo nguyên tắc FEFO</small>
                    </div>
                    <Select
                        className="exp-threshold__select"
                        value={days}
                        options={DAY_OPTIONS}
                        onChange={(v) => { setDays(v); setPage(1) }}
                        aria-label="Hết hạn trong bao nhiêu ngày"
                    />
                </div>
            </div>

            <section className="inv-card">
                <div className="inv-card__head stk-head">
                    <div>
                        <h2>Danh sách cần xử lý</h2>
                        <p>{two(rows.length)} túi máu đang cận hạn sử dụng</p>
                    </div>
                    <div className="disp-tools" style={{ marginBottom: 0 }}>
                        <Input
                            style={{ width: 240 }} size="large" prefix={<SearchOutlined />} placeholder="Tìm mã túi máu..."
                            value={keyword} allowClear onChange={(e) => { setKeyword(e.target.value); setPage(1) }}
                        />
                        <Select
                            size="large" style={{ width: 200 }} allowClear placeholder="Tất cả chế phẩm"
                            options={COMPONENT_OPTIONS} value={component} onChange={(v) => { setComponent(v); setPage(1) }}
                        />
                    </div>
                </div>

                {isLoading ? (
                    <div className="inv-card__body"><Skeleton active paragraph={{ rows: 5 }} /></div>
                ) : (
                    <Table<ExpiringSoonBag>
                        rowKey="bagId"
                        loading={isFetching && !isLoading}
                        dataSource={rows}
                        scroll={{ x: 1000 }}
                        locale={{ emptyText: `Không có túi máu nào hết hạn trong ${days} ngày tới` }}
                        rowClassName={(r) => (r.hoursRemaining < 24 ? 'exp-row-urgent' : '')}
                        pagination={{ current: page, pageSize: PAGE_SIZE, showSizeChanger: false, hideOnSinglePage: true, onChange: setPage }}
                        columns={[
                            {
                                title: 'MÃ TÚI MÁU', dataIndex: 'barcode',
                                render: (v: string) => <span className="exp-code"><i className="exp-drop" />{v}</span>,
                            },
                            { title: 'NHÓM MÁU', dataIndex: 'bloodGroup', width: 120, render: (v: string) => <BloodGroupTag value={v} /> },
                            { title: 'LOẠI CHẾ PHẨM', dataIndex: 'componentTypeName' },
                            {
                                title: 'HẠN SỬ DỤNG', dataIndex: 'expiredAt', width: 190,
                                render: (v: string) => dayjs(v).format('DD/MM/YYYY · HH:mm'),
                            },
                            {
                                title: 'THỜI GIAN CÒN LẠI', dataIndex: 'hoursRemaining', width: 190,
                                render: (h: number) => <span className={`exp-pill exp-pill--${tone(h)}`}>{fmtRemaining(h)}</span>,
                            },
                            {
                                title: 'THAO TÁC', width: 300,
                                render: (_: unknown, r) => (
                                    <div className="inv-actions" style={{ flexWrap: 'nowrap' }}>
                                        <Button
                                            className="exp-btn-prio"
                                            // TODO(sprint sau): nối API phân bổ / giữ chỗ
                                            onClick={() => message.info('Phân bổ theo FEFO sẽ có ở sprint sau')}
                                        >
                                            Ưu tiên phân bổ
                                        </Button>
                                        {canDispose(r) && (
                                            <Button danger loading={lock.isPending && lock.variables?.barcode === r.barcode} onClick={() => dispose(r)}>
                                                Lập biên bản hủy
                                            </Button>
                                        )}
                                    </div>
                                ),
                            },
                        ]}
                    />
                )}

                <div className="exp-foot">
                    <span>Cập nhật lần cuối lúc {dataUpdatedAt ? dayjs(dataUpdatedAt).format('HH:mm · DD/MM/YYYY') : '—'}</span>
                    <span className="exp-foot__live">
                        <i /> Dữ liệu thời gian thực
                        <Button type="link" size="small" icon={<ReloadOutlined />} loading={isFetching} onClick={() => refetch()}>Làm mới</Button>
                    </span>
                </div>
            </section>
        </div>
    )
}