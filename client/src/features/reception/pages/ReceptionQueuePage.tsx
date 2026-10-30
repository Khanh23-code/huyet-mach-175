import { useEffect, useMemo, useState } from 'react'
import { Avatar, Button, Input, Table, Tabs, Tag } from 'antd'
import { QrcodeOutlined, RightOutlined, SearchOutlined } from '@ant-design/icons'
import Can from '@/features/auth/components/Can'
import { PERMISSIONS } from '@/config/permissions'
import DonorDetailModal from '@/features/donors/components/DonorDetailModal'
import WalkInRegistration from '@/features/donors/components/WalkInRegistration'
import { fmtDate, initials } from '@/features/donors/format'
import { useAppointments } from '../hooks'
import { fmtTime, normalizeStatus, STATUS_COLOR, STATUS_LABEL, todayIso, waitingMinutes } from '../format'
import { useWorkCampaign } from '../useWorkCampaign'
import CampaignGate from '../components/CampaignGate'
import ScanCheckInModal from '../components/ScanCheckInModal'
import type { AppointmentListItem, AppointmentStatus } from '../types'
import './ReceptionQueuePage.css'

type Row = Omit<AppointmentListItem, 'status'> & { status: AppointmentStatus }
type TabKey = 'ALL' | 'BOOKED' | 'CHECKED_IN' | 'CLOSED'

const REFRESH_MS = 15_000

