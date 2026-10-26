import { useState } from 'react'
import { Breadcrumb } from 'antd'
import CampaignForm from '../components/CampaignForm'
import MyCampaigns from '../components/MyCampaigns'
import CampaignDetailModal from '../components/CampaignDetailModal'
import { useMyCampaigns } from '../hooks'
import type { CampaignType } from '../types'
import './CampaignPage.css'

export default function ProposalPage() {
    const [campaignType] = useState<CampaignType>('EVENT')
    const [detailId, setDetailId] = useState<number | null>(null)

    const pendingQuery = useMyCampaigns({ status: 'PENDING_APPROVAL', pageNumber: 1, pageSize: 1 })
    const pendingTotal = pendingQuery.data?.totalCount ?? 0

    return (
        <div className="ds">
            <Breadcrumb items={[{ title: 'Khoa Truyền máu' }, { title: 'Đề xuất & Quản lý đợt hiến máu' }]} />

            <div className="ds-title">
                <div>
                    <h1>Đề xuất &amp; Quản lý đợt hiến máu</h1>
                    <p>
                        Khởi tạo đợt hiến máu mới (Chiến dịch sự kiện hoặc Điểm hiến định kỳ), cấu hình khung giờ và gửi Ban Giám đốc phê duyệt.
                    </p>
                </div>
                {pendingTotal > 0 && (
                    <span className="ds-badge">
                        <i />
                        {pendingTotal} đợt đang chờ duyệt
                    </span>
                )}
            </div>

            <section className="ds-card">
                <div className="ds-card__head ds-card__head--row">
                    <div>
                        <h3>Tạo đề xuất đợt hiến máu mới</h3>
                        <p>Đợt hiến máu sẽ được lưu ở trạng thái Bản nháp (DRAFT) trước khi gửi phê duyệt.</p>
                    </div>
                </div>
                <div className="ds-card__body">
                    <CampaignForm key={campaignType} defaultType={campaignType} />
                </div>
            </section>

            <MyCampaigns onOpenDetail={setDetailId} />

            <CampaignDetailModal campaignId={detailId} onClose={() => setDetailId(null)} />
        </div>
    )
}
