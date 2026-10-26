import { useState } from 'react'
import { Breadcrumb, Empty, Input, Pagination, Select, Skeleton, Space } from 'antd'
import Can from '@/features/auth/components/Can'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import PendingProposals from '../components/PendingProposals'
import CampaignCard from '../components/CampaignCard'
import CampaignDetailModal from '../components/CampaignDetailModal'
import { useCampaigns, usePendingCampaigns } from '../hooks'
import type { CampaignListItem, CampaignStatus } from '../types'
import './CampaignPage.css'

const PAGE_SIZE = 9

export default function SiteMonitorPage() {
    const { can } = usePermission()
    const canApprove = can(PERMISSIONS.CAMPAIGN_APPROVE)

    const [page, setPage] = useState(1)
    const [statusFilter, setStatusFilter] = useState<CampaignStatus | undefined>(undefined)
    const [keyword, setKeyword] = useState<string>('')
    const [detailCampaign, setDetailCampaign] = useState<CampaignListItem | null>(null)

    const pendingQuery = usePendingCampaigns(canApprove)
    const pendingTotal = pendingQuery.data?.totalCount ?? 0

    const campaignsQuery = useCampaigns({
        pageNumber: page,
        pageSize: PAGE_SIZE,
        status: statusFilter,
        keyword: keyword || undefined,
    })

    const campaigns = campaignsQuery.data?.items ?? []
    const totalCount = campaignsQuery.data?.totalCount ?? 0

    return (
        <div className="ds">
            <Breadcrumb
                items={[
                    { title: 'Quản trị hệ thống' },
                    { title: canApprove ? 'Giám sát & Phê duyệt đợt hiến máu' : 'Theo dõi các đợt hiến máu' },
                ]}
            />

            <div className="ds-title">
                <div>
                    <h1>{canApprove ? 'Giám sát & Phê duyệt đợt hiến máu' : 'Theo dõi các đợt hiến máu'}</h1>
                    <p>
                        {canApprove
                            ? 'Kiểm soát và phê duyệt các đề xuất đợt hiến máu trước khi mở cổng đặt hẹn cho người hiến máu.'
                            : 'Theo dõi các chiến dịch hiến máu đang triển khai, khung giờ tiếp nhận và tiến độ thu gom.'}
                    </p>
                </div>
                <Can do={PERMISSIONS.CAMPAIGN_APPROVE}>
                    {pendingTotal > 0 && (
                        <span className="ds-badge">
                            <i />
                            {pendingTotal} đợt hiến chờ duyệt
                        </span>
                    )}
                </Can>
            </div>

            {/* Khu vực duyệt đợt hiến cho Lãnh đạo / Ban Giám đốc */}
            <Can do={PERMISSIONS.CAMPAIGN_APPROVE}>
                <PendingProposals />
            </Can>

            {/* Danh sách các đợt hiến máu */}
            <section className="ds-card">
                <div className="ds-card__head ds-card__head--row">
                    <div>
                        <div className="ds-eyebrow">Vận hành đợt hiến</div>
                        <h3>Danh sách đợt hiến máu</h3>
                        <p>Theo dõi các chiến dịch sự kiện và điểm hiến máu định kỳ trên hệ thống.</p>
                    </div>

                    <Space wrap>
                        <Input.Search
                            placeholder="Tìm kiếm chiến dịch, địa điểm..."
                            allowClear
                            onSearch={(v) => {
                                setKeyword(v.trim())
                                setPage(1)
                            }}
                            style={{ width: 240 }}
                        />
                        <Select
                            placeholder="Tất cả trạng thái"
                            allowClear
                            style={{ width: 170 }}
                            value={statusFilter}
                            onChange={(v) => {
                                setStatusFilter(v)
                                setPage(1)
                            }}
                            options={[
                                { value: 'IN_PROGRESS', label: 'Đang diễn ra' },
                                { value: 'APPROVED', label: 'Đã phê duyệt' },
                                { value: 'COMPLETED', label: 'Đã hoàn thành' },
                                { value: 'CANCELLED', label: 'Đã hủy' },
                                { value: 'PENDING_APPROVAL', label: 'Chờ phê duyệt' },
                                { value: 'DRAFT', label: 'Bản nháp' },
                                { value: 'REJECTED', label: 'Bị từ chối' },
                            ]}
                        />
                    </Space>
                </div>

                <div className="ds-card__body">
                    {campaignsQuery.isLoading ? (
                        <Skeleton active paragraph={{ rows: 6 }} />
                    ) : campaigns.length === 0 ? (
                        <Empty
                            image={Empty.PRESENTED_IMAGE_SIMPLE}
                            description="Không có đợt hiến máu nào phù hợp điều kiện lọc"
                        />
                    ) : (
                        <>
                            <div className="ds-grid">
                                {campaigns.map((c) => (
                                    <CampaignCard
                                        key={c.campaignId}
                                        campaign={c}
                                        onOpen={setDetailCampaign}
                                    />
                                ))}
                            </div>

                            {totalCount > PAGE_SIZE && (
                                <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 24 }}>
                                    <Pagination
                                        current={page}
                                        pageSize={PAGE_SIZE}
                                        total={totalCount}
                                        showSizeChanger={false}
                                        onChange={setPage}
                                    />
                                </div>
                            )}
                        </>
                    )}
                </div>
            </section>

            <CampaignDetailModal
                campaign={detailCampaign}
                onClose={() => setDetailCampaign(null)}
            />
        </div>
    )
}
