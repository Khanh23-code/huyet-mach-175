import { Input, Segmented, Skeleton } from 'antd'
import { RightOutlined, SearchOutlined } from '@ant-design/icons'
import { BloodGroupTag, SectionCard, StatusPill } from '@/shared/components'
import { initials } from '@/shared/utils/string'
import { fmtTime } from '@/shared/formatters/date'
import { fmtBlood } from '@/shared/formatters/medical'
import { SESSION_META } from '../format'
import type { SessionQueueItem, SessionStatus } from '../types'

interface Props {
    items: SessionQueueItem[]
    loading: boolean
    selectedId: number | null
    onSelect: (sessionId: number) => void
    filter: SessionStatus
    onFilterChange: (f: SessionStatus) => void
    keyword: string
    onKeywordChange: (v: string) => void
}

export default function SessionQueue({
                                         items,
                                         loading,
                                         selectedId,
                                         onSelect,
                                         filter,
                                         onFilterChange,
                                         keyword,
                                         onKeywordChange,
                                     }: Props) {
    return (
        <SectionCard
            title="Đạt tiêu chuẩn"
            subtitle={`${items.length} người hiến ${filter === 'WAITING_COLLECTION' ? 'chờ lấy máu' : '- ' + SESSION_META[filter].label.toLowerCase()}`}
            noPadding
        >
            <div className="ss-queue-tools">
                <Segmented<SessionStatus>
                    block
                    value={filter}
                    onChange={onFilterChange}
                    options={[
                        { label: 'Chờ lấy', value: 'WAITING_COLLECTION' },
                        { label: 'Hoàn tất', value: 'COMPLETED' },
                        { label: 'Sự cố', value: 'ABORTED' },
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

            <div className="ss-queue-list">
                {loading ? (
                    <Skeleton active paragraph={{ rows: 4 }} style={{ padding: 16 }} />
                ) : items.length === 0 ? (
                    <div className="ss-empty">Không có người hiến nào trong danh sách</div>
                ) : (
                    items.map((it) => (
                        <button
                            type="button"
                            key={it.sessionId}
                            className={`ss-queue-item${it.sessionId === selectedId ? ' is-active' : ''}`}
                            onClick={() => onSelect(it.sessionId)}
                        >
                            <span className="ss-avatar">{initials(it.donorName)}</span>
                            <span className="ss-queue-body">
                                <b>{it.donorName}</b>
                                <span className="ss-queue-meta">
                                    <span>{it.donorCode}</span>
                                    <BloodGroupTag value={fmtBlood(it.bloodType, it.rhFactor)} size="sm" />
                                    <span>Thông qua {fmtTime(it.screenedAt)}</span>
                                </span>
                                {it.status !== 'WAITING_COLLECTION' && (
                                    <StatusPill tone={SESSION_META[it.status].tone}>
                                        {SESSION_META[it.status].label}
                                    </StatusPill>
                                )}
                            </span>
                            <RightOutlined className="ss-queue-arrow" />
                        </button>
                    ))
                )}
            </div>

            <div className="ss-queue-foot">
                <span className="ss-dot" /> Tự động cập nhật hàng chờ
            </div>
        </SectionCard>
    )
}