import { useState } from 'react'
import { Button, Empty, Skeleton } from 'antd'
import ReviewProposalModal from './ReviewProposalModal'
import { usePendingProposals } from '../hooks'
import { SITE_TYPE_LABEL, fmtDateTime, proposalKindText, proposalTitle, slotsText } from '../format'

export default function PendingProposals() {
    const { data, isLoading } = usePendingProposals()
    const [reviewId, setReviewId] = useState<number | null>(null)

    return (
        <section className="ds-card">
            <div className="ds-card__head">
                <h3>Đề xuất đang chờ xử lý</h3>
                <p>Ưu tiên đề xuất gửi sớm nhất và kiểm tra kỹ ảnh hưởng đến lịch đang mở.</p>
            </div>

            {isLoading ? (
                <div style={{ padding: 24 }}><Skeleton active paragraph={{ rows: 3 }} /></div>
            ) : !data || data.items.length === 0 ? (
                <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Không có đề xuất nào đang chờ duyệt" style={{ padding: 24 }} />
            ) : (
                <div className="ds-rows">
                    {data.items.map((p) => (
                        <div className="ds-row" key={p.proposalId}>
                            <span className={`ds-tag ds-tag--${p.siteType}`}>{SITE_TYPE_LABEL[p.siteType]}</span>
                            <div>
                                <b>{p.siteName}</b>
                                <div className="ds-muted ds-small">{proposalKindText(p)} · {p.proposalCode}</div>
                            </div>
                            <div>
                                <div className="ds-muted ds-small">Cấu hình đề xuất</div>
                                <b>{proposalTitle(p)}</b>
                                <div className="ds-muted ds-small">{slotsText(p.slots)}</div>
                            </div>
                            <div>
                                <b>{p.createdByName}{p.createdByRoleCode ? ` · ${p.createdByRoleCode}` : ''}</b>
                                <div className="ds-muted ds-small">{fmtDateTime(p.createdAt)}</div>
                            </div>
                            <Button className="ds-review-btn" onClick={() => setReviewId(p.proposalId)}>Xem &amp; duyệt</Button>
                        </div>
                    ))}
                </div>
            )}

            <ReviewProposalModal proposalId={reviewId} onClose={() => setReviewId(null)} />
        </section>
    )
}
