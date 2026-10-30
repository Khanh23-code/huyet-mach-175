import { useState } from 'react'
import { Alert, Button, Input } from 'antd'
import { FieldNumberOutlined } from '@ant-design/icons'
import { campaignsApi } from '@/features/campaigns/campaignsApi'
import { parseApiError } from '@/services/apiError'
import { getCampaignBlockReason } from '../campaignRules'
import { useWorkCampaignStore } from '../workCampaignStore'
import '../pages/ReceptionQueuePage.css'

/** Chặn trang hàng chờ cho tới khi nhập mã (ID) đợt hiến máu hợp lệ. */
export default function CampaignGate() {
    const setCampaign = useWorkCampaignStore((s) => s.setCampaign)
    const [value, setValue] = useState('')
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    const submit = async () => {
        const id = Number(value)
        if (!Number.isInteger(id) || id <= 0) {
            setError('Mã đợt hiến máu phải là số nguyên dương.')
            return
        }

        setLoading(true)
        setError(null)
        try {
            const campaign = await campaignsApi.get(id)
            const reason = getCampaignBlockReason(campaign)
            if (reason) {
                setError(reason)
                return
            }
            setCampaign({
                id: campaign.campaignId,
                name: campaign.campaignName,
                locationName: campaign.locationName,
            })
        } catch (e) {
            setError(parseApiError(e).message)
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="rq-gate">
            <div className="rq-gate-icon"><FieldNumberOutlined /></div>
            <h2 className="rq-gate-title">Nhập mã đợt hiến máu để bắt đầu</h2>
            <p className="rq-gate-desc">
                Hàng chờ, quét mã check-in và đăng ký tại chỗ chỉ hoạt động trong một đợt hiến máu.
                Bạn vẫn có thể xem hồ sơ cá nhân khi chưa nhập mã.
            </p>

            <div className="rq-gate-form">
                <Input
                    size="large"
                    inputMode="numeric"
                    autoFocus
                    placeholder="Mã đợt hiến máu, ví dụ: 17"
                    value={value}
                    onChange={(e) => {
                        setValue(e.target.value.replace(/\D/g, ''))
                        setError(null)
                    }}
                    onPressEnter={submit}
                />
                <Button type="primary" size="large" loading={loading} onClick={submit}>
                    Vào đợt hiến máu
                </Button>
            </div>

            {error && <Alert type="error" showIcon title={error} style={{ marginTop: 16, textAlign: 'left' }} />}
        </div>
    )
}