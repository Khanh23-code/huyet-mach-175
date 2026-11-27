import type { StatusPillTone } from '@/shared/components'
import { fmtDay } from './stockFormat'

export type BagSourceType = 'CAMPAIGN' | 'WALK_IN' | 'EXTERNAL' | 'UNKNOWN'
export type SourceFilterValue = '' | 'CAMPAIGN' | 'WALK_IN' | 'EXTERNAL'

export interface BagDonor {
    donorId: string
    fullName: string
    citizenId: string
    gender: string
    dateOfBirth: string
    age: number
    phoneNumber: string
    declaredBloodGroup?: string | null
    totalDonations: number
    lastDonationDate?: string | null
}

export interface BagCollection {
    sessionId: string
    actualVolumeMl?: number | null
    completedAt?: string | null
    collectionIncident?: string | null
}

export interface BagSource {
    sourceType: BagSourceType
    sourceName: string
    campaignId?: string | null
    campaignName?: string | null
    campaignLocation?: string | null
    externalUnitName?: string | null
    externalCode?: string | null
    donor?: BagDonor | null
    collection?: BagCollection | null
}

export interface BagLabSummary {
    testId: string
    testedAt: string
    technicianName: string
    hivResult: string
    hbvResult: string
    hcvResult: string
    syphilisResult: string
    irregularAntibody: string
    confirmedBloodGroup: string
    overallConclusion: 'PASSED' | 'FAILED'
}

export const SOURCE_META: Record<
    BagSourceType,
    { label: string; color: string; tone: StatusPillTone }
> = {
    CAMPAIGN: { label: 'Chiến dịch', color: 'blue', tone: 'info' },
    WALK_IN: { label: 'Tại viện', color: 'green', tone: 'success' },
    EXTERNAL: { label: 'Ngoài viện', color: 'orange', tone: 'urgent' },
    UNKNOWN: { label: 'Không rõ', color: 'default', tone: 'neutral' },
}

export const SOURCE_FILTER_OPTIONS: { label: string; value: SourceFilterValue }[] = [
    { label: 'Tất cả', value: '' },
    { label: 'Chiến dịch', value: 'CAMPAIGN' },
    { label: 'Tại viện', value: 'WALK_IN' },
    { label: 'Ngoài viện', value: 'EXTERNAL' },
]

export interface ExpiryInfo {
    level: 'expired' | 'soon' | 'ok'
    label: string
    color: string
    tone: StatusPillTone
}

export const expiryInfo = (expiredAt: string, isExpired?: boolean): ExpiryInfo => {
    const ms = new Date(expiredAt).getTime() - Date.now()
    if (isExpired || ms <= 0) {
        return { level: 'expired', label: 'Đã hết hạn', color: 'red', tone: 'danger' }
    }
    const hours = ms / 3_600_000
    if (hours < 24) {
        return { level: 'soon', label: `Còn ${Math.floor(hours)} giờ`, color: 'volcano', tone: 'blood' }
    }
    const days = Math.floor(hours / 24)
    if (days <= 3) {
        return { level: 'soon', label: `Còn ${days} ngày`, color: 'orange', tone: 'warning' }
    }
    return { level: 'ok', label: `HSD ${fmtDay(expiredAt)}`, color: 'default', tone: 'neutral' }
}