import { useNavigate } from 'react-router-dom'
import { Button, Space } from 'antd'
import { BarChartOutlined } from '@ant-design/icons'
import type { WorkCampaign } from '../workCampaignStore'
import './WorkCampaign.css'

interface Props {
    campaign: WorkCampaign
    onChange: () => void
}

export default function WorkCampaignBanner({ campaign, onChange }: Props) {
    const navigate = useNavigate()

    return (
        <div className="wc-banner">
            <div className="wc-banner__info">
                <small className="wc-banner__label">Đợt hiến máu đang làm việc</small>
                <b className="wc-banner__title">#{campaign.id} - {campaign.name}</b>
                <span className="wc-banner__loc">{campaign.locationName}</span>
            </div>
            <Space wrap>
                <Button
                    icon={<BarChartOutlined />}
                    onClick={() => navigate(`/campaigns/${campaign.id}/summary`)}
                >
                    Xem tổng kết
                </Button>
                <Button onClick={onChange}>Đổi đợt</Button>
            </Space>
        </div>
    )
}
