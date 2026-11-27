import { useState } from 'react'
import { Button } from 'antd'
import { PageContainer, PageHeader, SectionCard, StatusPill } from '@/shared/components'
import CampaignForm from '../components/CampaignForm'
import MyCampaigns from '../components/MyCampaigns'
import CampaignDetailModal from '../components/CampaignDetailModal'
import { useMyCampaigns } from '../hooks'
import type { CampaignListItem } from '../types'
import './CampaignPage.css'

export default function ProposalPage() {
    const [detailId, setDetailId] = useState<string | null>(null)
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
        <PageContainer>
            <PageHeader
                breadcrumb={[
                    { title: 'Khoa Truyền máu' },
                    { title: 'Đề xuất & Quản lý đợt hiến máu' },
                ]}
                title="Đề xuất & Quản lý đợt hiến máu"
                subtitle="Khởi tạo đợt hiến máu mới (Chiến dịch sự kiện hoặc Điểm hiến định kỳ), cấu hình khung giờ và gửi Ban Giám đốc phê duyệt."
                extra={
                    pendingTotal > 0 ? (
                        <StatusPill tone="warning">
                            {pendingTotal} đợt đang chờ duyệt
                        </StatusPill>
                    ) : undefined
                }
            />

            <SectionCard
                eyebrow={editingCampaign ? 'Đang chỉnh sửa bản nháp' : 'Khởi tạo mới'}
                title={
                    editingCampaign
                        ? `Chỉnh sửa đợt hiến: ${editingCampaign.campaignName}`
                        : 'Tạo đề xuất đợt hiến máu mới'
                }
                subtitle={
                    editingCampaign
                        ? 'Cập nhật lại các thông tin của đợt hiến máu trước khi gửi phê duyệt.'
                        : 'Đợt hiến máu sẽ được lưu ở trạng thái Bản nháp (DRAFT) trước khi gửi phê duyệt.'
                }
                extra={
                    editingCampaign ? (
                        <Button onClick={handleFormDone}>
                            Hủy chế độ sửa (Tạo mới)
                        </Button>
                    ) : undefined
                }
            >
                <CampaignForm
                    campaign={editingCampaign}
                    onSuccess={handleFormDone}
                    onCancel={handleFormDone}
                />
            </SectionCard>

            <MyCampaigns
                onOpenDetail={setDetailId}
                onEdit={handleEdit}
            />

            <CampaignDetailModal campaignId={detailId} onClose={() => setDetailId(null)} />
        </PageContainer>
    )
}
