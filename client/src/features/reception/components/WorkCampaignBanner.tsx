import { Button } from 'antd'
import type { WorkCampaign } from '../workCampaignStore'
import '../pages/ReceptionQueuePage.css'

interface Props {
    campaign: WorkCampaign
    onChange: () => void
}

export default function WorkCampaignBanner({ campaign, onChange }: Props) {
    return (
        <div className="rq-campaign">
            <div>
                <small>Đợt hiến máu đang làm việc</small>
                <b>#{campaign.id} · {campaign.name}</b>
                <span>{campaign.locationName}</span>
            </div>
            <Button onClick={onChange}>Đổi đợt</Button>
        </div>
    )
}