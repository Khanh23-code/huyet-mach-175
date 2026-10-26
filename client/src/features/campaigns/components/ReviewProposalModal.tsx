import { useState } from 'react'
import { App, Button, Descriptions, Input, Modal, Skeleton, Tag } from 'antd'
import ConfigCard from './ConfigCard'
import Can from '@/features/auth/components/Can'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { useApproveCampaign, useCampaignDetail } from '../hooks'
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
import './ReviewProposalModal.css'

interface Props {
    campaignId?: number | null
    proposalId?: number | null
    onClose: () => void
}

export default function ReviewProposalModal({ campaignId, proposalId, onClose }: Props) {
    const id = campaignId ?? proposalId ?? null
    const { message, modal } = App.useApp()
    const { data: campaign, isLoading } = useCampaignDetail(id)

    const [rejecting, setRejecting] = useState(false)
    const [reason, setReason] = useState('')
    const [reasonError, setReasonError] = useState('')

    const approveMutation = useApproveCampaign()

    const close = () => {
        setRejecting(false)
        setReason('')
        setReasonError('')
        onClose()
    }

    const handleApprove = async () => {
        if (!id) return
        try {
            const result = await approveMutation.mutateAsync({
                id,
                body: { isApproved: true },
            })
            close()
            modal.success({
                title: 'Phê duyệt đợt hiến máu thành công',
                content: `Chiến dịch "${result.campaignName}" đã được phê duyệt và sẵn sàng triển khai.`,
                okText: 'Đóng',
            })
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    const handleReject = async () => {
        if (!id) return
        const r = reason.trim()
        if (r.length < 5) {
            setReasonError('Vui lòng nhập lý do từ chối (tối thiểu 5 ký tự)')
            return
        }
        if (r.length > 255) {
            setReasonError('Lý do tối đa 255 ký tự')
            return
        }

        try {
            const result = await approveMutation.mutateAsync({
                id,
                body: { isApproved: false, rejectionReason: r },
            })
            close()
            modal.info({
                title: 'Đã từ chối đề xuất đợt hiến máu',
                content: `Đề xuất "${result.campaignName}" đã bị từ chối. Lý do từ chối đã được gửi đến người khởi tạo.`,
                okText: 'Đóng',
            })
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    const busy = approveMutation.isPending

    return (
        <Modal
            className="review-proposal-modal"
            title="Xem xét & phê duyệt đợt hiến máu"
            open={id !== null}
            onCancel={close}
            width={760}
            destroyOnHidden
            maskClosable={!busy}
            footer={
                <div className="ds-modal-foot">
                    <Button onClick={close} disabled={busy}>
                        Đóng
                    </Button>
                    {campaign && !rejecting && (
                        <>
                            <Can do={PERMISSIONS.CAMPAIGN_APPROVE}>
                                <Button danger onClick={() => setRejecting(true)} disabled={busy}>
                                    Từ chối
                                </Button>
                                <Button
                                    type="primary"
                                    loading={approveMutation.isPending}
                                    onClick={handleApprove}
                                >
                                    Phê duyệt đợt hiến
                                </Button>
                            </Can>
                        </>
                    )}
                    {campaign && rejecting && (
                        <>
                            <Button
                                onClick={() => {
                                    setRejecting(false)
                                    setReasonError('')
                                }}
                                disabled={busy}
                            >
                                Quay lại
                            </Button>
                            <Button
                                danger
                                type="primary"
                                loading={approveMutation.isPending}
                                onClick={handleReject}
                            >
                                Xác nhận từ chối
                            </Button>
                        </>
                    )}
                </div>
            }
        >
            {isLoading || !campaign ? (
                <Skeleton active paragraph={{ rows: 6 }} />
            ) : (
                <>
                    {(() => {
                        const cType = normalizeCampaignType(campaign.campaignType)
                        const cStatus = normalizeCampaignStatus(campaign.status)
                        return (
                            <Descriptions column={2} size="small" bordered style={{ marginBottom: 16 }}>
                                <Descriptions.Item label="Tên chiến dịch" span={2}>
                                    <b>{campaign.campaignName}</b>
                                </Descriptions.Item>
                                <Descriptions.Item label="Hình thức">
                                    <Tag color={cType === 'EVENT' ? 'blue' : 'orange'}>
                                        {CAMPAIGN_TYPE_LABEL[cType]}
                                    </Tag>
                                </Descriptions.Item>
                                <Descriptions.Item label="Trạng thái">
                                    <Tag color={CAMPAIGN_STATUS_COLOR[cStatus]} style={{ fontWeight: 600 }}>
                                        {CAMPAIGN_STATUS_LABEL[cStatus]}
                                    </Tag>
                                </Descriptions.Item>
                                <Descriptions.Item label="Địa điểm" span={2}>
                                    {campaign.locationName}
                                </Descriptions.Item>
                                <Descriptions.Item label="Thời gian">
                                    {fmtDate(campaign.startDate)} – {fmtDate(campaign.endDate)}
                                </Descriptions.Item>
                                <Descriptions.Item label="Chỉ tiêu">
                                    <b>{campaign.targetDonations}</b> túi máu
                                    {campaign.maxCapacity ? ` (Tối đa: ${campaign.maxCapacity})` : ''}
                                </Descriptions.Item>
                                {cType === 'RECURRING' && campaign.operatingDays && (
                                    <Descriptions.Item label="Ngày định kỳ" span={2}>
                                        {fmtOperatingDays(campaign.operatingDays)}
                                    </Descriptions.Item>
                                )}
                                <Descriptions.Item label="Người tạo">
                                    {campaign.createdByName}
                                </Descriptions.Item>
                                <Descriptions.Item label="Thời gian gửi">
                                    {fmtDateTime(campaign.createdAt)}
                                </Descriptions.Item>
                                {campaign.description && (
                                    <Descriptions.Item label="Ghi chú" span={2}>
                                        {campaign.description}
                                    </Descriptions.Item>
                                )}
                            </Descriptions>
                        )
                    })()}

                    <div className="ds-compare">
                        <ConfigCard
                            label="Khung giờ & Sức chứa ca hiến"
                            title="Các slot tiếp nhận người hiến"
                            subtitle={`Tổng ${campaign.slots?.length ?? 0} khung giờ`}
                            slots={campaign.slots}
                            highlight
                        />
                    </div>

                    {rejecting && (
                        <div className="ds-field" style={{ marginTop: 16 }}>
                            <label>
                                Lý do từ chối <span className="ds-req">*</span>
                            </label>
                            <Input.TextArea
                                className="reject-reason-input"
                                rows={3}
                                maxLength={255}
                                showCount
                                value={reason}
                                status={reasonError ? 'error' : ''}
                                placeholder="Nêu rõ lý do để người tạo chỉnh sửa và gửi lại..."
                                onChange={(e) => {
                                    setReason(e.target.value)
                                    if (reasonError) setReasonError('')
                                }}
                            />
                            {reasonError && <div className="ds-field__error">{reasonError}</div>}
                        </div>
                    )}
                </>
            )}
        </Modal>
    )
}
