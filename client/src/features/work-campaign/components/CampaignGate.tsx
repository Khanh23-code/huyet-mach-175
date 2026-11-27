import { useMemo, useState } from 'react'
import { Alert, Button, Input, Select } from 'antd'
import { FieldNumberOutlined } from '@ant-design/icons'
import { campaignsApi } from '@/features/campaigns/campaignsApi'
import { normalizeCampaignStatus } from '@/features/campaigns/format'
import { useCampaigns } from '@/features/campaigns/hooks'
import { parseApiError } from '@/services/apiError'
import { getCampaignBlockReason } from '../campaignRules'
import { useWorkCampaignStore } from '../workCampaignStore'
import './WorkCampaign.css'

const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

/** Chặn trang hàng chờ cho tới khi chọn hoặc nhập mã (ID) đợt hiến máu hợp lệ. */
export default function CampaignGate() {
    const setCampaign = useWorkCampaignStore((s) => s.setCampaign)
    const [value, setValue] = useState('')
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    const campaignsQuery = useCampaigns({ pageNumber: 1, pageSize: 50 })
    const availableOptions = useMemo(() => {
        const items = campaignsQuery.data?.items ?? []
        return items
            .filter((c) => {
                const st = normalizeCampaignStatus(c.status)
                return st === 'IN_PROGRESS' || st === 'APPROVED'
            })
            .map((c) => ({
                value: c.campaignId,
                label: `${c.campaignName} (${c.locationName})`,
            }))
    }, [campaignsQuery.data])

    const submit = async () => {
        const id = value.trim()
        if (!UUID_REGEX.test(id)) {
            setError('Vui lòng chọn đợt hiến máu từ danh sách hoặc nhập đúng mã định danh (UUID) của đợt hiến.')
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
        <div className="wc-gate">
            <div className="wc-gate__icon"><FieldNumberOutlined /></div>
            <h2 className="wc-gate__title">Chọn đợt hiến máu để bắt đầu</h2>
            <p className="wc-gate__desc">
                Hàng chờ, quét mã check-in và đăng ký tại chỗ hoạt động theo từng đợt hiến máu.
                Bạn có thể chọn nhanh đợt đang mở hoặc dán mã định danh của đợt.
            </p>

            {availableOptions.length > 0 && (
                <div style={{ width: '100%', maxWidth: 460, marginBottom: 12 }}>
                    <Select
                        size="large"
                        style={{ width: '100%' }}
                        showSearch
                        optionFilterProp="label"
                        placeholder="Chọn nhanh đợt hiến máu đang mở..."
                        loading={campaignsQuery.isLoading}
                        options={availableOptions}
                        value={availableOptions.some((o) => o.value === value) ? value : undefined}
                        onChange={(selectedId) => {
                            setValue(selectedId)
                            setError(null)
                        }}
                    />
                </div>
            )}

            <div className="wc-gate__form">
                <Input
                    className="wc-gate__input"
                    size="large"
                    autoFocus={availableOptions.length === 0}
                    placeholder="Hoặc dán mã đợt hiến máu (UUID)..."
                    value={value}
                    onChange={(e) => {
                        setValue(e.target.value)
                        setError(null)
                    }}
                    onPressEnter={submit}
                />
                <Button type="primary" size="large" loading={loading} onClick={submit}>
                    Vào đợt hiến máu
                </Button>
            </div>

            {error && <Alert className="wc-gate__alert" type="error" showIcon title={error} />}
        </div>
    )
}
