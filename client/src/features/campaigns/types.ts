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
    startTime: string
    endTime: string
    capacity: number
}

export interface CampaignListItem {
    campaignId: number
    campaignName: string
    description?: string | null
    locationName: string
    campaignType: CampaignType
    operatingDays?: string | null
    startDate: string
    endDate: string
    targetDonations: number
    maxCapacity?: number | null
    status: CampaignStatus
    rejectionReason?: string | null
    cancellationReason?: string | null
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
    cancellationReason?: string | null
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

export interface CancelCampaignRequest {
    reason: string
}

export interface UpdateCampaignStatusRequest {
    status: CampaignStatus
}

export interface CampaignDeferralReasonStat {
    reason: string
    count: number
}

export interface CampaignIncident {
    sessionId: number
    appointmentId: number
    donorName: string
    donorCitizenId: string
    targetVolumeMl: string
    actualVolumeMl?: number | null
    incidentDescription: string
    completedAt?: string | null
}

export interface CampaignVolumeStat {
    volumeType: string
    volumeMl: number
    count: number
    totalMl: number
}

export interface CampaignBloodTypeStat {
    bloodType: string
    count: number
    percentage: number
}

export interface CampaignRhStat {
    rhFactor: string
    count: number
    percentage: number
}

export interface CampaignSlotSummary {
    slotId: number
    startTime: string
    endTime: string
    capacity: number
    totalRegistered: number
    totalCheckedIn: number
    totalCollected: number
    totalVolumeMl: number
}

export interface CampaignSummary {
    campaignId: number
    campaignName: string
    description?: string | null
    locationName: string
    campaignType: CampaignType
    operatingDays?: string | null
    startDate: string
    endDate: string
    status: CampaignStatus
    createdByName: string

    targetDonations: number
    totalCollectedUnits: number
    shortfallUnits: number
    exceededUnits: number
    completionRate: number
    totalVolumeMl: number

    totalRegistered: number
    totalCheckedIn: number
    scheduledCheckedIn: number
    walkInCheckedIn: number
    pendingArrival: number
    noShowCount: number
    cancelledCount: number
    attendanceRate: number

    totalScreened: number
    qualifiedCount: number
    deferredCount: number
    unqualifiedCount: number
    pendingScreeningCount: number
    topDeferralReasons: CampaignDeferralReasonStat[]

    totalSessions: number
    completedSessions: number
    abortedSessions: number
    waitingCollectionSessions: number
    incidentCount: number
    incidents: CampaignIncident[]

    volumeBreakdown: CampaignVolumeStat[]
    bloodTypeBreakdown: CampaignBloodTypeStat[]
    rhBreakdown: CampaignRhStat[]
    slots: CampaignSlotSummary[]
}

export interface DonorImportRowError {
    rowNumber: number
    stt?: number
    citizenId?: string | null
    fullName?: string | null
    errorMessage: string
}

export interface DonorImportResult {
    campaignId: number
    campaignName: string
    totalRows: number
    successCount: number
    createdDonorsCount: number
    reusedDonorsCount: number
    createdAppointmentsCount: number
    surveysIncludedCount: number
    errorsCount: number
    errors: DonorImportRowError[]
}