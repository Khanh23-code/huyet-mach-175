import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import { Skeleton } from 'antd'
import { CheckCircleOutlined, ClockCircleOutlined, RightOutlined, TeamOutlined } from '@ant-design/icons'
import { PageContainer, PageHeader, SectionCard, StatusPill, type StatusPillTone } from '@/shared/components'
import { usePermission } from '@/features/auth/usePermission'
import { useCurrentUser } from '@/features/auth/useCurrentUser'
import { PERMISSIONS } from '@/config/permissions'
import { initials } from '@/shared/utils/string'
import { fmtDate, addDaysIso } from '@/shared/formatters/date'
import { useAppointments } from '@/features/reception/hooks'
import { normalizeStatus, waitingMinutes } from '@/features/reception/format'
import { todayIso } from '@/shared/utils/date'
import { CampaignGate, useWorkCampaign, WorkCampaignBanner } from '@/features/work-campaign'
import { useScreeningQueue } from '@/features/screening/hooks'
import './ReceptionDashboard.css'

const REFRESH_MS = 15_000
const pct = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 1 })

interface WaitRow {
    id: number
    name: string
    sub: string
    label: string
    tone: StatusPillTone
    rank: number
}

function Donut({ values }: { values: { value: number; color: string }[] }) {
    const total = values.reduce((a, b) => a + b.value, 0)
    const R = 80
    const C = 2 * Math.PI * R
    let offset = 0

    return (
        <svg viewBox="0 0 200 200" className="dash-donut" role="img" aria-label="Biểu đồ kết luận sàng lọc">
            <circle cx="100" cy="100" r={R} fill="none" stroke="var(--c-border)" strokeWidth="26" />
            {total > 0 &&
                values.map((s, i) => {
                    if (s.value === 0) return null
                    const len = (s.value / total) * C
                    const el = (
                        <circle
                            key={i}
                            cx="100"
                            cy="100"
                            r={R}
                            fill="none"
                            stroke={s.color}
                            strokeWidth="26"
                            strokeDasharray={`${len} ${C - len}`}
                            strokeDashoffset={-offset}
                            transform="rotate(-90 100 100)"
                        />
                    )
                    offset += len
                    return el
                })}
        </svg>
    )
}

