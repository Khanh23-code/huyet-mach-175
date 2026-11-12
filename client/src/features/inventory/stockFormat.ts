import dayjs from 'dayjs'
import type { AxiosError } from 'axios'
import type { BagStatus } from './stockTypes'

export const BAG_STATUS_META: Record<BagStatus, { label: string; color: string }> = {
    NEWLY_COLLECTED: { label: 'Mới thu gom', color: 'default' },
    READY_TESTING: { label: 'Chờ xét nghiệm', color: 'gold' },
    TESTED_PASSED: { label: 'Đạt xét nghiệm', color: 'cyan' },
    AVAILABLE: { label: 'Có sẵn', color: 'green' },
    LOCKED: { label: 'Tạm khóa', color: 'red' },
    RESERVED: { label: 'Đặt chỗ', color: 'blue' },
    ISSUED: { label: 'Đã cấp phát', color: 'purple' },
    TRANSFUSED: { label: 'Đã truyền', color: 'default' },
    DISCARDED: { label: 'Đã hủy', color: 'default' },
    PROCESSED: { label: 'Đã điều chế', color: 'geekblue' },
}

export const statusMeta = (s: string) =>
    BAG_STATUS_META[s as BagStatus] ?? { label: s, color: 'default' }

export const STATUS_FILTER_OPTIONS = (Object.keys(BAG_STATUS_META) as BagStatus[]).map((k) => ({
    value: k,
    label: BAG_STATUS_META[k].label,
}))

/** 8 nhóm máu, thứ tự hiển thị theo thiết kế. */
export const BLOOD_GROUPS = ['O+', 'A+', 'B+', 'AB+', 'O-', 'A-', 'B-', 'AB-']

/**
 * TODO(BE): BE chưa có "định mức tồn tối ưu" theo nhóm máu -> dùng hằng số FE tạm.
 * Khi BE có API, thay bằng dữ liệu thật.
 */
export const STOCK_TARGET: Record<string, number> = Object.fromEntries(BLOOD_GROUPS.map((g) => [g, 260]))

export type StockLevel = 'ok' | 'warn' | 'low'
export const stockLevel = (pct: number): StockLevel => (pct >= 50 ? 'ok' : pct >= 25 ? 'warn' : 'low')

// ---- Chuyển trạng thái thủ công (POST /api/blood-bags/transition-status) ----
/** Chỉ 3 trạng thái này được cập nhật qua quét: Tiếp nhận / Cách ly / Nhập kho. */
export const MANUAL_STATUSES: BagStatus[] = ['READY_TESTING', 'LOCKED', 'AVAILABLE']
/** Tự động cập nhật bởi nghiệp vụ khác. */
export const AUTO_STATUSES: BagStatus[] = ['TESTED_PASSED', 'PROCESSED']
/** Sprint sau: chỉ hiển thị, disable. */
export const COMING_SOON_STATUSES: BagStatus[] = ['RESERVED', 'ISSUED', 'TRANSFUSED']

/** Phản chiếu BloodBagTransitionRules.cs (BE vẫn là nơi quyết định cuối cùng). */
export const allowedManualTargets = (current: string, expired: boolean): BagStatus[] => {
    if (current === 'DISCARDED' || current === 'TRANSFUSED') return []
    const out: BagStatus[] = []
    if (current === 'NEWLY_COLLECTED') out.push('READY_TESTING')
    if (current === 'LOCKED') {
        out.push('READY_TESTING')
        if (!expired) out.push('AVAILABLE')
    }
    if (current === 'TESTED_PASSED' && !expired) out.push('AVAILABLE')
    if (current !== 'LOCKED') out.push('LOCKED')
    return out
}

// ---- Format ----
export const fmtDay = (iso?: string | null) => (iso ? dayjs(iso).format('DD/MM/YYYY') : '—')
export const fmtDateTimeSec = (iso?: string | null) => (iso ? dayjs(iso).format('DD/MM/YYYY · HH:mm:ss') : '—')
export const fmtNum = (n?: number | null) => (n ?? 0).toLocaleString('vi-VN')

export const fmtRemaining = (hours: number) => {
    if (hours <= 0) return 'Đã hết hạn'
    if (hours < 24) return `${Math.floor(hours)} giờ ${Math.round((hours % 1) * 60)} phút`
    return `${Math.floor(hours / 24)} ngày ${Math.floor(hours % 24)} giờ`
}

/** Không có response = mất mạng / server không phản hồi. */
export const isNetworkError = (e: unknown) => !(e as AxiosError)?.response

/** Nhóm máu -> tham số lọc BE. */
export const bloodGroupParams = (group: string) =>
    group === 'ALL'
        ? {}
        : {
            bloodType: group.slice(0, -1),
            rhFactor: group.endsWith('+') ? 'POSITIVE' : 'NEGATIVE',
        }