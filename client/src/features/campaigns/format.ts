import type { Proposal, ProposalStatus, Slot, SiteType } from './types'

export const SITE_TYPE_LABEL: Record<SiteType, string> = { FIXED: 'Cố định', MOBILE: 'Lưu động' }

export const STATUS_LABEL: Record<ProposalStatus, string> = {
    PENDING: 'Đang chờ duyệt',
    APPROVED: 'Đã duyệt',
    REJECTED: 'Bị từ chối',
}

const pad = (n: number) => String(n).padStart(2, '0')

/** 02/06/2025 · 09:42 */
export const fmtDateTime = (iso?: string | null) => {
    if (!iso) return ''
    const d = new Date(iso)
    return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} · ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

/** 07:00–09:00: 16 · 09:00–11:00: 16 */
export const slotsText = (slots: Slot[]) =>
    slots.map((s) => `${s.startTime}–${s.endTime}: ${s.capacity}`).join(' · ')

export const proposalTitle = (p: Proposal) =>
    p.proposalType === 'NEW_SITE'
        ? `${p.siteType === 'MOBILE' ? 'Điểm lưu động mới' : 'Điểm cố định mới'} · dự kiến ${p.totalCapacityPerDay} lượt/ngày`
        : 'Thay đổi lịch & sức chứa'

export const proposalKindText = (p: Proposal) =>
    p.proposalType === 'NEW_SITE' ? 'Tạo điểm hiến mới' : 'Thay đổi lịch & sức chứa'
