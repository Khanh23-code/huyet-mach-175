import type { KeyboardEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, Progress } from 'antd'
import { BarChartOutlined, CalendarOutlined, CloseCircleOutlined, SyncOutlined } from '@ant-design/icons'
import { StatusPill } from '@/shared/components'
import SlotsTable from './SlotsTable'
import {
    CAMPAIGN_STATUS_LABEL,
    CAMPAIGN_STATUS_TONE,
    CAMPAIGN_TYPE_LABEL,
    CAMPAIGN_TYPE_TONE,
    fmtDate,
    fmtOperatingDays,
    normalizeCampaignStatus,
    normalizeCampaignType,
} from '../format'
import type { CampaignListItem } from '../types'

interface Props {
    campaign: CampaignListItem
    onOpen: (campaign: CampaignListItem) => void
}

export default function CampaignCard({ campaign, onOpen }: Props) {
    const navigate = useNavigate()

    const onKey = (e: KeyboardEvent) => {
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault()
            onOpen(campaign)
        }
    }

    const status = normalizeCampaignStatus(campaign.status)
    const campaignType = normalizeCampaignType(campaign.campaignType)
    const isClosed = status === 'REJECTED' || status === 'CANCELLED'

    const percent = campaign.targetDonations > 0
        ? Math.min(100, Math.round((campaign.totalRegistered / campaign.targetDonations) * 100))
        : 0

    return (
        <div
            className={`ds-site ds-site--${status.toLowerCase()}`}
            role="button"
            tabIndex={0}
            onClick={() => onOpen(campaign)}
            onKeyDown={onKey}
        >
            <div className="ds-site__top">
                <StatusPill tone={CAMPAIGN_TYPE_TONE[campaignType]} dot={false}>
                    {CAMPAIGN_TYPE_LABEL[campaignType]}
                </StatusPill>

                <StatusPill
                    tone={CAMPAIGN_STATUS_TONE[status]}
                    pulse={status === 'IN_PROGRESS'}
                >
                    {CAMPAIGN_STATUS_LABEL[status]}
                </StatusPill>
            </div>

            <div className="ds-code" title="campaign_id">ID: {campaign.campaignId}</div>
            <h4>{campaign.campaignName}</h4>
            <p className="ds-site__addr">{campaign.locationName}</p>

            <div className="ds-site__meta">
                <div className="ds-site__meta-row">
                    <CalendarOutlined />
                    <span>{fmtDate(campaign.startDate)} – {fmtDate(campaign.endDate)}</span>
                </div>
                {campaignType === 'RECURRING' && campaign.operatingDays && (
                    <div className="ds-site__meta-row">
                        <SyncOutlined />
                        <span>{fmtOperatingDays(campaign.operatingDays)}</span>
                    </div>
                )}
            </div>

            {isClosed ? (
                <div className="ds-closed-alert">
                    <CloseCircleOutlined />
                    <span>
                        {status === 'REJECTED'
                            ? 'Đề xuất bị từ chối · Không mở đăng ký'
                            : 'Đợt hiến đã bị hủy · Đã dừng tiếp nhận'}
                    </span>
                </div>
            ) : (
                <div className="ds-progress-wrap">
                    <div className="ds-progress-labels">
                        <span>Đã đăng ký: <b>{campaign.totalRegistered}</b> / {campaign.targetDonations}</span>
                        <span>{percent}%</span>
                    </div>
                    <Progress
                        percent={percent}
                        showInfo={false}
                        strokeColor={status === 'COMPLETED' ? 'var(--c-purple)' : 'var(--c-primary)'}
                        size="small"
                    />
                    <div className="ds-muted ds-small">
                        Đã thu gom: <b>{campaign.totalCollected}</b> túi máu
                    </div>
                </div>
            )}

            {campaign.slots && campaign.slots.length > 0 && (
                <SlotsTable slots={campaign.slots} />
            )}

            {(status === 'IN_PROGRESS' || status === 'COMPLETED' || status === 'CANCELLED') && (
                <div className="ds-site__footer">
                    <Button
                        size="small"
                        icon={<BarChartOutlined />}
                        style={{ width: '100%' }}
                        onClick={(e) => {
                            e.stopPropagation()
                            navigate(`/campaigns/${campaign.campaignId}/summary`)
                        }}
                    >
                        Xem báo cáo tổng kết
                    </Button>
                </div>
            )}
        </div>
    )
}
