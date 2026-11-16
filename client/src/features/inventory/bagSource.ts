import { fmtDay } from './stockFormat'

export type BagSourceType = 'CAMPAIGN' | 'WALK_IN' | 'EXTERNAL' | 'UNKNOWN'
export type SourceFilterValue = '' | 'CAMPAIGN' | 'WALK_IN' | 'EXTERNAL'

export interface BagDonor {
    donorId: number
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
    sessionId: number
    actualVolumeMl?: number | null
    completedAt?: string | null
    collectionIncident?: string | null
}

export interface BagSource {
    sourceType: BagSourceType
    sourceName: string
    campaignId?: number | null
    campaignName?: string | null
    campaignLocation?: string | null
    externalUnitName?: string | null
    externalCode?: string | null
    donor?: BagDonor | null
    collection?: BagCollection | null
}

export interface BagLabSummary {
    testId: number
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

export const SOURCE_META: Record<BagSourceType, { label: string; color: string }> = {
    CAMPAIGN: { label: 'Chiến dịch', color: 'blue' },
    WALK_IN: { label: 'Tại viện', color: 'green' },
    EXTERNAL: { label: 'Ngoài viện', color: 'orange' },
    UNKNOWN: { label: 'Không rõ', color: 'default' },
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
}

export const expiryInfo = (expiredAt: string, isExpired?: boolean): ExpiryInfo => {
    const ms = new Date(expiredAt).getTime() - Date.now()
    if (isExpired || ms <= 0) return { level: 'expired', label: 'Đã hết hạn', color: 'red' }
    const hours = ms / 3_600_000
    if (hours < 24) return { level: 'soon', label: `Còn ${Math.floor(hours)} giờ`, color: 'volcano' }
    const days = Math.floor(hours / 24)
    if (days <= 3) return { level: 'soon', label: `Còn ${days} ngày`, color: 'orange' }
    return { level: 'ok', label: `HSD ${fmtDay(expiredAt)}`, color: 'default' }
}