import { useState } from 'react'
import { Breadcrumb, Empty, Skeleton } from 'antd'
import Can from '@/features/auth/components/Can'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import PendingProposals from '../components/PendingProposals'
import SiteCard from '../components/SiteCard'
import SiteDetailModal from '../components/SiteDetailModal'
import { usePendingProposals, useSites } from '../hooks'
import type { Site } from '../types'
import './CampaignPage.css'

export default function SiteMonitorPage() {
    const { can } = usePermission()
    const canApprove = can(PERMISSIONS.CAMPAIGN_APPROVE)
    const sites = useSites()
    const pending = usePendingProposals(canApprove)
    const [detail, setDetail] = useState<Site | null>(null)

    const pendingTotal = pending.data?.total ?? 0

    return (
        <div className="ds">
            <Breadcrumb items={[
                { title: 'Quản trị hệ thống' },
                { title: canApprove ? 'Lịch & sức chứa điểm hiến' : 'Điểm hiến máu & Lịch slot' },
            ]} />

            <div className="ds-title">
                <div>
                    <h1>{canApprove ? 'Duyệt & kích hoạt đề xuất' : 'Điểm hiến máu & Lịch slot'}</h1>
                    <p>
                        {canApprove
                            ? 'Kiểm soát thay đổi điểm hiến trước khi đồng bộ sang lịch đặt hẹn của người hiến máu.'
                            : 'Theo dõi các điểm hiến đang hoạt động cùng khung giờ và sức chứa mỗi slot.'}
                    </p>
                </div>
                <Can do={PERMISSIONS.CAMPAIGN_APPROVE}>
                    {pendingTotal > 0 && <span className="ds-badge"><i />{pendingTotal} đề xuất chờ duyệt</span>}
                </Can>
            </div>

            <Can do={PERMISSIONS.CAMPAIGN_APPROVE}>
                <PendingProposals />
            </Can>

            <section className="ds-card">
                <div className="ds-card__head ds-card__head--row">
                    <div>
                        <div className="ds-eyebrow">Cấu hình đang phát hành</div>
                        <h3>Điểm hiến đang hoạt động</h3>
                        <p>Danh sách chỉ đọc. Mọi thay đổi phải được BBNK gửi qua luồng đề xuất mới.</p>
                    </div>
                    <span className="ds-status ds-status--on"><i />Đang đồng bộ ứng dụng người hiến</span>
                </div>

                <div className="ds-card__body">
                    {sites.isLoading ? (
                        <Skeleton active paragraph={{ rows: 6 }} />
                    ) : !sites.data || sites.data.length === 0 ? (
                        <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Chưa có điểm hiến nào đang hoạt động" />
                    ) : (
                        <div className="ds-grid">
                            {sites.data.map((s) => <SiteCard key={s.siteId} site={s} onOpen={setDetail} />)}
                        </div>
                    )}
                </div>
            </section>

            <SiteDetailModal site={detail} onClose={() => setDetail(null)} />
        </div>
    )
}
