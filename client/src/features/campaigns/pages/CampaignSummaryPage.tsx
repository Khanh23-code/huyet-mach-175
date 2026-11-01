import { useMemo, useState } from 'react'
import { useNavigate, useParams, Link } from 'react-router-dom'
import {
    Alert,
    Breadcrumb,
    Button,
    Card,
    Col,
    Empty,
    Progress,
    Row,
    Skeleton,
    Space,
    Table,
    Tabs,
    Tag,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
    AimOutlined,
    ArrowLeftOutlined,
    CalendarOutlined,
    CheckCircleOutlined,
    ClockCircleOutlined,
    EnvironmentOutlined,
    ExperimentOutlined,
    FallOutlined,
    FileExcelOutlined,
    MedicineBoxOutlined,
    PrinterOutlined,
    ReloadOutlined,
    RiseOutlined,
    TeamOutlined,
    UserOutlined,
} from '@ant-design/icons'
import ImportDonorsModal from '../components/ImportDonorsModal'
import Can from '@/features/auth/components/Can'
import { PERMISSIONS } from '@/config/permissions'
import { useCampaignSummary } from '../hooks'
import {
    CAMPAIGN_STATUS_COLOR,
    CAMPAIGN_STATUS_LABEL,
    CAMPAIGN_TYPE_LABEL,
    fmtDate,
    fmtDateTime,
    fmtOperatingDays,
    normalizeCampaignStatus,
    normalizeCampaignType,
} from '../format'
import type { CampaignIncident, CampaignSlotSummary } from '../types'
import './CampaignSummaryPage.css'