export default function ReceptionDashboard() {
    const { can } = usePermission()
    const wc = useWorkCampaign()
    const { roleLabel } = useCurrentUser()
    const today = useMemo(todayIso, [])
    const yesterday = useMemo(() => addDaysIso(todayIso(), -1), [])
    const canScreen = can(PERMISSIONS.SCREENING_VIEW)

    const appts = useAppointments(
        { date: today, campaignId: wc.campaignId, page: 1, pageSize: 100 },
        { enabled: !wc.blocked, refetchInterval: REFRESH_MS },
    )
    // status = 1 là CHECKED_IN (xem reception/format.ts)
    const prev = useAppointments(
        { date: yesterday, campaignId: wc.campaignId, status: 1, page: 1, pageSize: 1 },
        { enabled: !wc.blocked },
    )
    const queue = useScreeningQueue(
        { date: today, campaignId: wc.campaignId, page: 1, pageSize: 100 },
        { enabled: !wc.blocked && canScreen, refetchInterval: REFRESH_MS },
    )

    const stats = useMemo(() => {
        const items = queue.data?.items ?? []
        const pending = items.filter((i) => i.screeningStatus === 'PENDING')
        const done = items.length - pending.length
        const waits = pending
            .map((i) => waitingMinutes(i.checkedInAt))
            .filter((m): m is number => m !== null)
        return {
            checkedIn: queue.data?.totalCount ?? 0,
            waiting: pending.length,
            avgWait: waits.length ? Math.round(waits.reduce((a, b) => a + b, 0) / waits.length) : null,
            done,
            qualified: items.filter((i) => i.screeningStatus === 'QUALIFIED').length,
            deferred: items.filter((i) => i.screeningStatus === 'DEFERRED').length,
            unqualified: items.filter((i) => i.screeningStatus === 'UNQUALIFIED').length,
        }
    }, [queue.data])

    const rows: WaitRow[] = useMemo(() => {
        const byAppt = new Map((queue.data?.items ?? []).map((i) => [i.appointmentId, i.screeningStatus]))
        const out: WaitRow[] = []
        for (const a of appts.data?.items ?? []) {
            const st = normalizeStatus(a.status)
            if (st === 'CANCELLED' || st === 'NO_SHOW') continue
            const sub = a.isWalkIn ? 'Vãng lai' : `Lịch hẹn ${a.timeSlot.split('-')[0] || '—'}`
            if (st === 'BOOKED') {
                out.push({ id: a.appointmentId, name: a.donorName, sub, label: 'Chờ tiếp nhận', tone: 'warning', rank: 1 })
            } else if ((byAppt.get(a.appointmentId) ?? 'PENDING') === 'PENDING') {
                out.push({ id: a.appointmentId, name: a.donorName, sub, label: 'Chờ khám', tone: 'info', rank: 0 })
            } else {
                out.push({ id: a.appointmentId, name: a.donorName, sub, label: 'Hoàn tất', tone: 'success', rank: 2 })
            }
        }
        return out.sort((x, y) => x.rank - y.rank).slice(0, 6)
    }, [appts.data, queue.data])

    const prevCount = prev.data?.totalCount ?? 0
    const delta = prevCount > 0 ? Math.round(((stats.checkedIn - prevCount) / prevCount) * 100) : null
    const donePct = stats.checkedIn > 0 ? (stats.done / stats.checkedIn) * 100 : 0

    const header = (
        <PageHeader
            breadcrumb={[{ title: 'Dashboard' }, { title: 'Bàn tiếp đón' }]}
            title="Tổng quan tiếp đón & sàng lọc"
            subtitle="Theo dõi hoạt động người hiến trong ngày hôm nay."
            extra={
                <div className="dash-meta">
                    {roleLabel && <span className="role-chip">{roleLabel}</span>}
                    <div className="date-chip">
                        <small>Hôm nay</small>
                        <b>{fmtDate(today)}</b>
                    </div>
                </div>
            }
        />
    )

    if (wc.blocked) {
        return (
            <PageContainer>
                {header}
                <CampaignGate />
            </PageContainer>
        )
    }

    const dash = (v: number | string) => (canScreen ? v : '—')

    return (
        <PageContainer>
            {header}
            {wc.requireCampaign && wc.campaign && <WorkCampaignBanner campaign={wc.campaign} onChange={wc.clear} />}

            <div className="dash-cards">
                <div className="dash-card">
                    <div className="dash-card-top">
                        <span className="dash-icon dash-icon--green"><TeamOutlined /></span>
                        <StatusPill tone="primary" dot={false}>
                            {delta === null ? 'Chưa có dữ liệu hôm qua' : `${delta > 0 ? '+' : ''}${delta}% so hôm qua`}
                        </StatusPill>
                    </div>
                    <small>Đã check-in</small>
                    <b>{dash(stats.checkedIn)}</b>
                </div>

                <div className="dash-card">
                    <div className="dash-card-top">
                        <span className="dash-icon dash-icon--amber"><ClockCircleOutlined /></span>
                        <StatusPill tone="warning" dot={false}>
                            {stats.avgWait === null ? 'Không có người chờ' : `TB ${stats.avgWait} phút`}
                        </StatusPill>
                    </div>
                    <small>Đang chờ khám</small>
                    <b>{dash(stats.waiting)}</b>
                </div>

                <div className="dash-card">
                    <div className="dash-card-top">
                        <span className="dash-icon dash-icon--blue"><CheckCircleOutlined /></span>
                        <StatusPill tone="info" dot={false}>{pct(donePct)}% tổng lượt</StatusPill>
                    </div>
                    <small>Hoàn tất sàng lọc</small>
                    <b>{dash(stats.done)}</b>
                </div>
            </div>

            <div className="dash-main">
                <SectionCard
                    title="Hàng chờ người hiến"
                    subtitle="Cập nhật theo thời gian thực"
                    extra={<Link to="/btd/queue" className="dash-link">Mở hàng chờ <RightOutlined /></Link>}
                >
                    {appts.isLoading ? (
                        <Skeleton active paragraph={{ rows: 4 }} />
                    ) : rows.length === 0 ? (
                        <div className="dash-empty">Chưa có người hiến nào trong hàng chờ hôm nay</div>
                    ) : (
                        <ol className="dash-list">
                            {rows.map((r, i) => (
                                <li key={r.id}>
                                    <span className="dash-no">{i + 1}</span>
                                    <span className="dash-avatar">{initials(r.name)}</span>
                                    <span className="dash-who">
                                        <b>{r.name}</b>
                                        <small>{r.sub}</small>
                                    </span>
                                    <StatusPill tone={r.tone}>{r.label}</StatusPill>
                                </li>
                            ))}
                        </ol>
                    )}
                </SectionCard>

                <SectionCard
                    title="Sàng lọc hôm nay"
                    subtitle="Theo kết luận khám"
                >
                    <div className="dash-donut-wrap">
                        <Donut
                            values={[
                                { value: stats.qualified, color: 'var(--c-primary)' },
                                { value: stats.deferred, color: 'var(--c-warning)' },
                                { value: stats.unqualified, color: 'var(--c-danger)' },
                            ]}
                        />
                        <div className="dash-donut-center">
                            <b>{stats.qualified + stats.deferred + stats.unqualified}</b>
                            <small>đã khám</small>
                        </div>
                    </div>

                    <ul className="dash-legend">
                        <li><i style={{ background: 'var(--c-primary)' }} /> Đạt tiêu chuẩn <b>{dash(stats.qualified)}</b></li>
                        <li><i style={{ background: 'var(--c-warning)' }} /> Tạm hoãn <b>{dash(stats.deferred)}</b></li>
                        <li><i style={{ background: 'var(--c-danger)' }} /> Không đạt <b>{dash(stats.unqualified)}</b></li>
                    </ul>
                </SectionCard>
            </div>
        </PageContainer>
    )
}
