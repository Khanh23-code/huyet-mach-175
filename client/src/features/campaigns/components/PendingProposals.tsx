import { useState } from 'react'
import { Button, Empty, Skeleton } from 'antd'
import { SectionCard, StatusPill } from '@/shared/components'
import ReviewProposalModal from './ReviewProposalModal'
import { usePendingCampaigns } from '../hooks'
import {
    CAMPAIGN_TYPE_LABEL,
    CAMPAIGN_TYPE_TONE,
    fmtDate,
    fmtDateTime,
    fmtOperatingDays,
    normalizeCampaignType,
    slotsText,
} from '../format'

export default function PendingProposals() {
    const { data, isLoading } = usePendingCampaigns()
    const [reviewId, setReviewId] = useState<string | null>(null)

    return (
        <SectionCard
            eyebrow="Chờ phê duyệt"
            title="Đợt hiến máu chờ phê duyệt"
            subtitle="Xem xét nội dung đề xuất, khung giờ và địa điểm trước khi phê duyệt cho phép đặt hẹn."
            noPadding
        >
            {isLoading ? (
                <div style={{ padding: 'var(--space-6)' }}>
                    <Skeleton active paragraph={{ rows: 3 }} />
                </div>
            ) : !data || data.items.length === 0 ? (
                <Empty
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                    description="Không có đợt hiến máu nào đang chờ duyệt"
                    style={{ padding: 'var(--space-6)' }}
                />
            ) : (
                <div className="ds-rows">
                    {data.items.map((c) => {
                        const cType = normalizeCampaignType(c.campaignType)
                        return (
                            <div className="ds-row" key={c.campaignId}>
                                <div>
                                    <StatusPill tone={CAMPAIGN_TYPE_TONE[cType]} dot={false}>
                                        {CAMPAIGN_TYPE_LABEL[cType]}
                                    </StatusPill>
                                </div>
                                <div>
                                    <span className="ds-code">ID: {c.campaignId}</span>
                                    <div><b>{c.campaignName}</b></div>
                                    <div className="ds-muted ds-small">{c.locationName}</div>
                                </div>
                                <div>
                                    <div className="ds-muted ds-small">
                                        {fmtDate(c.startDate)} – {fmtDate(c.endDate)}
                                        {cType === 'RECURRING' && c.operatingDays && ` (${fmtOperatingDays(c.operatingDays)})`}
                                    </div>
                                    <b>Chỉ tiêu: {c.targetDonations} túi</b>
                                    {c.slots && c.slots.length > 0 && (
                                        <div className="ds-muted ds-small">{slotsText(c.slots)}</div>
                                    )}
                                </div>
                                <div>
                                    <b>{c.createdByName ?? 'Nhân viên'}</b>
                                    <div className="ds-muted ds-small">{fmtDateTime(c.createdAt)}</div>
                                </div>
                                <Button
                                    color="primary"
                                    variant="filled"
                                    onClick={() => setReviewId(c.campaignId)}
                                >
                                    Xem &amp; duyệt
                                </Button>
                            </div>
                        )
                    })}
                </div>
            )}

            <ReviewProposalModal campaignId={reviewId} onClose={() => setReviewId(null)} />
        </SectionCard>
    )
}
