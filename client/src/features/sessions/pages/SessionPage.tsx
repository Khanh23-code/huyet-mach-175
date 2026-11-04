import { useEffect, useMemo, useState } from 'react'
import { Breadcrumb, Empty, Skeleton, Tag } from 'antd'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { todayIso } from '@/features/reception/format'
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
    const [selectedId, setSelectedId] = useState<number | null>(null)

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
        <div className="ss">
            <Breadcrumb items={[{ title: 'Tiếp nhận & Sàng lọc' }, { title: 'Phiên lấy máu' }]} />
            <div className="ss-title">
                <div>
                    <h1>Phiên lấy máu</h1>
                    <p>Quản lý người hiến đạt tiêu chuẩn, ghi nhận thông tin thu thập và sự cố phát sinh.</p>
                </div>
                <Tag color="blue">Ca sáng · Phòng lấy máu A</Tag>
            </div>

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
                        <Empty description="Chọn một người hiến trong hàng chờ để bắt đầu lấy máu" />
                    ) : detailLoading ? (
                        <Skeleton active paragraph={{ rows: 10 }} />
                    ) : detailError || !detail ? (
                        <Empty description="Không tải được thông tin phiên lấy máu" />
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
        </div>
    )
}