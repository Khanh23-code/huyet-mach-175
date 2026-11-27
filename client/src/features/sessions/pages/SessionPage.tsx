import { useEffect, useMemo, useState } from 'react'
import { Empty, Skeleton } from 'antd'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { PageContainer, PageHeader, SectionCard, StatusPill } from '@/shared/components'
import { todayIso } from '@/shared/utils/date'
import { useSessionDetail, useSessionQueue } from '../hooks'
import SessionQueue from '../components/SessionQueue'
import SessionWorkspace from '../components/SessionWorkspace'
import type { SessionStatus } from '../types'
import './SessionPage.css'

const REFRESH_MS = 15_000

export default function SessionPage() {
    const { can } = usePermission()
    const today = useMemo(todayIso, [])

    const [filter, setFilter] = useState<SessionStatus>('WAITING_COLLECTION')
    const [keyword, setKeyword] = useState('')
    const [search, setSearch] = useState('')
    const [selectedId, setSelectedId] = useState<string | null>(null)

    useEffect(() => {
        const t = setTimeout(() => setSearch(keyword.trim()), 400)
        return () => clearTimeout(t)
    }, [keyword])

    const { data, isLoading } = useSessionQueue(
        { date: today, status: filter, search: search || undefined, pageNumber: 1, pageSize: 100 },
        { refetchInterval: REFRESH_MS },
    )
    const items = useMemo(() => data?.items ?? [], [data])

    useEffect(() => {
        if (selectedId !== null) return
        if (items[0]) setSelectedId(items[0].sessionId)
    }, [items, selectedId])

    const { data: detail, isLoading: detailLoading, isError: detailError } = useSessionDetail(selectedId)

    const handleFilterChange = (f: SessionStatus) => {
        setFilter(f)
        setSelectedId(null)
    }

    const handleClosed = () => {
        const next = items.find((i) => i.status === 'WAITING_COLLECTION' && i.sessionId !== selectedId)
        setSelectedId(next ? next.sessionId : null)
    }

    return (
        <PageContainer>
            <PageHeader
                breadcrumb={[{ title: 'Tiếp nhận & Sàng lọc' }, { title: 'Phiên lấy máu' }]}
                title="Phiên lấy máu"
                subtitle="Quản lý người hiến đạt tiêu chuẩn, ghi nhận thông tin thu thập và sự cố phát sinh."
                extra={<StatusPill tone="info">Ca sáng - Phòng lấy máu A</StatusPill>}
            />

            <div className="ss-layout">
                <SessionQueue
                    items={items}
                    loading={isLoading}
                    selectedId={selectedId}
                    onSelect={setSelectedId}
                    filter={filter}
                    onFilterChange={handleFilterChange}
                    keyword={keyword}
                    onKeywordChange={setKeyword}
                />

                <div className="ss-detail">
                    {selectedId === null ? (
                        <SectionCard>
                            <Empty description="Chọn một người hiến trong hàng chờ để bắt đầu lấy máu" />
                        </SectionCard>
                    ) : detailLoading ? (
                        <SectionCard>
                            <Skeleton active paragraph={{ rows: 10 }} />
                        </SectionCard>
                    ) : detailError || !detail ? (
                        <SectionCard>
                            <Empty description="Không tải được thông tin phiên lấy máu" />
                        </SectionCard>
                    ) : (
                        <SessionWorkspace
                            key={detail.sessionId}
                            detail={detail}
                            canEdit={can(PERMISSIONS.SESSION_EDIT)}
                            onClosed={handleClosed}
                        />
                    )}
                </div>
            </div>
        </PageContainer>
    )
}