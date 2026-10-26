import { useState } from 'react'
import { Button, Empty, Skeleton, Tag } from 'antd'
import ReviewProposalModal from './ReviewProposalModal'
import { usePendingCampaigns } from '../hooks'
import {
    CAMPAIGN_TYPE_LABEL,
    fmtDate,
    fmtDateTime,
    fmtOperatingDays,
    slotsText,
} from '../format'

export default function PendingProposals() {
    const { data, isLoading } = usePendingCampaigns()
    const [reviewId, setReviewId] = useState<number | null>(null)

    return (
        <section className="ds-card">
            <div className="ds-card__head">
                <h3>Đợt hiến máu chờ phê duyệt</h3>
                <p>Xem xét nội dung đề xuất, khung giờ và địa điểm trước khi phê duyệt cho phép đặt hẹn.</p>
            </div>

            {isLoading ? (
                <div style={{ padding: 24 }}>
                    <Skeleton active paragraph={{ rows: 3 }} />
                </div>
            ) : !data || data.items.length === 0 ? (
                <Empty
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                    description="Không có đợt hiến máu nào đang chờ duyệt"
                    style={{ padding: 24 }}
                />
            ) : (
                <div className="ds-rows">
                    {data.items.map((c) => (
                        <div className="ds-row" key={c.campaignId}>
                            <div>
                                <Tag color={c.campaignType === 'EVENT' ? 'blue' : 'orange'}>
                                    {CAMPAIGN_TYPE_LABEL[c.campaignType]}
                                </Tag>
                            </div>
                            <div>
                                <b>{c.campaignName}</b>
                                <div className="ds-muted ds-small">{c.locationName}</div>
                            </div>
                            <div>
                                <div className="ds-muted ds-small">
                                    {fmtDate(c.startDate)} – {fmtDate(c.endDate)}
                                    {c.campaignType === 'RECURRING' && c.operatingDays && ` (${fmtOperatingDays(c.operatingDays)})`}
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
                                className="ds-review-btn"
                                onClick={() => setReviewId(c.campaignId)}
                            >
                                Xem &amp; duyệt
                            </Button>
                        </div>
                    ))}
                </div>
            )}

            <ReviewProposalModal campaignId={reviewId} onClose={() => setReviewId(null)} />
        </section>
    )
}