export default function ReceptionQueuePage() {
    const today = useMemo(todayIso, [])
    const [tab, setTab] = useState<TabKey>('ALL')
    const [keyword, setKeyword] = useState('')
    const [walkInOpen, setWalkInOpen] = useState(false)
    const [scanOpen, setScanOpen] = useState(false)
    const [detailDonorId, setDetailDonorId] = useState<number | null>(null)

    // Người có quyền thao tác (APPOINTMENTS_EDIT) bắt buộc làm việc trong 1 đợt hiến máu.
    // Người chỉ có quyền xem (MGT, CLIN) vẫn xem toàn bộ như trước.
    const {
        requireCampaign,
        campaign: workCampaign,
        campaignId,
        blocked,
        clear: clearCampaign,
    } = useWorkCampaign()

    // Tìm kiếm phía server (họ tên / CCCD / SĐT), debounce 400ms
    const [search, setSearch] = useState('')
    useEffect(() => {
        const t = setTimeout(() => setSearch(keyword.trim()), 400)
        return () => clearTimeout(t)
    }, [keyword])

    const { data, isLoading } = useAppointments(
        {
            date: today,
            campaignId,
            search: search || undefined,
            page: 1,
            pageSize: 100,
        },
        { enabled: !blocked, refetchInterval: REFRESH_MS },
    )

    const rows: Row[] = useMemo(
        () => (data?.items ?? []).map((i) => ({ ...i, status: normalizeStatus(i.status) })),
        [data],
    )

    const count = (keys: AppointmentStatus[]) => rows.filter((r) => keys.includes(r.status)).length
    const tabItems = [
        { key: 'ALL', label: 'Tất cả', n: rows.length },
        { key: 'BOOKED', label: 'Đã đặt lịch', n: count(['BOOKED']) },
        { key: 'CHECKED_IN', label: 'Đã check-in', n: count(['CHECKED_IN']) },
        { key: 'CLOSED', label: 'Hủy / Vắng', n: count(['CANCELLED', 'NO_SHOW']) },
    ]

    const visible = rows.filter((r) => {
        if (tab === 'BOOKED' && r.status !== 'BOOKED') return false
        if (tab === 'CHECKED_IN' && r.status !== 'CHECKED_IN') return false
        if (tab === 'CLOSED' && r.status !== 'CANCELLED' && r.status !== 'NO_SHOW') return false
        return true
    })

    const waits = rows
        .filter((r) => r.status === 'CHECKED_IN')
        .map((r) => waitingMinutes(r.checkedInAt))
        .filter((m): m is number => m !== null)
    const avgWait = waits.length ? Math.round(waits.reduce((a, b) => a + b, 0) / waits.length) : null

    const head = (
        <div className="rq-head">
            <div>
                <h1 className="rq-title">Hàng chờ tiếp nhận</h1>
                <p className="rq-sub">Theo dõi và tiếp nhận người hiến máu theo lịch hẹn hôm nay.</p>
            </div>
            <div className="rq-date">
                <small>Hôm nay</small>
                <b>{fmtDate(today)}</b>
            </div>
        </div>
    )

    // Chưa nhập mã đợt: giao diện rỗng, chỉ có ô nhập mã
    if (blocked) {
        return (
            <div>
                {head}
                <CampaignGate />
            </div>
        )
    }

    return (
        <div>
            {head}

            {requireCampaign && workCampaign && (
                <div className="rq-campaign">
                    <div>
                        <small>Đợt hiến máu đang làm việc</small>
                        <b>#{workCampaign.id} · {workCampaign.name}</b>
                        <span>{workCampaign.locationName}</span>
                    </div>
                    <Button onClick={clearCampaign}>Đổi đợt</Button>
                </div>
            )}

            <div className="rq-quick">
                <div className="rq-quick-icon"><QrcodeOutlined /></div>
                <div className="rq-quick-body">
                    <div className="rq-quick-eyebrow">CHECK-IN NHANH</div>
                    <div className="rq-quick-title">Quét mã QR check-in</div>
                    <div className="rq-quick-desc">
                        Quét mã trên phiếu hẹn hoặc ứng dụng của người hiến để tự động thêm vào hàng chờ.
                    </div>
                </div>
                <div className="rq-quick-actions">
                    <Can do={PERMISSIONS.APPOINTMENTS_EDIT}>
                        <Button
                            type="primary"
                            size="large"
                            icon={<QrcodeOutlined />}
                            onClick={() => setScanOpen(true)}
                        >
                            Mở máy quét
                        </Button>
                    </Can>
                    {/* Tạo lịch walk-in cần cả hai quyền: tạo hồ sơ người hiến + tạo lịch hẹn */}
                    <Can do={[PERMISSIONS.DONORS_EDIT, PERMISSIONS.APPOINTMENTS_EDIT]} all>
                        <Button size="large" onClick={() => setWalkInOpen(true)}>
                            Đăng ký tại chỗ
                        </Button>
                    </Can>
                </div>
            </div>

            {walkInOpen && (
                <Can do={[PERMISSIONS.DONORS_EDIT, PERMISSIONS.APPOINTMENTS_EDIT]} all>
                    <WalkInRegistration onClose={() => setWalkInOpen(false)} />
                </Can>
            )}

            <div className="rq-table">
                <div className="rq-toolbar">
                    <Tabs
                        activeKey={tab}
                        onChange={(k) => setTab(k as TabKey)}
                        items={tabItems.map((t) => ({
                            key: t.key,
                            label: <span>{t.label} <Tag style={{ marginInlineStart: 6 }}>{t.n}</Tag></span>,
                        }))}
                    />
                    <Input
                        allowClear
                        style={{ maxWidth: 320 }}
                        prefix={<SearchOutlined />}
                        placeholder="Tìm tên, CCCD hoặc SĐT người hiến..."
                        value={keyword}
                        onChange={(e) => setKeyword(e.target.value)}
                    />
                </div>

                <Table<Row>
                    rowKey="appointmentId"
                    loading={isLoading}
                    dataSource={visible}
                    pagination={false}
                    scroll={{ x: 800 }}
                    locale={{ emptyText: 'Chưa có người hiến nào trong hàng chờ' }}
                    columns={[
                        { title: 'STT', width: 70, render: (_, __, i) => String(i + 1).padStart(2, '0') },
                        {
                            title: 'Họ và tên',
                            render: (_, r) => (
                                <div className="rq-name">
                                    <Avatar style={{ background: 'var(--c-primary-soft)', color: 'var(--c-primary)', fontWeight: 600 }}>
                                        {initials(r.donorName)}
                                    </Avatar>
                                    <div>
                                        <b>{r.donorName}</b>
                                        <small>{r.donorCitizenId}</small>
                                    </div>
                                </div>
                            ),
                        },
                        { title: 'Giờ hẹn', render: (_, r) => <b>{r.isWalkIn ? 'Vãng lai' : r.timeSlot || '—'}</b> },
                        { title: 'Giờ check-in', render: (_, r) => fmtTime(r.checkedInAt) },
                        {
                            title: 'Thời gian chờ',
                            render: (_, r) => {
                                const m = r.status === 'CHECKED_IN' ? waitingMinutes(r.checkedInAt) : null
                                return m === null ? '—' : <span className="rq-wait">{m} phút</span>
                            },
                        },
                        {
                            title: 'Trạng thái',
                            render: (_, r) => <Tag color={STATUS_COLOR[r.status]}>{STATUS_LABEL[r.status]}</Tag>,
                        },
                        {
                            title: '',
                            width: 60,
                            render: (_, r) => (
                                <Can do={PERMISSIONS.DONORS_VIEW}>
                                    <Button
                                        type="text"
                                        icon={<RightOutlined />}
                                        aria-label={`Xem chi tiết ${r.donorName}`}
                                        onClick={() => setDetailDonorId(r.donorId)}
                                    />
                                </Can>
                            ),
                        },
                    ]}
                />

                <div className="rq-foot">
                    <span>Hiển thị {visible.length} người hiến trong hàng chờ</span>
                    <span>Thời gian chờ trung bình: <b>{avgWait === null ? '—' : `${avgWait} phút`}</b></span>
                </div>
            </div>

            {requireCampaign && workCampaign && (
                <ScanCheckInModal
                    open={scanOpen}
                    campaignId={workCampaign.id}
                    onClose={() => setScanOpen(false)}
                />
            )}

            <DonorDetailModal donorId={detailDonorId} onClose={() => setDetailDonorId(null)} />
        </div>
    )
}