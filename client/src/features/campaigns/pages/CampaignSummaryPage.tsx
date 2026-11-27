import { useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
    Alert,
    Button,
    Col,
    Empty,
    Progress,
    Row,
    Skeleton,
    Space,
    Table,
    Tabs,
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
import {
    BloodGroupTag,
    PageContainer,
    PageHeader,
    SectionCard,
    StatusPill,
} from '@/shared/components'
import ImportDonorsModal from '../components/ImportDonorsModal'
import Can from '@/features/auth/components/Can'
import { PERMISSIONS } from '@/config/permissions'
import { useCampaignSummary } from '../hooks'
import {
    CAMPAIGN_STATUS_LABEL,
    CAMPAIGN_STATUS_TONE,
    CAMPAIGN_TYPE_LABEL,
    CAMPAIGN_TYPE_TONE,
    fmtDate,
    fmtDateTime,
    fmtOperatingDays,
    normalizeCampaignStatus,
    normalizeCampaignType,
} from '../format'
import type { CampaignIncident, CampaignSlotSummary } from '../types'
import './CampaignPage.css'
import './CampaignSummaryPage.css'

export default function CampaignSummaryPage() {
    const { id } = useParams<{ id: string }>()
    const navigate = useNavigate()
    const campaignId = id?.trim() || null

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
                        <ClockCircleOutlined style={{ color: 'var(--c-text-muted)' }} />
                        <span style={{ fontWeight: 600 }} className="tabular-nums">
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
                render: (val) => (
                    <span className="tabular-nums" style={{ fontWeight: 700, color: 'var(--c-primary)' }}>
                        {val}
                    </span>
                ),
            },
            {
                title: 'Lượng máu (ml)',
                dataIndex: 'totalVolumeMl',
                key: 'totalVolumeMl',
                align: 'right',
                render: (val) => (
                    <span className="tabular-nums">
                        {`${Number(val).toLocaleString('vi-VN')} ml`}
                    </span>
                ),
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
                    <span className="text-mono" style={{ fontWeight: 600 }} title={`Phiên: ${r.sessionId} / Lịch hẹn: ${r.appointmentId}`}>
                        #{r.sessionId.slice(0, 8)} (Hẹn #{r.appointmentId.slice(0, 8)})
                    </span>
                ),
            },
            {
                title: 'Người hiến máu',
                key: 'donor',
                render: (_, r) => (
                    <div>
                        <div style={{ fontWeight: 600 }}>{r.donorName || 'Chưa cập nhật'}</div>
                        <div className="ds-muted ds-small">
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
                    <span className="tabular-nums">
                        {r.targetVolumeMl} / {r.actualVolumeMl !== null && r.actualVolumeMl !== undefined ? `${r.actualVolumeMl} ml` : 'Chưa xác định'}
                    </span>
                ),
            },
            {
                title: 'Mô tả sự cố / Lý do dừng',
                dataIndex: 'incidentDescription',
                key: 'incidentDescription',
                render: (text) => (
                    <span style={{ color: 'var(--c-danger)', fontWeight: 500 }}>
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
            <PageContainer className="summary-page">
                <Skeleton active paragraph={{ rows: 14 }} />
            </PageContainer>
        )
    }

    if (error || !summary) {
        return (
            <PageContainer className="summary-page">
                <Alert
                    type="error"
                    showIcon
                    message="Không thể tải báo cáo tổng kết"
                    description="Đợt hiến máu không tồn tại hoặc chưa bắt đầu diễn ra (chỉ xem được báo cáo khi đợt hiến ở trạng thái Đang diễn ra, Đã hoàn thành hoặc Đã hủy)."
                />
                <div>
                    <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/campaigns/sites')}>
                        Quay lại danh sách đợt hiến máu
                    </Button>
                </div>
            </PageContainer>
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
        <PageContainer className="summary-page">
            <PageHeader
                breadcrumb={[
                    { title: 'Điểm hiến máu & Lịch slot', path: '/campaigns/sites' },
                    { title: 'Báo cáo tổng kết đợt hiến máu' },
                ]}
                backTo={-1}
                title={summary.campaignName}
                titleExtra={
                    <>
                        <StatusPill
                            tone={CAMPAIGN_STATUS_TONE[status]}
                            pulse={status === 'IN_PROGRESS'}
                        >
                            {CAMPAIGN_STATUS_LABEL[status]}
                        </StatusPill>
                        <StatusPill tone={CAMPAIGN_TYPE_TONE[campaignType]} dot={false}>
                            {CAMPAIGN_TYPE_LABEL[campaignType]}
                        </StatusPill>
                        <span className="ds-code" title={summary.campaignId}>#{summary.campaignId.slice(0, 8)}</span>
                    </>
                }
                subtitle={
                    <div>
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
                            <p className="summary-description">{summary.description}</p>
                        )}
                    </div>
                }
                extra={
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
                }
            />

            <Row gutter={[16, 16]}>
                <Col xs={24} sm={12} xl={6}>
                    <div className="summary-kpi-card">
                        <div className="summary-kpi-head">
                            <span className="summary-kpi-title">Tiến độ chỉ tiêu thu gom</span>
                            <div className="summary-kpi-icon-wrap summary-kpi-icon-wrap--info">
                                <AimOutlined />
                            </div>
                        </div>
                        <div className="summary-kpi-value">
                            {summary.totalCollectedUnits}{' '}
                            <span className="summary-kpi-unit">
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
                                <span className="summary-kpi-trend summary-kpi-trend--danger">
                                    <FallOutlined /> Còn thiếu: <b>{summary.shortfallUnits}</b> túi
                                </span>
                            ) : summary.exceededUnits > 0 ? (
                                <span className="summary-kpi-trend summary-kpi-trend--success">
                                    <RiseOutlined /> Vượt chỉ tiêu: <b>{summary.exceededUnits}</b> túi
                                </span>
                            ) : (
                                <span className="summary-kpi-trend summary-kpi-trend--success">
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
                            <div className="summary-kpi-icon-wrap summary-kpi-icon-wrap--blood">
                                <ExperimentOutlined />
                            </div>
                        </div>
                        <div className="summary-kpi-value">
                            {totalVolumeLiters}{' '}
                            <span className="summary-kpi-unit">lít</span>
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
                            <div className="summary-kpi-icon-wrap summary-kpi-icon-wrap--success">
                                <TeamOutlined />
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
                                Có hẹn: <b>{summary.scheduledCheckedIn}</b> - Vãng lai: <b>{summary.walkInCheckedIn}</b>
                            </span>
                            <span>
                                Chưa đến: <b>{summary.pendingArrival}</b> - Vắng: <b>{summary.noShowCount}</b>
                            </span>
                        </div>
                    </div>
                </Col>

                <Col xs={24} sm={12} xl={6}>
                    <div className="summary-kpi-card">
                        <div className="summary-kpi-head">
                            <span className="summary-kpi-title">Sàng lọc & An toàn</span>
                            <div className="summary-kpi-icon-wrap summary-kpi-icon-wrap--warning">
                                <MedicineBoxOutlined />
                            </div>
                        </div>
                        <div className="summary-kpi-value">
                            {summary.qualifiedCount}{' '}
                            <span className="summary-kpi-unit">
                                / {summary.totalScreened} đạt
                            </span>
                        </div>
                        <div className="summary-kpi-sub">
                            Tỷ lệ đạt lâm sàng: <b>{summary.totalScreened > 0 ? Math.round((summary.qualifiedCount / summary.totalScreened) * 100) : 0}%</b>
                        </div>
                        <div className="summary-kpi-foot">
                            <span>
                                Hoãn hiến: <b>{summary.deferredCount}</b> - Không đạt: <b>{summary.unqualifiedCount}</b>
                            </span>
                            <span style={{ color: summary.incidentCount > 0 ? 'var(--c-danger)' : 'inherit' }}>
                                Sự cố / Hủy ca: <b>{summary.incidentCount}</b> ca ({summary.abortedSessions} ca hủy)
                            </span>
                        </div>
                    </div>
                </Col>
            </Row>

            <Row gutter={[16, 16]}>
                <Col xs={24} lg={12}>
                    <SectionCard
                        className="summary-section-card"
                        title="Phân bổ thể tích túi máu"
                        extra={
                            <StatusPill tone="info" dot={false}>
                                {summary.totalCollectedUnits} túi thành công
                            </StatusPill>
                        }
                    >
                        {summary.volumeBreakdown && summary.volumeBreakdown.length > 0 ? (
                            <Space direction="vertical" style={{ width: '100%' }} size="middle">
                                {summary.volumeBreakdown.map((vol) => {
                                    const pct = summary.totalCollectedUnits > 0
                                        ? Math.round((vol.count / summary.totalCollectedUnits) * 100)
                                        : 0
                                    return (
                                        <div key={vol.volumeType}>
                                            <div className="summary-volume-row">
                                                <span style={{ fontWeight: 600 }}>Túi {vol.volumeMl} ml</span>
                                                <span>
                                                    <b>{vol.count}</b> túi ({pct}%) - {vol.totalMl.toLocaleString('vi-VN')} ml
                                                </span>
                                            </div>
                                            <Progress
                                                percent={pct}
                                                strokeColor={
                                                    vol.volumeMl === 450
                                                        ? 'var(--c-purple)'
                                                        : vol.volumeMl === 350
                                                            ? 'var(--c-primary)'
                                                            : 'var(--c-info)'
                                                }
                                            />
                                        </div>
                                    )
                                })}
                            </Space>
                        ) : (
                            <Empty description="Chưa có dữ liệu thể tích túi máu" />
                        )}
                    </SectionCard>
                </Col>

                <Col xs={24} lg={12}>
                    <SectionCard
                        className="summary-section-card"
                        title="Cơ cấu nhóm máu (ABO) & Yếu tố Rh"
                    >
                        <div className="blood-type-grid">
                            {summary.bloodTypeBreakdown && summary.bloodTypeBreakdown.length > 0 ? (
                                summary.bloodTypeBreakdown.map((b) => (
                                    <div key={b.bloodType} className="blood-type-item">
                                        <BloodGroupTag value={b.bloodType} size="lg" />
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

                        <div className="summary-rh-section">
                            <div className="summary-rh-title">
                                Phân bố Yếu tố Rh:
                            </div>
                            <Space size="middle" wrap>
                                {summary.rhBreakdown && summary.rhBreakdown.length > 0 ? (
                                    summary.rhBreakdown.map((r) => (
                                        <StatusPill
                                            key={r.rhFactor}
                                            tone={r.rhFactor.includes('NEG') || r.rhFactor.includes('-') ? 'urgent' : 'success'}
                                            dot={false}
                                        >
                                            {r.rhFactor}: <b>{r.count}</b> túi ({r.percentage}%)
                                        </StatusPill>
                                    ))
                                ) : (
                                    <span className="ds-muted ds-small">Chưa có dữ liệu</span>
                                )}
                            </Space>
                        </div>
                    </SectionCard>
                </Col>

                <Col xs={24} lg={12}>
                    <SectionCard
                        className="summary-section-card"
                        title="Phễu người hiến máu (Donation Funnel)"
                    >
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
                    </SectionCard>
                </Col>

                <Col xs={24} lg={12}>
                    <SectionCard
                        className="summary-section-card"
                        title="Lý do hoãn hiến máu phổ biến"
                        extra={
                            <StatusPill tone="warning" dot={false}>
                                {summary.deferredCount} lượt hoãn
                            </StatusPill>
                        }
                    >
                        {summary.topDeferralReasons && summary.topDeferralReasons.length > 0 ? (
                            <Space direction="vertical" style={{ width: '100%' }}>
                                {summary.topDeferralReasons.map((r, i) => (
                                    <div key={i} className="reason-item">
                                        <span><b>#{i + 1}</b> {r.reason}</span>
                                        <StatusPill tone="warning" dot={false}>
                                            {r.count} ca
                                        </StatusPill>
                                    </div>
                                ))}
                            </Space>
                        ) : (
                            <div className="summary-empty-state">
                                <CheckCircleOutlined className="summary-empty-state__icon" />
                                <div>Không ghi nhận trường hợp hoãn hiến máu nào</div>
                            </div>
                        )}
                    </SectionCard>
                </Col>
            </Row>

            <SectionCard className="summary-section-card" noPadding>
                <Tabs
                    defaultActiveKey="slots"
                    tabBarStyle={{ padding: '0 24px', margin: 0 }}
                    items={[
                        {
                            key: 'slots',
                            label: `Tiến độ theo khung giờ (${summary.slots?.length || 0} slot)`,
                            children: (
                                <div className="summary-tab-body">
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
                                <div className="summary-tab-body">
                                    {summary.incidents && summary.incidents.length > 0 ? (
                                        <Table
                                            rowKey="sessionId"
                                            dataSource={summary.incidents}
                                            columns={incidentColumns}
                                            pagination={false}
                                            size="middle"
                                        />
                                    ) : (
                                        <div className="summary-empty-state">
                                            <CheckCircleOutlined className="summary-empty-state__icon" />
                                            <div className="summary-empty-state__title">
                                                Không ghi nhận sự cố hay phản ứng bất lợi nào
                                            </div>
                                            <div className="ds-muted ds-small" style={{ marginTop: 4 }}>
                                                Toàn bộ các ca lấy máu diễn ra an toàn theo đúng quy trình chuyên môn.
                                            </div>
                                        </div>
                                    )}
                                </div>
                            ),
                        },
                    ]}
                />
            </SectionCard>

            <ImportDonorsModal
                campaignId={campaignId}
                campaignName={summary.campaignName}
                open={isImportOpen}
                onClose={() => setIsImportOpen(false)}
            />
        </PageContainer>
    )
}
