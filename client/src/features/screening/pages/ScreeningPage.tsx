import { useEffect, useMemo, useState } from 'react'
import { Empty, Skeleton } from 'antd'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { PageContainer, PageHeader, SectionCard } from '@/shared/components'
import { todayIso } from '@/shared/utils/date'
import { CampaignGate, useWorkCampaign, WorkCampaignBanner } from '@/features/work-campaign'
import { useScreeningQueue, useSurveyDetail } from '../hooks'
import ScreeningQueue, { type QueueFilter } from '../components/ScreeningQueue'
import ScreeningWorkspace from '../components/ScreeningWorkspace'
import './ScreeningPage.css'

const REFRESH_MS = 15_000

export default function ScreeningPage() {
    const { can } = usePermission()
    const wc = useWorkCampaign()
    const today = useMemo(todayIso, [])

    const [filter, setFilter] = useState<QueueFilter>('PENDING')
    const [keyword, setKeyword] = useState('')
    const [search, setSearch] = useState('')
    const [selectedId, setSelectedId] = useState<string | null>(null)

    useEffect(() => {
        const t = setTimeout(() => setSearch(keyword.trim()), 400)
        return () => clearTimeout(t)
    }, [keyword])

    const { data, isLoading } = useScreeningQueue(
        { date: today, campaignId: wc.campaignId, search: search || undefined, page: 1, pageSize: 100 },
        { enabled: !wc.blocked, refetchInterval: REFRESH_MS },
    )

    const items = useMemo(() => data?.items ?? [], [data])
    const pendingCount = items.filter((i) => i.screeningStatus === 'PENDING').length
    const visible = items.filter((i) =>
        filter === 'ALL' ? true : filter === 'PENDING' ? i.screeningStatus === 'PENDING' : i.screeningStatus !== 'PENDING',
    )

    // Tự chọn người đầu hàng chờ khi chưa chọn ai
    useEffect(() => {
        if (selectedId !== null) return
        const first = items.find((i) => i.screeningStatus === 'PENDING') ?? items[0]
        if (first) setSelectedId(first.appointmentId)
    }, [items, selectedId])

    // Đổi đợt hiến máu -> bỏ chọn
    useEffect(() => {
        setSelectedId(null)
    }, [wc.campaignId])

    const canViewSurvey = can(PERMISSIONS.SURVEY_VIEW)
    const { data: detail, isLoading: detailLoading, isError: detailError } = useSurveyDetail(selectedId, canViewSurvey)

    // Sau khi lưu: chuyển sang người kế tiếp đang chờ khám
    const handleSaved = () => {
        const next = items.find((i) => i.screeningStatus === 'PENDING' && i.appointmentId !== selectedId)
        if (next) setSelectedId(next.appointmentId)
    }

    const header = (
        <PageHeader
            breadcrumb={[{ title: 'Tiếp nhận & Sàng lọc' }, { title: 'Khám sàng lọc' }]}
            title="Khám sàng lọc người hiến"
            subtitle="Đánh giá điều kiện sức khỏe và đưa ra quyết định tiếp nhận hiến máu."
        />
    )

    if (wc.blocked) {
        return (
            <PageContainer>
                {header}
                <CampaignGate />
            </PageContainer>
        )
    }

    return (
        <PageContainer>
            {header}
            {wc.requireCampaign && wc.campaign && <WorkCampaignBanner campaign={wc.campaign} onChange={wc.clear} />}

            <div className="sc-layout">
                <ScreeningQueue
                    items={visible}
                    loading={isLoading}
                    pendingCount={pendingCount}
                    selectedId={selectedId}
                    onSelect={setSelectedId}
                    filter={filter}
                    onFilterChange={setFilter}
                    keyword={keyword}
                    onKeywordChange={setKeyword}
                />

                <div className="sc-detail">
                    {!canViewSurvey ? (
                        <SectionCard>
                            <Empty description="Bạn không có quyền xem hồ sơ sàng lọc" />
                        </SectionCard>
                    ) : selectedId === null ? (
                        <SectionCard>
                            <Empty description="Chọn một người hiến trong hàng chờ để bắt đầu khám" />
                        </SectionCard>
                    ) : detailLoading ? (
                        <SectionCard>
                            <Skeleton active paragraph={{ rows: 10 }} />
                        </SectionCard>
                    ) : detailError || !detail ? (
                        <SectionCard>
                            <Empty description="Không tải được hồ sơ sàng lọc" />
                        </SectionCard>
                    ) : (
                        <ScreeningWorkspace
                            key={detail.appointmentId}
                            detail={detail}
                            canEdit={can(PERMISSIONS.SCREENING_EDIT)}
                            onSaved={handleSaved}
                        />
                    )}
                </div>
            </div>
        </PageContainer>
    )
}
