export type SiteType = 'FIXED' | 'MOBILE'
export type ProposalType = 'NEW_SITE' | 'UPDATE_SCHEDULE'
export type ProposalStatus = 'PENDING' | 'APPROVED' | 'REJECTED'

export interface Slot {
    startTime: string // "HH:mm"
    endTime: string
    capacity: number
}

export interface Site {
    siteId: number
    siteCode: string
    siteName: string
    siteType: SiteType
    address: string
    phoneNumber: string
    isActive: boolean
    totalCapacityPerDay: number
    slots: Slot[]
    createdAt: string
}

export interface Proposal {
    proposalId: number
    proposalCode: string
    proposalType: ProposalType
    status: ProposalStatus
    siteId: number | null
    siteName: string
    siteType: SiteType
    address: string
    phoneNumber: string
    slots: Slot[]
    totalCapacityPerDay: number
    currentSlots: Slot[]
    createdBy: number
    createdByName: string
    createdByRoleCode: string | null
    createdAt: string
    reviewedBy: number | null
    reviewedByName: string | null
    reviewedAt: string | null
    rejectionReason: string | null
}

export interface ProposalPage {
    items: Proposal[]
    total: number
}

export interface ProposalQuery {
    pageNumber?: number
    pageSize?: number
    status?: ProposalStatus
}

export interface CreateProposalRequest {
    proposalType: ProposalType
    siteId?: number
    siteName?: string
    siteType?: SiteType
    address?: string
    phoneNumber?: string
    slots: Slot[]
}
