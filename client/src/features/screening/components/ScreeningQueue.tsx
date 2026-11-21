import { Input, Segmented, Skeleton } from 'antd'
import { RightOutlined, SearchOutlined } from '@ant-design/icons'
import { SectionCard, StatusPill } from '@/shared/components'
import { initials } from '@/shared/utils/string'
import { fmtTime } from '@/shared/formatters/date'
import { SCREENING_META } from '../format'
import type { ScreeningQueueItem } from '../types'

export type QueueFilter = 'PENDING' | 'DONE' | 'ALL'

interface Props {
    items: ScreeningQueueItem[]
    loading: boolean
    pendingCount: number
    selectedId: number | null
    onSelect: (appointmentId: number) => void
    filter: QueueFilter
    onFilterChange: (f: QueueFilter) => void
    keyword: string
    onKeywordChange: (v: string) => void
}

export default function ScreeningQueue({
                                           items,
                                           loading,
                                           pendingCount,
                                           selectedId,
                                           onSelect,
                                           filter,
                                           onFilterChange,
                                           keyword,
                                           onKeywordChange,
                                       }: Props) {
    return (
        <SectionCard
            title="Chờ sàng lọc"
            subtitle={`${pendingCount} người hiến chờ khám`}
            noPadding
        >
            <div className="sc-queue-tools">
                <Segmented<QueueFilter>
                    block
                    value={filter}
                    onChange={onFilterChange}
                    options={[
                        { label: 'Chờ khám', value: 'PENDING' },
                        { label: 'Đã khám', value: 'DONE' },
                        { label: 'Tất cả', value: 'ALL' },
                    ]}
                />
                <Input
                    allowClear
                    prefix={<SearchOutlined />}
                    placeholder="Tìm tên, CCCD, SĐT hoặc mã QR"
                    value={keyword}
                    onChange={(e) => onKeywordChange(e.target.value)}
                />
            </div>

            <div className="sc-queue-list">
                {loading ? (
                    <Skeleton active paragraph={{ rows: 4 }} style={{ padding: 16 }} />
                ) : items.length === 0 ? (
                    <div className="sc-empty">Không có người hiến nào trong danh sách</div>
                ) : (
                    items.map((it, i) => {
                        const meta = SCREENING_META[it.screeningStatus]
                        return (
                            <button
                                type="button"
                                key={it.appointmentId}
                                className={`sc-queue-item${it.appointmentId === selectedId ? ' is-active' : ''}`}
                                onClick={() => onSelect(it.appointmentId)}
                            >
                                <span className="sc-queue-no">{String(i + 1).padStart(2, '0')}</span>
                                <span className="sc-avatar">{initials(it.donorName)}</span>
                                <span className="sc-queue-body">
                                    <b>{it.donorName}</b>
                                    <small>{it.donorCode} · Check-in {fmtTime(it.checkedInAt)}</small>
                                    <span className="sc-queue-tags">
                                        {it.isWalkIn && <StatusPill tone="neutral" dot={false}>Vãng lai</StatusPill>}
                                        {it.hasRisk && <StatusPill tone="warning">Có cảnh báo</StatusPill>}
                                        {it.screeningStatus !== 'PENDING' && (
                                            <StatusPill tone={meta.tone}>{meta.label}</StatusPill>
                                        )}
                                    </span>
                                </span>
                                <RightOutlined className="sc-queue-arrow" />
                            </button>
                        )
                    })
                )}
            </div>

            <div className="sc-queue-foot">
                <span className="sc-dot" /> Tự động cập nhật hàng chờ
            </div>
        </SectionCard>
    )
}