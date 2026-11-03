import type { SessionStatus, TargetVolume } from './types'

/** "VOL_350" -> 350 */
export const volumeMl = (v?: TargetVolume | string | null) => {
    const n = Number(String(v ?? '').replace('VOL_', ''))
    return Number.isFinite(n) ? n : 0
}

export const VOLUME_CHOICES = [250, 350, 450]

export const SESSION_META: Record<SessionStatus, { label: string; color: string }> = {
    WAITING_COLLECTION: { label: 'Chờ lấy máu', color: 'blue' },
    COMPLETED: { label: 'Hoàn tất', color: 'green' },
    ABORTED: { label: 'Sự cố', color: 'red' },
}

/** ISO không kèm múi giờ coi như UTC (BE lưu UTC). */
export const parseUtc = (iso: string) =>
    Date.parse(/(Z|[+-]\d{2}:?\d{2})$/i.test(iso) ? iso : `${iso}Z`)

const pad = (n: number) => String(n).padStart(2, '0')

export const fmtElapsed = (totalSec: number) => {
    const h = Math.floor(totalSec / 3600)
    const m = Math.floor((totalSec % 3600) / 60)
    const s = totalSec % 60
    return h > 0 ? `${pad(h)}:${pad(m)}:${pad(s)}` : `${pad(m)}:${pad(s)}`
}