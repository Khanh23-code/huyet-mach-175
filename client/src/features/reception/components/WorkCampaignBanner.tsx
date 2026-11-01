import { useNavigate } from 'react-router-dom'
import { Button, Space } from 'antd'
import { BarChartOutlined } from '@ant-design/icons'
import type { WorkCampaign } from '../workCampaignStore'
import '../pages/ReceptionQueuePage.css'

interface Props {
    campaign: WorkCampaign
    onChange: () => void
}

export default function WorkCampaignBanner({ campaign, onChange }: Props) {
    const navigate = useNavigate()

    return (
        <div className="rq-campaign">
            <div>
                <small>Đợt hiến máu đang làm việc</small>
                <b>#{campaign.id} · {campaign.name}</b>
                <span>{campaign.locationName}</span>
            </div>
            <Space>
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