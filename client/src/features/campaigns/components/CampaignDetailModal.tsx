import { useState } from 'react'
import {
    App,
    Button,
    Card,
    Col,
    Descriptions,
    Divider,
    Modal,
    Progress,
    Row,
    Skeleton,
    Space,
    Statistic,
    Tag,
} from 'antd'
import {
    CheckCircleOutlined,
    CloseCircleOutlined,
    PlayCircleOutlined,
} from '@ant-design/icons'
import ConfigCard from './ConfigCard'
import CancelCampaignModal from './CancelCampaignModal'
import Can from '@/features/auth/components/Can'
import { PERMISSIONS } from '@/config/permissions'
import { useCampaignDetail, useUpdateCampaignStatus } from '../hooks'
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
import { parseApiError } from '@/services/apiError'
import type { CampaignListItem, CampaignStatus } from '../types'

interface Props {
    campaignId?: number | null
    campaign?: CampaignListItem | null
    site?: any // fallback compatibility
    onClose: () => void
}

export default function CampaignDetailModal({ campaignId, campaign, site, onClose }: Props) {
    const id = campaignId ?? campaign?.campaignId ?? site?.siteId ?? null
    const { message } = App.useApp()
    const [isCancelOpen, setIsCancelOpen] = useState(false)
    const { data: detail, isLoading } = useCampaignDetail(id)
    const updateStatusMutation = useUpdateCampaignStatus()

    const status = detail ? normalizeCampaignStatus(detail.status) : 'DRAFT'
    const campaignType = detail ? normalizeCampaignType(detail.campaignType) : 'EVENT'
    const isClosed = status === 'REJECTED' || status === 'CANCELLED'

    const handleUpdateStatus = async (newStatus: CampaignStatus, label: string) => {
        if (!id) return
        try {
            await updateStatusMutation.mutateAsync({ id, status: newStatus })
            message.success(`Đã cập nhật trạng thái chiến dịch: ${label}`)
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    return (
        <Modal
            title="Chi tiết đợt hiến máu"
            open={id !== null}
            onCancel={onClose}
            style={{ top: 24, paddingBottom: 24 }}
            styles={{
                body: {
                    maxHeight: 'calc(85vh - 110px)',
                    overflowY: 'auto',
                    paddingRight: 8,
                },
            }}
            footer={
                detail ? (
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Space>
                            <Can do={PERMISSIONS.CAMPAIGN_EDIT}>
                                {status === 'APPROVED' && (
                                    <Button
                                        type="primary"
                                        icon={<PlayCircleOutlined />}
                                        loading={updateStatusMutation.isPending}
                                        onClick={() => handleUpdateStatus('IN_PROGRESS', 'Đang diễn ra')}
                                    >
                                        Bắt đầu diễn ra
                                    </Button>
                                )}
                                {status === 'IN_PROGRESS' && (
                                    <Button
                                        type="primary"
                                        icon={<CheckCircleOutlined />}
                                        style={{ background: '#6d28d9', borderColor: '#6d28d9' }}
                                        loading={updateStatusMutation.isPending}
                                        onClick={() => handleUpdateStatus('COMPLETED', 'Đã hoàn thành')}
                                    >
                                        Hoàn thành chiến dịch
                                    </Button>
                                )}
                            </Can>
                            <Can do={PERMISSIONS.CAMPAIGN_CANCEL}>
                                {(status === 'APPROVED' || status === 'IN_PROGRESS') && (
                                    <Button
                                        danger
                                        icon={<CloseCircleOutlined />}
                                        onClick={() => setIsCancelOpen(true)}
                                    >
                                        Hủy đợt hiến
                                    </Button>
                                )}
                            </Can>
                        </Space>
                        <Button onClick={onClose}>Đóng</Button>
                    </div>
                ) : null
            }
            width={780}
            destroyOnHidden
        >
            {isLoading || !detail ? (
                <Skeleton active paragraph={{ rows: 8 }} />
            ) : (
                <>
                    {/* Thống kê tiến độ */}
                    <Card size="small" style={{ marginBottom: 16, background: '#f8fafc' }}>
                        <Row gutter={[16, 16]}>
                            <Col span={6}>
                                <Statistic
                                    title="Đã đăng ký"
                                    value={detail.totalRegistered}
                                    suffix={`/ ${detail.targetDonations}`}
                                />
                            </Col>
                            <Col span={6}>
                                <Statistic
                                    title="Đã điểm danh"
                                    value={detail.totalCheckedIn}
                                    valueStyle={{ color: '#0369a1' }}
                                />
                            </Col>
                            <Col span={6}>
                                <Statistic
                                    title="Đã thu gom (túi)"
                                    value={detail.totalCollected}
                                    valueStyle={{ color: '#1a7534' }}
                                />
                            </Col>
                            <Col span={6}>
                                <Statistic
                                    title="Còn lại"
                                    value={Math.max(0, detail.remainingDays)}
                                    suffix="ngày"
                                />
                            </Col>
                        </Row>

                        {!isClosed ? (
                            <div style={{ marginTop: 12 }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, marginBottom: 4 }}>
                                    <span>Tiến độ hoàn thành chỉ tiêu</span>
                                    <b>{detail.completionRate}%</b>
                                </div>
                                <Progress
                                    percent={Math.min(100, Math.round(detail.completionRate))}
                                    strokeColor={status === 'COMPLETED' ? '#6d28d9' : '#1a7534'}
                                />
                            </div>
                        ) : (
                            <div className="ds-closed-alert" style={{ marginTop: 12 }}>
                                <CloseCircleOutlined style={{ color: '#c4261b' }} />
                                <span>
                                    {status === 'REJECTED'
                                        ? 'Đề xuất chiến dịch này đã bị từ chối · Không mở tiếp nhận đăng ký'
                                        : `Đợt hiến máu đã bị hủy${detail.cancellationReason ? ` · Lý do: ${detail.cancellationReason}` : ' · Đã dừng tiếp nhận người hiến máu'}`}
                                </span>
                            </div>
                        )}
                    </Card>

                    <Descriptions column={2} size="small" bordered style={{ marginBottom: 16 }}>
                        <Descriptions.Item label="Tên chiến dịch" span={2}>
                            <b>{detail.campaignName}</b>
                        </Descriptions.Item>
                        <Descriptions.Item label="Hình thức">
                            <Tag className="campaign-tag">
                                {CAMPAIGN_TYPE_LABEL[campaignType]}
                            </Tag>
                        </Descriptions.Item>
                        <Descriptions.Item label="Trạng thái">
                            <Tag color={CAMPAIGN_STATUS_COLOR[status]} style={{ fontWeight: 600 }}>
                                {CAMPAIGN_STATUS_LABEL[status]}
                            </Tag>
                        </Descriptions.Item>
                        <Descriptions.Item label="Địa điểm" span={2}>
                            {detail.locationName}
                        </Descriptions.Item>
                        <Descriptions.Item label="Thời gian diễn ra">
                            {fmtDate(detail.startDate)} – {fmtDate(detail.endDate)}
                        </Descriptions.Item>
                        <Descriptions.Item label="Chỉ tiêu thu gom">
                            <b>{detail.targetDonations}</b> túi máu
                            {detail.maxCapacity ? ` (Sức chứa tối đa: ${detail.maxCapacity})` : ''}
                        </Descriptions.Item>
                        {campaignType === 'RECURRING' && detail.operatingDays && (
                            <Descriptions.Item label="Ngày định kỳ" span={2}>
                                {fmtOperatingDays(detail.operatingDays)}
                            </Descriptions.Item>
                        )}
                        <Descriptions.Item label="Người tạo">
                            {detail.createdByName}
                        </Descriptions.Item>
                        <Descriptions.Item label="Thời gian tạo">
                            {fmtDateTime(detail.createdAt)}
                        </Descriptions.Item>
                        {detail.approvedByName && (
                            <>
                                <Descriptions.Item label="Người duyệt">
                                    {detail.approvedByName}
                                </Descriptions.Item>
                                <Descriptions.Item label="Thời gian duyệt">
                                    {fmtDateTime(detail.approvedAt)}
                                </Descriptions.Item>
                            </>
                        )}
                        {detail.cancelledByName && (
                            <>
                                <Descriptions.Item label="Người hủy">
                                    {detail.cancelledByName}
                                </Descriptions.Item>
                                <Descriptions.Item label="Thời gian hủy">
                                    {fmtDateTime(detail.cancelledAt)}
                                </Descriptions.Item>
                            </>
                        )}
                        {detail.rejectionReason && (
                            <Descriptions.Item label="Lý do từ chối" span={2}>
                                <span style={{ color: '#c4261b', fontWeight: 500 }}>{detail.rejectionReason}</span>
                            </Descriptions.Item>
                        )}
                        {detail.cancellationReason && (
                            <Descriptions.Item label="Lý do hủy đợt hiến" span={2}>
                                <span style={{ color: '#c4261b', fontWeight: 500 }}>{detail.cancellationReason}</span>
                            </Descriptions.Item>
                        )}
                        {detail.description && (
                            <Descriptions.Item label="Mô tả / Ghi chú" span={2}>
                                {detail.description}
                            </Descriptions.Item>
                        )}
                    </Descriptions>

                    <Divider style={{ margin: '16px 0' }} />

                    <ConfigCard
                        label="Khung giờ & Sức chứa ca hiến"
                        title="Các slot tiếp nhận người hiến"
                        subtitle={`Tổng ${detail.slots?.length ?? 0} khung giờ`}
                        slots={detail.slots}
                        highlight
                    />
                </>
            )}

            <CancelCampaignModal
                campaign={detail ?? null}
                open={isCancelOpen}
                onClose={() => setIsCancelOpen(false)}
            />
        </Modal>
    )
}
