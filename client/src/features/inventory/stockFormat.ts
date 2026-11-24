import dayjs from 'dayjs'
import type { StatusPillTone } from '@/shared/components'
import type { CompatibilityRuleType, StorageZone } from './types'
import type { BagStatus } from './stockTypes'

export const STORAGE_ZONE_META: Record<
    StorageZone,
    { label: string; shortLabel: string; tone: StatusPillTone; hint: string }
> = {
    QUARANTINE: {
        label: 'Khu cách ly',
        shortLabel: 'Cách ly',
        tone: 'warning',
        hint: 'Lưu túi chờ xét nghiệm, khóa tạm thời hoặc kiểm tra lại',
    },
    RELEASED: {
        label: 'Kho sạch (Cấp phát)',
        shortLabel: 'Kho sạch',
        tone: 'success',
        hint: 'Lưu túi đạt chuẩn, sẵn sàng cấp phát lâm sàng',
    },
    DISPOSAL_HOLD: {
        label: 'Khu chờ tiêu hủy',
        shortLabel: 'Chờ hủy',
        tone: 'danger',
        hint: 'Chỉ lưu túi tạm khóa / không đạt tái nhập chờ hủy',
    },
}

export const storageZoneMeta = (zone?: string | null) =>
    STORAGE_ZONE_META[zone as StorageZone] ?? {
        label: zone || '—',
        shortLabel: zone || '—',
        tone: 'neutral' as StatusPillTone,
        hint: '',
    }

export const STORAGE_ZONE_OPTIONS = (Object.keys(STORAGE_ZONE_META) as StorageZone[]).map((z) => ({
    value: z,
    label: STORAGE_ZONE_META[z].label,
}))

export const COMPATIBILITY_RULE_META: Record<
    CompatibilityRuleType,
    { label: string; tone: StatusPillTone; desc: string }
> = {
    RBC: {
        label: 'RBC - Hồng cầu',
        tone: 'danger',
        desc: 'O nhận O; A nhận A/O; B nhận B/O; AB nhận tất cả; Rh- chỉ nhận Rh-',
    },
    PLASMA: {
        label: 'PLASMA - Huyết tương',
        tone: 'info',
        desc: 'Ngược chiều hồng cầu: O nhận tất cả; A nhận A/AB; B nhận B/AB; AB nhận AB (không xét Rh)',
    },
    PLATELET: {
        label: 'PLATELET - Tiểu cầu',
        tone: 'warning',
        desc: 'ABO theo chiều huyết tương (O nhận tất cả, AB nhận AB) và bắt buộc Rh- chỉ nhận Rh-',
    },
}

export const BAG_STATUS_META: Record<
    BagStatus,
    { label: string; color: string; tone: StatusPillTone }
> = {
    NEWLY_COLLECTED: { label: 'Mới thu gom', color: 'default', tone: 'neutral' },
    READY_TESTING: { label: 'Chờ xét nghiệm', color: 'gold', tone: 'warning' },
    TESTED_PASSED: { label: 'Đạt xét nghiệm', color: 'cyan', tone: 'info' },
    AVAILABLE: { label: 'Có sẵn', color: 'green', tone: 'success' },
    LOCKED: { label: 'Tạm khóa', color: 'red', tone: 'danger' },
    RESERVED: { label: 'Đặt chỗ', color: 'blue', tone: 'info' },
    ISSUED: { label: 'Đã cấp phát', color: 'purple', tone: 'purple' },
    TRANSFUSED: { label: 'Đã truyền', color: 'default', tone: 'neutral' },
    DISCARDED: { label: 'Đã hủy', color: 'default', tone: 'neutral' },
    PROCESSED: { label: 'Đã điều chế', color: 'geekblue', tone: 'primary' },
}

export const statusMeta = (s: string) =>
    BAG_STATUS_META[s as BagStatus] ?? { label: s, color: 'default', tone: 'neutral' as StatusPillTone }

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
/** Thuộc luồng cấp phát và truyền máu lâm sàng, không chuyển thủ công tại quét kho. */
export const COMING_SOON_STATUSES: BagStatus[] = ['RESERVED', 'ISSUED', 'TRANSFUSED']

/**
 * Phản chiếu BloodBagTransitionRules.cs (BE vẫn là nơi quyết định cuối cùng).
 * Túi đã hết hạn, đã xuất kho (ISSUED), đã truyền (TRANSFUSED), đã tiêu hủy (DISCARDED),
 * hoặc đã điều chế (PROCESSED) không còn đích chuyển thủ công tại màn quét kho.
 */
export const allowedManualTargets = (current: string, expired: boolean): BagStatus[] => {
    if (
        current === 'DISCARDED' ||
        current === 'TRANSFUSED' ||
        current === 'ISSUED' ||
        current === 'PROCESSED'
    ) {
        return []
    }
    if (expired) return []
    const out: BagStatus[] = []
    if (current === 'NEWLY_COLLECTED') out.push('READY_TESTING')
    if (current === 'LOCKED') out.push('READY_TESTING', 'AVAILABLE')
    if (current === 'TESTED_PASSED') out.push('AVAILABLE')
    if (current !== 'LOCKED') out.push('LOCKED')
    return out
}

// ---- Format ----
export const fmtDay = (iso?: string | null) => (iso ? dayjs(iso).format('DD/MM/YYYY') : '—')
export const fmtDateTimeSec = (iso?: string | null) => (iso ? dayjs(iso).format('DD/MM/YYYY - HH:mm:ss') : '—')
export const fmtNum = (n?: number | null) => (n ?? 0).toLocaleString('vi-VN')

export const fmtRemaining = (hours: number) => {
    if (hours <= 0) return 'Đã hết hạn'
    if (hours < 24) return `${Math.floor(hours)} giờ ${Math.round((hours % 1) * 60)} phút`
    return `${Math.floor(hours / 24)} ngày ${Math.floor(hours % 24)} giờ`
}

/** Nhóm máu -> tham số lọc BE. */
export const bloodGroupParams = (group: string) =>
    group === 'ALL'
        ? {}
        : {
            bloodType: group.slice(0, -1),
            rhFactor: group.endsWith('+') ? 'POSITIVE' : 'NEGATIVE',
        }