export default function CampaignSummaryPage() {
    const { id } = useParams<{ id: string }>()
    const navigate = useNavigate()
    const campaignId = id ? Number(id) : null

    const [isImportOpen, setIsImportOpen] = useState(false)
    const { data: summary, isLoading, error, refetch, isFetching } = useCampaignSummary(campaignId)

    const status = summary ? normalizeCampaignStatus(summary.status) : 'DRAFT'
    const campaignType = summary ? normalizeCampaignType(summary.campaignType) : 'EVENT'

    const slotColumns: ColumnsType<CampaignSlotSummary> = useMemo(
        () => [
            {
                title: 'Khung giờ',
                key: 'time',
                render: (_, r) => (
                    <Space>
                        <ClockCircleOutlined style={{ color: 'var(--c-text-muted, #5f7367)' }} />
                        <span style={{ fontWeight: 600 }}>
                            {r.startTime.substring(0, 5)} – {r.endTime.substring(0, 5)}
                        </span>
                    </Space>
                ),
            },
            {
                title: 'Sức chứa',
                dataIndex: 'capacity',
                key: 'capacity',
                align: 'center',
            },
            {
                title: 'Đã đăng ký',
                dataIndex: 'totalRegistered',
                key: 'totalRegistered',
                align: 'center',
            },
            {
                title: 'Đã điểm danh',
                dataIndex: 'totalCheckedIn',
                key: 'totalCheckedIn',
                align: 'center',
            },
            {
                title: 'Đã thu gom (túi)',
                dataIndex: 'totalCollected',
                key: 'totalCollected',
                align: 'center',
                render: (val) => <span style={{ fontWeight: 700, color: '#1a7534' }}>{val}</span>,
            },
            {
                title: 'Lượng máu (ml)',
                dataIndex: 'totalVolumeMl',
                key: 'totalVolumeMl',
                align: 'right',
                render: (val) => `${Number(val).toLocaleString('vi-VN')} ml`,
            },
            {
                title: 'Hiệu suất thu gom',
                key: 'rate',
                render: (_, r) => {
                    const pct = r.capacity > 0 ? Math.round((r.totalCollected / r.capacity) * 100) : 0
                    return (
                        <div style={{ minWidth: 120 }}>
                            <Progress
                                percent={pct}
                                size="small"
                                status={pct >= 100 ? 'success' : 'normal'}
                            />
                        </div>
                    )
                },
            },
        ],
        [],
    )

    const incidentColumns: ColumnsType<CampaignIncident> = useMemo(
        () => [
            {
                title: 'Mã phiên / Hẹn',
                key: 'id',
                render: (_, r) => (
                    <span style={{ fontFamily: 'monospace', fontWeight: 600 }}>
                        #{r.sessionId} (Hẹn #{r.appointmentId})
                    </span>
                ),
            },
            {
                title: 'Người hiến máu',
                key: 'donor',
                render: (_, r) => (
                    <div>
                        <div style={{ fontWeight: 600 }}>{r.donorName || 'Chưa cập nhật'}</div>
                        <div style={{ fontSize: 12, color: 'var(--c-text-muted, #5f7367)' }}>
                            CCCD: {r.donorCitizenId || 'N/A'}
                        </div>
                    </div>
                ),
            },
            {
                title: 'Thể tích dự kiến / thực tế',
                key: 'volume',
                align: 'center',
                render: (_, r) => (
                    <span>
                        {r.targetVolumeMl} / {r.actualVolumeMl !== null && r.actualVolumeMl !== undefined ? `${r.actualVolumeMl} ml` : 'Chưa xác định'}
                    </span>
                ),
            },
            {
                title: 'Mô tả sự cố / Lý do dừng',
                dataIndex: 'incidentDescription',
                key: 'incidentDescription',
                render: (text) => (
                    <span style={{ color: '#b91c1c', fontWeight: 500 }}>
                        {text || 'Không có mô tả'}
                    </span>
                ),
            },
            {
                title: 'Thời điểm ghi nhận',
                dataIndex: 'completedAt',
                key: 'completedAt',
                render: (val) => fmtDateTime(val) || 'N/A',
            },
        ],
        [],
    )

    if (isLoading) {
        return (
            <div className="summary-page">
                <Skeleton active paragraph={{ rows: 14 }} />
            </div>
        )
    }

    if (error || !summary) {
        return (
            <div className="summary-page">
                <Alert
                    type="error"
                    showIcon
                    message="Không thể tải báo cáo tổng kết"
                    description="Đợt hiến máu không tồn tại hoặc chưa bắt đầu diễn ra (chỉ xem được báo cáo khi đợt hiến ở trạng thái Đang diễn ra, Đã hoàn thành hoặc Đã hủy)."
                />
                <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/campaigns/sites')}>
                    Quay lại danh sách đợt hiến máu
                </Button>
            </div>
        )
    }

    const totalVolumeLiters = (summary.totalVolumeMl / 1000).toLocaleString('vi-VN', {
        maximumFractionDigits: 1,
    })

    const avgMlPerDonor =
        summary.totalCollectedUnits > 0
            ? Math.round(summary.totalVolumeMl / summary.totalCollectedUnits)
            : 0

    return (
        <div className="summary-page">
            <Breadcrumb
                className="no-print"
                items={[
                    { title: <Link to="/campaigns/sites">Điểm hiến máu & Lịch slot</Link> },
                    { title: `Báo cáo tổng kết đợt #${summary.campaignId}` },
                ]}
            />

            <header className="summary-header">
                <div className="summary-header-top">
                    <div className="summary-header-info">
                        <div className="summary-title-row">
                            <Button
                                className="no-print"
                                icon={<ArrowLeftOutlined />}
                                onClick={() => navigate(-1)}
                            />
                            <h1>{summary.campaignName}</h1>
                            <Tag color={CAMPAIGN_STATUS_COLOR[status]}>
                                {CAMPAIGN_STATUS_LABEL[status]}
                            </Tag>
                            <Tag>{CAMPAIGN_TYPE_LABEL[campaignType]}</Tag>
                            <span className="ds-code">#{summary.campaignId}</span>
                        </div>

                        <div className="summary-meta-row">
                            <span className="summary-meta-item">
                                <EnvironmentOutlined />
                                <span>{summary.locationName}</span>
                            </span>
                            <span className="summary-meta-item">
                                <CalendarOutlined />
                                <span>
                                    {fmtDate(summary.startDate)} – {fmtDate(summary.endDate)}
                                </span>
                            </span>
                            {campaignType === 'RECURRING' && summary.operatingDays && (
                                <span className="summary-meta-item">
                                    <ClockCircleOutlined />
                                    <span>{fmtOperatingDays(summary.operatingDays)}</span>
                                </span>
                            )}
                            <span className="summary-meta-item">
                                <UserOutlined />
                                <span>Người tạo: {summary.createdByName || 'N/A'}</span>
                            </span>
                        </div>

                        {summary.description && (
                            <p style={{ margin: 0, color: 'var(--c-text-muted, #5f7367)', fontSize: 13 }}>
                                {summary.description}
                            </p>
                        )}
                    </div>

                    <div className="summary-actions no-print">
                        <Can do={PERMISSIONS.CAMPAIGN_EDIT}>
                            {(status === 'IN_PROGRESS' || status === 'APPROVED') && (
                                <Button
                                    icon={<FileExcelOutlined />}
                                    onClick={() => setIsImportOpen(true)}
                                >
                                    Nhập danh sách Excel
                                </Button>
                            )}
                        </Can>
                        <Button
                            icon={<ReloadOutlined />}
                            onClick={() => refetch()}
                            loading={isFetching}
                        >
                            Tải lại
                        </Button>
                        <Button
                            type="primary"
                            icon={<PrinterOutlined />}
                            onClick={() => window.print()}
                        >
                            In báo cáo
                        </Button>
                    </div>
                </div>
            </header>

            <Row gutter={[16, 16]}>
                <Col xs={24} sm={12} xl={6}>
                    <div className="summary-kpi-card">
                        <div className="summary-kpi-head">
                            <span className="summary-kpi-title">Tiến độ chỉ tiêu thu gom</span>
                            <div className="summary-kpi-icon-wrap" style={{ background: '#e0f2fe' }}>
                                <AimOutlined style={{ color: '#0369a1', fontSize: 18 }} />
                            </div>
                        </div>
                        <div className="summary-kpi-value">
                            {summary.totalCollectedUnits}{' '}
                            <span style={{ fontSize: 16, fontWeight: 500, color: 'var(--c-text-muted, #5f7367)' }}>
                                / {summary.targetDonations} túi
                            </span>
                        </div>
                        <div className="summary-kpi-sub">
                            <Progress
                                percent={Math.min(100, Math.round(summary.completionRate))}
                                size="small"
                                status={summary.completionRate >= 100 ? 'success' : 'active'}
                            />
                        </div>
                        <div className="summary-kpi-foot">
                            {summary.shortfallUnits > 0 ? (
                                <span style={{ color: '#b91c1c', display: 'flex', alignItems: 'center', gap: 6 }}>
                                    <FallOutlined /> Còn thiếu: <b>{summary.shortfallUnits}</b> túi
                                </span>
                            ) : summary.exceededUnits > 0 ? (
                                <span style={{ color: '#1a7534', display: 'flex', alignItems: 'center', gap: 6 }}>
                                    <RiseOutlined /> Vượt chỉ tiêu: <b>{summary.exceededUnits}</b> túi
                                </span>
                            ) : (
                                <span style={{ color: '#1a7534', display: 'flex', alignItems: 'center', gap: 6 }}>
                                    <CheckCircleOutlined /> Đạt 100% chỉ tiêu đề ra
                                </span>
                            )}
                        </div>
                    </div>
                </Col>

                <Col xs={24} sm={12} xl={6}>
                    <div className="summary-kpi-card">
                        <div className="summary-kpi-head">
                            <span className="summary-kpi-title">Tổng thể tích máu</span>
                            <div className="summary-kpi-icon-wrap" style={{ background: '#fce7f3' }}>
                                <ExperimentOutlined style={{ color: '#be185d', fontSize: 18 }} />
                            </div>
                        </div>
                        <div className="summary-kpi-value">
                            {totalVolumeLiters}{' '}
                            <span style={{ fontSize: 16, fontWeight: 500, color: 'var(--c-text-muted, #5f7367)' }}>
                                lít
                            </span>
                        </div>
                        <div className="summary-kpi-sub">
                            Quy đổi: <b>{summary.totalVolumeMl.toLocaleString('vi-VN')}</b> ml
                        </div>
                        <div className="summary-kpi-foot">
                            <span>
                                Trung bình: <b>{avgMlPerDonor} ml</b> / túi máu
                            </span>
                        </div>
                    </div>
                </Col>

                <Col xs={24} sm={12} xl={6}>
                    <div className="summary-kpi-card">
                        <div className="summary-kpi-head">
                            <span className="summary-kpi-title">Tỷ lệ người hiến có mặt</span>
                            <div className="summary-kpi-icon-wrap" style={{ background: '#dcfce7' }}>
                                <TeamOutlined style={{ color: '#15803d', fontSize: 18 }} />
                            </div>
                        </div>
                        <div className="summary-kpi-value">
                            {summary.attendanceRate}%
                        </div>
                        <div className="summary-kpi-sub">
                            Đã điểm danh: <b>{summary.totalCheckedIn}</b> / {summary.totalRegistered} người
                        </div>
                        <div className="summary-kpi-foot">
                            <span>
                                Có hẹn: <b>{summary.scheduledCheckedIn}</b> · Vãng lai: <b>{summary.walkInCheckedIn}</b>
                            </span>
                            <span>
                                Chưa đến: <b>{summary.pendingArrival}</b> · Vắng: <b>{summary.noShowCount}</b>
                            </span>
                        </div>
                    </div>
                </Col>

                <Col xs={24} sm={12} xl={6}>
                    <div className="summary-kpi-card">
                        <div className="summary-kpi-head">
                            <span className="summary-kpi-title">Sàng lọc & An toàn</span>
                            <div className="summary-kpi-icon-wrap" style={{ background: '#fef3c7' }}>
                                <MedicineBoxOutlined style={{ color: '#b45309', fontSize: 18 }} />
                            </div>
                        </div>
                        <div className="summary-kpi-value">
                            {summary.qualifiedCount}{' '}
                            <span style={{ fontSize: 16, fontWeight: 500, color: 'var(--c-text-muted, #5f7367)' }}>
                                / {summary.totalScreened} đạt
                            </span>
                        </div>
                        <div className="summary-kpi-sub">
                            Tỷ lệ đạt lâm sàng: <b>{summary.totalScreened > 0 ? Math.round((summary.qualifiedCount / summary.totalScreened) * 100) : 0}%</b>
                        </div>
                        <div className="summary-kpi-foot">
                            <span>
                                Hoãn hiến: <b>{summary.deferredCount}</b> · Không đạt: <b>{summary.unqualifiedCount}</b>
                            </span>
                            <span style={{ color: summary.incidentCount > 0 ? '#b91c1c' : 'inherit' }}>
                                Sự cố / Hủy ca: <b>{summary.incidentCount}</b> ca ({summary.abortedSessions} ca hủy)
                            </span>
                        </div>
                    </div>
                </Col>
            </Row>

            <Row gutter={[16, 16]}>
                <Col xs={24} lg={12}>
                    <div className="summary-section-card">
                        <div className="summary-section-head">
                            <h3>Phân bổ thể tích túi máu</h3>
                            <Tag color="blue">{summary.totalCollectedUnits} túi thành công</Tag>
                        </div>
                        <div className="summary-section-body">
                            {summary.volumeBreakdown && summary.volumeBreakdown.length > 0 ? (
                                <Space direction="vertical" style={{ width: '100%' }} size="middle">
                                    {summary.volumeBreakdown.map((vol) => {
                                        const pct = summary.totalCollectedUnits > 0
                                            ? Math.round((vol.count / summary.totalCollectedUnits) * 100)
                                            : 0
                                        return (
                                            <div key={vol.volumeType}>
                                                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4, fontSize: 13 }}>
                                                    <span style={{ fontWeight: 600 }}>Túi {vol.volumeMl} ml</span>
                                                    <span>
                                                        <b>{vol.count}</b> túi ({pct}%) · {vol.totalMl.toLocaleString('vi-VN')} ml
                                                    </span>
                                                </div>
                                                <Progress
                                                    percent={pct}
                                                    strokeColor={vol.volumeMl === 450 ? '#6d28d9' : vol.volumeMl === 350 ? '#1a7534' : '#0369a1'}
                                                />
                                            </div>
                                        )
                                    })}
                                </Space>
                            ) : (
                                <Empty description="Chưa có dữ liệu thể tích túi máu" />
                            )}
                        </div>
                    </div>
                </Col>

                <Col xs={24} lg={12}>
                    <div className="summary-section-card">
                        <div className="summary-section-head">
                            <h3>Cơ cấu nhóm máu (ABO) & Yếu tố Rh</h3>
                        </div>
                        <div className="summary-section-body">
                            <div className="blood-type-grid">
                                {summary.bloodTypeBreakdown && summary.bloodTypeBreakdown.length > 0 ? (
                                    summary.bloodTypeBreakdown.map((b) => (
                                        <div key={b.bloodType} className="blood-type-item">
                                            <span className="blood-type-name">{b.bloodType}</span>
                                            <span className="blood-type-count"><b>{b.count}</b> túi</span>
                                            <span className="blood-type-pct">{b.percentage}%</span>
                                        </div>
                                    ))
                                ) : (
                                    <div style={{ gridColumn: '1 / -1' }}>
                                        <Empty description="Chưa có thông tin nhóm máu" />
                                    </div>
                                )}
                            </div>

                            <div style={{ borderTop: '1px dashed var(--c-border, #dfe8e2)', paddingTop: 14 }}>
                                <div style={{ fontSize: 13, fontWeight: 600, marginBottom: 8, color: 'var(--c-text-muted, #5f7367)' }}>
                                    Phân bố Yếu tố Rh:
                                </div>
                                <Space size="middle" wrap>
                                    {summary.rhBreakdown && summary.rhBreakdown.length > 0 ? (
                                        summary.rhBreakdown.map((r) => (
                                            <Tag key={r.rhFactor} color={r.rhFactor.includes('NEG') || r.rhFactor.includes('-') ? 'orange' : 'green'} style={{ padding: '4px 10px', fontSize: 13 }}>
                                                {r.rhFactor}: <b>{r.count}</b> túi ({r.percentage}%)
                                            </Tag>
                                        ))
                                    ) : (
                                        <span style={{ fontSize: 13, color: 'var(--c-text-muted, #5f7367)' }}>Chưa có dữ liệu</span>
                                    )}
                                </Space>
                            </div>
                        </div>
                    </div>
                </Col>

                <Col xs={24} lg={12}>
                    <div className="summary-section-card">
                        <div className="summary-section-head">
                            <h3>Phễu người hiến máu (Donation Funnel)</h3>
                        </div>
                        <div className="summary-section-body">
                            <div className="funnel-step">
                                <span className="funnel-step-name">
                                    <TeamOutlined /> 1. Đăng ký tham gia
                                </span>
                                <span className="funnel-step-value">{summary.totalRegistered} người</span>
                            </div>
                            <div className="funnel-step">
                                <span className="funnel-step-name">
                                    <UserOutlined /> 2. Có mặt tại bàn tiếp đón
                                </span>
                                <span className="funnel-step-value">
                                    {summary.totalCheckedIn} người ({summary.attendanceRate}%)
                                </span>
                            </div>
                            <div className="funnel-step">
                                <span className="funnel-step-name">
                                    <MedicineBoxOutlined /> 3. Đạt khám sàng lọc lâm sàng
                                </span>
                                <span className="funnel-step-value">
                                    {summary.qualifiedCount} người (
                                    {summary.totalCheckedIn > 0 ? Math.round((summary.qualifiedCount / summary.totalCheckedIn) * 100) : 0}%)
                                </span>
                            </div>
                            <div className="funnel-step">
                                <span className="funnel-step-name">
                                    <CheckCircleOutlined /> 4. Thu gom máu thành công
                                </span>
                                <span className="funnel-step-value">
                                    {summary.completedSessions} túi (
                                    {summary.qualifiedCount > 0 ? Math.round((summary.completedSessions / summary.qualifiedCount) * 100) : 0}%)
                                </span>
                            </div>
                        </div>
                    </div>
                </Col>

                <Col xs={24} lg={12}>
                    <div className="summary-section-card">
                        <div className="summary-section-head">
                            <h3>Lý do hoãn hiến máu phổ biến</h3>
                            <Tag color="warning">{summary.deferredCount} lượt hoãn</Tag>
                        </div>
                        <div className="summary-section-body">
                            {summary.topDeferralReasons && summary.topDeferralReasons.length > 0 ? (
                                <Space direction="vertical" style={{ width: '100%' }}>
                                    {summary.topDeferralReasons.map((r, i) => (
                                        <div key={i} className="reason-item">
                                            <span><b>#{i + 1}</b> {r.reason}</span>
                                            <Tag color="gold">{r.count} ca</Tag>
                                        </div>
                                    ))}
                                </Space>
                            ) : (
                                <div style={{ textAlign: 'center', padding: '24px 0', color: 'var(--c-text-muted, #5f7367)' }}>
                                    <CheckCircleOutlined style={{ fontSize: 28, color: '#1a7534', marginBottom: 8 }} />
                                    <div>Không ghi nhận trường hợp hoãn hiến máu nào</div>
                                </div>
                            )}
                        </div>
                    </div>
                </Col>
            </Row>

            <Card className="summary-section-card" bodyStyle={{ padding: 0 }}>
                <Tabs
                    defaultActiveKey="slots"
                    tabBarStyle={{ padding: '0 24px', margin: 0 }}
                    items={[
                        {
                            key: 'slots',
                            label: `Tiến độ theo khung giờ (${summary.slots?.length || 0} slot)`,
                            children: (
                                <div style={{ padding: 16 }}>
                                    <Table
                                        rowKey="slotId"
                                        dataSource={summary.slots || []}
                                        columns={slotColumns}
                                        pagination={false}
                                        size="middle"
                                    />
                                </div>
                            ),
                        },
                        {
                            key: 'incidents',
                            label: (
                                <span>
                                    Nhật ký sự cố & Phiên hủy ({summary.incidentCount || 0})
                                </span>
                            ),
                            children: (
                                <div style={{ padding: 16 }}>
                                    {summary.incidents && summary.incidents.length > 0 ? (
                                        <Table
                                            rowKey="sessionId"
                                            dataSource={summary.incidents}
                                            columns={incidentColumns}
                                            pagination={false}
                                            size="middle"
                                        />
                                    ) : (
                                        <div style={{ textAlign: 'center', padding: '32px 0' }}>
                                            <CheckCircleOutlined style={{ fontSize: 32, color: '#1a7534', marginBottom: 12 }} />
                                            <div style={{ fontSize: 15, fontWeight: 600 }}>
                                                Không ghi nhận sự cố hay phản ứng bất lợi nào
                                            </div>
                                            <div style={{ fontSize: 13, color: 'var(--c-text-muted, #5f7367)', marginTop: 4 }}>
                                                Toàn bộ các ca lấy máu diễn ra an toàn theo đúng quy trình chuyên môn.
                                            </div>
                                        </div>
                                    )}
                                </div>
                            ),
                        },
                    ]}
                />
            </Card>

            <ImportDonorsModal
                campaignId={campaignId}
                campaignName={summary.campaignName}
                open={isImportOpen}
                onClose={() => setIsImportOpen(false)}
            />
        </div>
    )
}
