import type { KeyboardEvent } from 'react'
import { Progress, Tag } from 'antd'
import { CalendarOutlined, SyncOutlined } from '@ant-design/icons'
import SlotsTable from './SlotsTable'
import {
    CAMPAIGN_STATUS_COLOR,
    CAMPAIGN_STATUS_LABEL,
    CAMPAIGN_TYPE_LABEL,
    fmtDate,
    fmtOperatingDays,
} from '../format'
import type { CampaignListItem } from '../types'

interface Props {
    campaign: CampaignListItem
    onOpen: (campaign: CampaignListItem) => void
}

export default function CampaignCard({ campaign, onOpen }: Props) {
    const onKey = (e: KeyboardEvent) => {
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault()
            onOpen(campaign)
        }
    }

    const percent = campaign.targetDonations > 0
        ? Math.min(100, Math.round((campaign.totalRegistered / campaign.targetDonations) * 100))
        : 0

    return (
        <div
            className="ds-site"
            role="button"
            tabIndex={0}
            onClick={() => onOpen(campaign)}
            onKeyDown={onKey}
        >
            <div className="ds-site__top">
                <Tag color={campaign.campaignType === 'EVENT' ? 'blue' : 'orange'}>
                    {CAMPAIGN_TYPE_LABEL[campaign.campaignType]}
                </Tag>
                <Tag color={CAMPAIGN_STATUS_COLOR[campaign.status]}>
                    {CAMPAIGN_STATUS_LABEL[campaign.status]}
                </Tag>
            </div>

            <h4>{campaign.campaignName}</h4>
            <p className="ds-site__addr">{campaign.locationName}</p>

            <div className="ds-site__meta">
                <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                    <CalendarOutlined style={{ color: 'var(--ds-muted)' }} />
                    <span>{fmtDate(campaign.startDate)} – {fmtDate(campaign.endDate)}</span>
                </div>
                {campaign.campaignType === 'RECURRING' && campaign.operatingDays && (
                    <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 4 }}>
                        <SyncOutlined style={{ color: 'var(--ds-muted)' }} />
                        <span>{fmtOperatingDays(campaign.operatingDays)}</span>
                    </div>
                )}
            </div>

            <div className="ds-progress-wrap">
                <div className="ds-progress-labels">
                    <span>Đã đăng ký: <b>{campaign.totalRegistered}</b> / {campaign.targetDonations}</span>
                    <span>{percent}%</span>
                </div>
                <Progress
                    percent={percent}
                    showInfo={false}
                    strokeColor={campaign.status === 'COMPLETED' ? '#6d28d9' : '#1a7534'}
                    size="small"
                />
                <div className="ds-muted ds-small" style={{ marginTop: 2 }}>
                    Đã thu gom: <b>{campaign.totalCollected}</b> túi máu
                </div>
            </div>

            {campaign.slots && campaign.slots.length > 0 && (
                <SlotsTable slots={campaign.slots} />
            )}
        </div>
    )
}
