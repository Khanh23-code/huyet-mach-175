import type { KeyboardEvent } from 'react'
import { Progress, Tag } from 'antd'
import { CalendarOutlined, CloseCircleOutlined, SyncOutlined } from '@ant-design/icons'
import SlotsTable from './SlotsTable'
import {
    CAMPAIGN_STATUS_LABEL,
    CAMPAIGN_TYPE_LABEL,
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
    const onKey = (e: KeyboardEvent) => {
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault()
            onOpen(campaign)
        }
    }

    const status = normalizeCampaignStatus(campaign.status)
    const campaignType = normalizeCampaignType(campaign.campaignType)

    // REJECTED hoặc CANCELLED: không cho phép đăng ký -> gắn badge đỏ và ẩn thanh progress bar
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
                <Tag className="campaign-tag">
                    {CAMPAIGN_TYPE_LABEL[campaignType]}
                </Tag>

                <Tag className="campaign-tag">
                    {status === 'IN_PROGRESS' && (
                        <span className="ds-pulse-dot" />
                    )}
                    {CAMPAIGN_STATUS_LABEL[status]}
                </Tag>
            </div>


            <h4>{campaign.campaignName}</h4>
            <p className="ds-site__addr">{campaign.locationName}</p>

            <div className="ds-site__meta">
                <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                    <CalendarOutlined style={{ color: 'var(--ds-muted)' }} />
                    <span>{fmtDate(campaign.startDate)} – {fmtDate(campaign.endDate)}</span>
                </div>
                {campaignType === 'RECURRING' && campaign.operatingDays && (
                    <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 4 }}>
                        <SyncOutlined style={{ color: 'var(--ds-muted)' }} />
                        <span>{fmtOperatingDays(campaign.operatingDays)}</span>
                    </div>
                )}
            </div>

            {/* Với các campaign REJECTED hoặc CANCELLED: ẩn progress bar và hiển thị thông báo đã dừng đăng ký */}
            {isClosed ? (
                <div className="ds-closed-alert">
                    <CloseCircleOutlined style={{ color: '#c4261b' }} />
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
                        strokeColor={status === 'COMPLETED' ? '#6d28d9' : '#1a7534'}
                        size="small"
                    />
                    <div className="ds-muted ds-small" style={{ marginTop: 2 }}>
                        Đã thu gom: <b>{campaign.totalCollected}</b> túi máu
                    </div>
                </div>
            )}

            {campaign.slots && campaign.slots.length > 0 && (
                <SlotsTable slots={campaign.slots} />
            )}
        </div>
    )
}
