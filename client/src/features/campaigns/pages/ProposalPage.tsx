import { useState } from 'react'
import { Breadcrumb, Button } from 'antd'
import CampaignForm from '../components/CampaignForm'
import MyCampaigns from '../components/MyCampaigns'
import CampaignDetailModal from '../components/CampaignDetailModal'
import { useMyCampaigns } from '../hooks'
import type { CampaignListItem } from '../types'
import './CampaignPage.css'

export default function ProposalPage() {
    const [detailId, setDetailId] = useState<number | null>(null)
    const [editingCampaign, setEditingCampaign] = useState<CampaignListItem | null>(null)

    const pendingQuery = useMyCampaigns({ status: 'PENDING_APPROVAL', pageNumber: 1, pageSize: 1 })
    const pendingTotal = pendingQuery.data?.totalCount ?? 0

    const handleEdit = (campaign: CampaignListItem) => {
        setEditingCampaign(campaign)
        window.scrollTo({ top: 0, behavior: 'smooth' })
    }

    const handleFormDone = () => {
        setEditingCampaign(null)
    }

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
                        <div className="ds-eyebrow">
                            {editingCampaign ? 'Đang chỉnh sửa bản nháp' : 'Khởi tạo mới'}
                        </div>
                        <h3>
                            {editingCampaign
                                ? `Chỉnh sửa đợt hiến: ${editingCampaign.campaignName}`
                                : 'Tạo đề xuất đợt hiến máu mới'}
                        </h3>
                        <p>
                            {editingCampaign
                                ? 'Cập nhật lại các thông tin của đợt hiến máu trước khi gửi phê duyệt.'
                                : 'Đợt hiến máu sẽ được lưu ở trạng thái Bản nháp (DRAFT) trước khi gửi phê duyệt.'}
                        </p>
                    </div>
                    {editingCampaign && (
                        <Button onClick={handleFormDone}>
                            Hủy chế độ sửa (Tạo mới)
                        </Button>
                    )}
                </div>
                <div className="ds-card__body">
                    <CampaignForm
                        campaign={editingCampaign}
                        onSuccess={handleFormDone}
                        onCancel={handleFormDone}
                    />
                </div>
            </section>

            <MyCampaigns
                onOpenDetail={setDetailId}
                onEdit={handleEdit}
            />

            <CampaignDetailModal campaignId={detailId} onClose={() => setDetailId(null)} />
        </div>
    )
}
