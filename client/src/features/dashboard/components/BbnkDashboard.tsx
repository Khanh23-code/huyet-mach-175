import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import { Breadcrumb, Skeleton } from 'antd'
import { BarChartOutlined, ClockCircleOutlined, FileTextOutlined, RightOutlined } from '@ant-design/icons'
import { useCurrentUser } from '@/features/auth/useCurrentUser'
import { fmtDate } from '@/features/donors/format'
import { todayIso } from '@/features/reception/format'
import { useInventorySummary } from '@/features/inventory/stockHooks'
import { BLOOD_GROUPS, STOCK_TARGET, fmtNum, stockLevel } from '@/features/inventory/stockFormat'
import './BbnkDashboard.css'

/**
 * TODO(BE): chưa có API đặt chỗ / yêu cầu cấp cứu (sprint sau) -> dữ liệu MẪU bên dưới.
 * Thay bằng hook thật khi BE sẵn sàng.
 */
const MOCK_RESERVED = { bags: 96, requests: 12 }
const MOCK_EMERGENCY = [
    { code: 'YC-250602-041', level: 'Cấp cứu', tone: 'red', dept: 'Khoa Cấp cứu', group: 'O+', bags: 4, eta: '04:28' },
    { code: 'YC-250602-043', level: 'Cấp cứu', tone: 'red', dept: 'Hồi sức tích cực', group: 'AB-', bags: 2, eta: '08:51' },
    { code: 'YC-250602-039', level: 'Khẩn', tone: 'amber', dept: 'Khoa Ngoại', group: 'A+', bags: 2, eta: '17:20' },
]

export default function BbnkDashboard() {
    const { roleLabel } = useCurrentUser()
    const { data, isLoading } = useInventorySummary()
    const today = useMemo(todayIso, [])

    // Cộng gộp breakdown (nhóm máu x chế phẩm x trạng thái) -> số túi khả dụng theo 8 nhóm máu
    const byGroup = useMemo(() => {
        const m = new Map<string, number>()
        for (const b of data?.breakdown ?? []) {
            if (b.status === 'AVAILABLE') m.set(b.bloodGroup, (m.get(b.bloodGroup) ?? 0) + b.totalBags)
        }
        return m
    }, [data])

    const o = data?.overview

    return (
        <div className="bb">
            <div className="bb-head">
                <div>
                    <Breadcrumb items={[{ title: 'Dashboard' }, { title: 'Khoa Truyền máu' }]} />
                    <h1>Tổng quan ngân hàng máu</h1>
                    <p>Giám sát tồn kho và nhu cầu cấp cứu theo thời gian thực.</p>
                </div>
                <div className="bb-meta">
                    {roleLabel && <span className="bb-role">{roleLabel}</span>}
                    <div className="bb-date"><small>Hôm nay</small><b>{fmtDate(today)}</b></div>
                </div>
            </div>

            <div className="bb-cards">
                <div className="bb-card">
                    <div className="bb-card-top">
                        <span className="bb-icon bb-icon--green"><FileTextOutlined /></span>
                        <span className="bb-chip bb-chip--green">Sẵn sàng cấp phát</span>
                    </div>
                    <small>Túi máu khả dụng</small>
                    <b>{isLoading ? '—' : fmtNum(o?.availableBags)}</b>
                </div>
                <div className="bb-card">
                    <div className="bb-card-top">
                        <span className="bb-icon bb-icon--blue"><FileTextOutlined /></span>
                        <span className="bb-chip bb-chip--blue">{MOCK_RESERVED.requests} phiếu yêu cầu · dữ liệu mẫu</span>
                    </div>
                    <small>Đang giữ chỗ</small>
                    <b>{MOCK_RESERVED.bags}</b>
                </div>
                <div className="bb-card">
                    <div className="bb-card-top">
                        <span className="bb-icon bb-icon--amber"><ClockCircleOutlined /></span>
                        <span className="bb-chip bb-chip--amber">Cần ưu tiên FEFO</span>
                    </div>
                    <small>Cận hạn 72 giờ</small>
                    <b>{isLoading ? '—' : fmtNum(o?.expiringSoonBags)}</b>
                </div>
            </div>

            <div className="bb-main">
                <div className="bb-panel">
                    <div className="bb-panel-head">
                        <div>
                            <h2>Tồn kho theo 8 nhóm máu</h2>
                            <small>Tỷ lệ so với định mức tối ưu</small>
                        </div>
                        <Link to="/inventory/stock" className="bb-link">Chi tiết tồn kho <RightOutlined /></Link>
                    </div>

                    {isLoading ? (
                        <Skeleton active paragraph={{ rows: 5 }} />
                    ) : (
                        <div className="bb-groups">
                            {BLOOD_GROUPS.map((g) => {
                                const n = byGroup.get(g) ?? 0
                                const pct = Math.min(100, Math.round((n / (STOCK_TARGET[g] || 1)) * 100))
                                const lv = stockLevel(pct)
                                return (
                                    <div key={g} className={`bb-group bb-group--${lv}`}>
                                        <div className="bb-group-top">
                                            <span className="bb-group-tag">{g}</span>
                                            <b>{pct}%</b>
                                        </div>
                                        <div className="bb-bar"><i style={{ width: `${pct}%` }} /></div>
                                        <small>{fmtNum(n)} túi khả dụng</small>
                                    </div>
                                )
                            })}
                        </div>
                    )}
                </div>

                <div className="bb-panel">
                    <div className="bb-panel-head">
                        <div>
                            <h2>Yêu cầu cấp cứu</h2>
                            <small>Cần xử lý ngay · dữ liệu mẫu</small>
                        </div>
                    </div>
                    <ul className="bb-urgent">
                        {MOCK_EMERGENCY.map((r) => (
                            <li key={r.code}>
                                <div className="bb-urgent-top">
                                    <span className={`bb-level bb-level--${r.tone}`}>{r.level}</span>
                                    <b className={`bb-eta bb-eta--${r.tone}`}>{r.eta}</b>
                                </div>
                                <b className="bb-code">{r.code}</b>
                                <small>{r.dept} · {r.group} · {r.bags} túi</small>
                            </li>
                        ))}
                    </ul>
                    <Link to="/inventory/expiring" className="bb-link bb-link--foot"><BarChartOutlined /> Xem cảnh báo cận hạn <RightOutlined /></Link>
                </div>
            </div>
        </div>
    )
}