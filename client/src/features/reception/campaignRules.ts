import type { CampaignDetail } from '@/features/campaigns/types'
import { fmtDate } from '@/features/donors/format'
import { todayIso } from './format'

const OPEN_STATUSES = ['APPROVED', 'IN_PROGRESS']

const STATUS_TEXT: Record<string, string> = {
    DRAFT: 'Bản nháp',
    PENDING_APPROVAL: 'Chờ duyệt',
    REJECTED: 'Bị từ chối',
    COMPLETED: 'Đã hoàn thành',
    CANCELLED: 'Đã hủy',
}

/** Trả về lý do không được làm việc với đợt này, hoặc null nếu hợp lệ. */
export function getCampaignBlockReason(
    c: Pick<CampaignDetail, 'status' | 'startDate' | 'endDate'>,
): string | null {
    const status = String(c.status)
    if (!OPEN_STATUSES.includes(status)) {
        return `Đợt hiến máu đang ở trạng thái "${STATUS_TEXT[status] ?? status}", chỉ đợt đã duyệt hoặc đang diễn ra mới được tiếp nhận.`
    }

    const today = todayIso()
    const start = c.startDate.slice(0, 10)
    const end = c.endDate.slice(0, 10)
    if (today < start) return `Đợt hiến máu chưa bắt đầu (bắt đầu ngày ${fmtDate(start)}).`
    if (today > end) return `Đợt hiến máu đã kết thúc (ngày ${fmtDate(end)}).`

    return null
}