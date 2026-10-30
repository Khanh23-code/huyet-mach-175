import { Input, Segmented, Skeleton, Tag } from 'antd'
import { RightOutlined, SearchOutlined } from '@ant-design/icons'
import { initials } from '@/features/donors/format'
import { fmtTime } from '@/features/reception/format'
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
        <div className="sc-card sc-queue">
            <div className="sc-queue-head">
                <div>
                    <div className="sc-queue-title">Chờ sàng lọc</div>
                    <div className="sc-muted">{pendingCount} người hiến chờ khám</div>
                </div>
            </div>

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
                                        {it.isWalkIn && <Tag>Vãng lai</Tag>}
                                        {it.hasRisk && <Tag color="orange">Có cảnh báo</Tag>}
                                        {it.screeningStatus !== 'PENDING' && <Tag color={meta.color}>{meta.label}</Tag>}
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
        </div>
    )
}