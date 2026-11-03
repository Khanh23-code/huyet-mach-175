import { Input, Segmented, Skeleton, Tag } from 'antd'
import { RightOutlined, SearchOutlined } from '@ant-design/icons'
import { initials } from '@/features/donors/format'
import { fmtTime } from '@/features/reception/format'
import { fmtBlood } from '@/features/screening/format'
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
        <div className="ss-card ss-queue">
            <div className="ss-queue-head">
                <div className="ss-queue-title">Đạt tiêu chuẩn</div>
                <div className="ss-muted">
                    {items.length} người hiến {filter === 'WAITING_COLLECTION' ? 'chờ lấy máu' : '· ' + SESSION_META[filter].label.toLowerCase()}
                </div>
            </div>

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
                                <small>
                                    {it.donorCode} · <b className="ss-blood">{fmtBlood(it.bloodType, it.rhFactor)}</b> · Thông qua{' '}
                                    {fmtTime(it.screenedAt)}
                                </small>
                                {it.status !== 'WAITING_COLLECTION' && (
                                    <Tag color={SESSION_META[it.status].color}>{SESSION_META[it.status].label}</Tag>
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
        </div>
    )
}