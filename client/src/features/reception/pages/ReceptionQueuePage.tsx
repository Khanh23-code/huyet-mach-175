import { useEffect, useMemo, useState } from 'react'
import { Avatar, Button, Input, Table, Tabs } from 'antd'
import { QrcodeOutlined, RightOutlined, SearchOutlined } from '@ant-design/icons'
import Can from '@/features/auth/components/Can'
import { PERMISSIONS } from '@/config/permissions'
import DonorDetailModal from '@/features/donors/components/DonorDetailModal'
import WalkInRegistration from '@/features/donors/components/WalkInRegistration'
import { PageContainer, PageHeader, SectionCard, StatusPill } from '@/shared/components'
import { initials } from '@/shared/utils/string'
import { fmtDate, fmtTime } from '@/shared/formatters/date'
import { useAppointments } from '../hooks'
import { normalizeStatus, STATUS_LABEL, STATUS_TONE, waitingMinutes } from '../format'
import { todayIso } from '@/shared/utils/date'
import { CampaignGate, WorkCampaignBanner, useWorkCampaign } from '@/features/work-campaign'
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
    const [detailDonorId, setDetailDonorId] = useState<string | null>(null)

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

    const header = (
        <PageHeader
            breadcrumb={[
                { title: 'Tiếp nhận & Sàng lọc' },
                { title: 'Hàng chờ tiếp nhận' },
            ]}
            title="Hàng chờ tiếp nhận"
            subtitle="Theo dõi và tiếp nhận người hiến máu theo lịch hẹn hôm nay."
            extra={
                <div className="date-chip">
                    <small>Hôm nay</small>
                    <b>{fmtDate(today)}</b>
                </div>
            }
        />
    )

    // Chưa nhập mã đợt: giao diện rỗng, chỉ có ô nhập mã
    if (blocked) {
        return (
            <PageContainer>
                {header}
                <CampaignGate />
            </PageContainer>
        )
    }

    return (
        <PageContainer>
            {header}

            {requireCampaign && workCampaign && (
                <WorkCampaignBanner campaign={workCampaign} onChange={clearCampaign} />
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

            <SectionCard noPadding>
                <div className="rq-toolbar">
                    <Tabs
                        className="rq-tabs"
                        activeKey={tab}
                        onChange={(k) => setTab(k as TabKey)}
                        items={tabItems.map((t) => ({
                            key: t.key,
                            label: (
                                <span className="rq-tab-label">
                                    {t.label}
                                    <span className="rq-tab-count">{t.n}</span>
                                </span>
                            ),
                        }))}
                    />
                    <Input
                        className="rq-search"
                        allowClear
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
                        {
                            title: 'STT',
                            width: 70,
                            render: (_, __, i) => <span className="tabular-nums">{String(i + 1).padStart(2, '0')}</span>,
                        },
                        {
                            title: 'Họ và tên',
                            render: (_, r) => (
                                <div className="rq-name">
                                    <Avatar className="rq-avatar">{initials(r.donorName)}</Avatar>
                                    <div>
                                        <b>{r.donorName}</b>
                                        <small>{r.donorCitizenId}</small>
                                    </div>
                                </div>
                            ),
                        },
                        { title: 'Giờ hẹn', render: (_, r) => <b>{r.isWalkIn ? 'Vãng lai' : r.timeSlot || '—'}</b> },
                        { title: 'Giờ check-in', render: (_, r) => <span className="tabular-nums">{fmtTime(r.checkedInAt)}</span> },
                        {
                            title: 'Thời gian chờ',
                            render: (_, r) => {
                                const m = r.status === 'CHECKED_IN' ? waitingMinutes(r.checkedInAt) : null
                                return m === null ? '—' : <span className="rq-wait">{m} phút</span>
                            },
                        },
                        {
                            title: 'Trạng thái',
                            render: (_, r) => <StatusPill tone={STATUS_TONE[r.status]}>{STATUS_LABEL[r.status]}</StatusPill>,
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
                    <span>Thời gian chờ trung bình: <b className="tabular-nums">{avgWait === null ? '—' : `${avgWait} phút`}</b></span>
                </div>
            </SectionCard>

            {requireCampaign && workCampaign && (
                <ScanCheckInModal
                    open={scanOpen}
                    campaignId={workCampaign.id}
                    onClose={() => setScanOpen(false)}
                />
            )}

            <DonorDetailModal donorId={detailDonorId} onClose={() => setDetailDonorId(null)} />
        </PageContainer>
    )
}
