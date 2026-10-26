export type CampaignType = 'EVENT' | 'RECURRING'

export type CampaignStatus =
    | 'DRAFT'
    | 'PENDING_APPROVAL'
    | 'APPROVED'
    | 'IN_PROGRESS'
    | 'COMPLETED'
    | 'CANCELLED'
    | 'REJECTED'

export interface CampaignSlot {
    slotId?: number
    startTime: string // Định dạng "HH:mm:ss" hoặc "HH:mm"
    endTime: string   // Định dạng "HH:mm:ss" hoặc "HH:mm"
    capacity: number  // Sức chứa tối đa trong khung giờ này
}

export interface CampaignListItem {
    campaignId: number
    campaignName: string
    description?: string | null
    locationName: string
    campaignType: CampaignType
    operatingDays?: string | null // Ví dụ: "2,4,6" khi campaignType = RECURRING
    startDate: string // "YYYY-MM-DD"
    endDate: string   // "YYYY-MM-DD"
    targetDonations: number
    maxCapacity?: number | null
    status: CampaignStatus
    totalRegistered: number
    totalCollected: number
    createdBy?: number
    createdByName?: string
    createdAt: string
    slots?: CampaignSlot[]
}

export interface CampaignDetail extends CampaignListItem {
    totalCheckedIn: number
    completionRate: number
    remainingDays: number
    createdBy: number
    createdByName: string
    approvedBy?: number | null
    approvedByName?: string | null
    approvedAt?: string | null
    rejectionReason?: string | null
    cancelledBy?: number | null
    cancelledByName?: string | null
    cancelledAt?: string | null
    slots: CampaignSlot[]
}

export interface CampaignFilterQuery {
    pageNumber?: number
    pageSize?: number
    status?: CampaignStatus
    fromDate?: string
    toDate?: string
    keyword?: string
}

export interface PagedResult<T> {
    items: T[]
    totalCount: number
    pageNumber: number
    pageSize: number
}

export interface CreateCampaignRequest {
    campaignName: string
    description?: string
    locationName: string
    campaignType: CampaignType
    operatingDays?: string
    startDate: string
    endDate: string
    targetDonations: number
    maxCapacity?: number
    slots: {
        startTime: string
        endTime: string
        capacity: number
    }[]
}

export interface UpdateCampaignRequest extends CreateCampaignRequest {}

export interface ApproveCampaignRequest {
    isApproved: boolean
    rejectionReason?: string
}

export interface UpdateCampaignStatusRequest {
    status: CampaignStatus
}
