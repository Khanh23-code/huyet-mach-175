import { useEffect, useMemo, useState } from 'react'
import { Breadcrumb, Empty, Skeleton } from 'antd'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { todayIso } from '@/features/reception/format'
import { useWorkCampaign } from '@/features/reception/useWorkCampaign'
import CampaignGate from '@/features/reception/components/CampaignGate'
import WorkCampaignBanner from '@/features/reception/components/WorkCampaignBanner'
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
    const [selectedId, setSelectedId] = useState<number | null>(null)

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

    const head = (
        <>
            <Breadcrumb items={[{ title: 'Tiếp nhận & Sàng lọc' }, { title: 'Khám sàng lọc' }]} />
            <div className="sc-title">
                <h1>Khám sàng lọc người hiến</h1>
                <p>Đánh giá điều kiện sức khỏe và đưa ra quyết định tiếp nhận hiến máu.</p>
            </div>
        </>
    )

    if (wc.blocked) {
        return (
            <div className="sc">
                {head}
                <CampaignGate />
            </div>
        )
    }

    return (
        <div className="sc">
            {head}
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
                        <Empty description="Bạn không có quyền xem hồ sơ sàng lọc" />
                    ) : selectedId === null ? (
                        <Empty description="Chọn một người hiến trong hàng chờ để bắt đầu khám" />
                    ) : detailLoading ? (
                        <Skeleton active paragraph={{ rows: 10 }} />
                    ) : detailError || !detail ? (
                        <Empty description="Không tải được hồ sơ sàng lọc" />
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
        </div>
    )
}