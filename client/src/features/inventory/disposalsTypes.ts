export type DisposalReason = 'EXPIRED' | 'INFECTIOUS_POSITIVE' | 'HEMOLYSIS' | 'DAMAGED_BAG' | 'OTHER'
export type DisposalStatus = 'PENDING_APPROVAL' | 'APPROVED' | 'DISPOSED' | 'REJECTED' | 'CANCELLED'

export interface DisposalRecord {
    disposalId: string
    disposalCode: string
    totalBags: number
    barcodesSummary: string
    reason: DisposalReason
    reasonText: string
    notes?: string | null
    status: DisposalStatus
    statusText: string
    requestedBy: string
    requestedByName: string
    requestedAt: string
    approvedBy?: string | null
    approvedByName?: string | null
    approvedAt?: string | null
    rejectionReason?: string | null
}

export interface DisposalItem {
    itemId: string
    bagId: string
    barcode: string
    bloodGroup: string
    componentTypeCode: string
    componentTypeName: string
    volumeMl: number
    storageLocationName?: string | null
    collectedAt?: string | null
    expiredAt?: string | null
    previousStatus: string
    previousStatusText: string
    currentBagStatus: string
    currentBagStatusText: string
    isVerified: boolean
}

export interface DisposalDetail extends DisposalRecord {
    items: DisposalItem[]
}

export interface DisposalCandidate {
    bagId: string
    barcode: string
    bloodGroup: string
    componentTypeCode: string
    componentTypeName: string
    volumeMl: number
    storageLocationName?: string | null
    collectedAt: string
    expiredAt: string
    discardReason?: string | null
}

export interface DisposalFilter {
    status?: string
    reason?: string
    search?: string
    pageNumber?: number
    pageSize?: number
}

export interface CandidateFilter {
    search?: string
    pageNumber?: number
    pageSize?: number
}

export interface CreateDisposalRequest {
    barcodes: string[]
    reason: DisposalReason
    notes?: string
}

export interface CreateDisposalResponse {
    disposalId: string
    disposalCode: string
    totalBags: number
    message: string
}

export interface ApproveDisposalRequest {
    isApproved: boolean
    rejectionReason?: string
}