import { useState } from 'react'
import { Alert, App, Button, Card, Input, Modal, Space } from 'antd'
import { ExclamationCircleOutlined } from '@ant-design/icons'
import { useCancelCampaign } from '../hooks'
import { fmtDate, normalizeCampaignType } from '../format'
import { parseApiError } from '@/services/apiError'
import type { CampaignDetail, CampaignListItem } from '../types'

interface Props {
    campaign: CampaignListItem | CampaignDetail | null
    open: boolean
    onClose: () => void
    onSuccess?: () => void
}

export default function CancelCampaignModal({ campaign, open, onClose, onSuccess }: Props) {
    const { message } = App.useApp()
    const [reason, setReason] = useState('')
    const [error, setError] = useState<string | null>(null)
    const cancelMutation = useCancelCampaign()

    if (!campaign) return null

    const handleConfirm = async () => {
        const trimmed = reason.trim()
        if (trimmed.length < 5) {
            setError('Lý do hủy là bắt buộc và phải có ít nhất 5 ký tự.')
            return
        }
        if (trimmed.length > 255) {
            setError('Lý do hủy không được vượt quá 255 ký tự.')
            return
        }

        try {
            await cancelMutation.mutateAsync({
                id: campaign.campaignId,
                reason: trimmed,
            })
            message.success(`Đã hủy đợt hiến máu "${campaign.campaignName}" thành công.`)
            setReason('')
            setError(null)
            onSuccess?.()
            onClose()
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    const handleCancel = () => {
        setReason('')
        setError(null)
        onClose()
    }

    const cType = normalizeCampaignType(campaign.campaignType)

    return (
        <Modal
            title={
                <Space style={{ color: '#b91c1c' }}>
                    <ExclamationCircleOutlined />
                    <span>Xác nhận Hủy đợt hiến máu</span>
                </Space>
            }
            open={open}
            onCancel={handleCancel}
            destroyOnHidden
            centered
            maskClosable={!cancelMutation.isPending}
            footer={
                <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, paddingTop: 12 }}>
                    <Button onClick={handleCancel} disabled={cancelMutation.isPending}>
                        Quay lại
                    </Button>
                    <Button
                        type="primary"
                        danger
                        loading={cancelMutation.isPending}
                        onClick={handleConfirm}
                    >
                        Xác nhận Hủy đợt hiến
                    </Button>
                </div>
            }
            width={580}
        >
            <Alert
                type="error"
                showIcon
                message="Cảnh báo dừng đợt hiến"
                description="Thao tác này dành riêng cho Lãnh đạo bệnh viện. Đợt hiến máu sẽ dừng tiếp nhận ngay lập tức và không thể kích hoạt lại."
                style={{ marginBottom: 16 }}
            />

            <Card size="small" style={{ marginBottom: 16, background: '#f8fafc' }}>
                <div style={{ fontWeight: 600, fontSize: 15, marginBottom: 4 }}>
                    {campaign.campaignName}
                </div>
                <div className="ds-muted ds-small">
                    <b>Địa điểm:</b> {campaign.locationName}
                </div>
                <div className="ds-muted ds-small">
                    <b>Thời gian:</b> {fmtDate(campaign.startDate)} – {fmtDate(campaign.endDate)} ({cType === 'EVENT' ? 'Chiến dịch sự kiện' : 'Điểm hiến định kỳ'})
                </div>
                <div className="ds-muted ds-small">
                    <b>Số người đã đăng ký:</b> {campaign.totalRegistered} người
                </div>
            </Card>

            <div style={{ marginBottom: 28 }}>
                <label style={{ fontWeight: 600, display: 'block', marginBottom: 6 }}>
                    Lý do hủy đợt hiến máu <span style={{ color: '#c4261b' }}>*</span>
                </label>
                <Input.TextArea
                    rows={4}
                    placeholder="Nhập lý do chi tiết hủy đợt hiến (tối thiểu 5 ký tự, ví dụ: Sự cố kỹ thuật cơ sở vật chất, thay đổi kế hoạch khẩn cấp...)"
                    value={reason}
                    maxLength={255}
                    showCount
                    onChange={(e) => {
                        setReason(e.target.value)
                        if (error) setError(null)
                    }}
                    status={error ? 'error' : ''}
                />
                {error && (
                    <div style={{ color: '#c4261b', fontSize: 13, marginTop: 8 }}>
                        {error}
                    </div>
                )}
            </div>
        </Modal>
    )
}
