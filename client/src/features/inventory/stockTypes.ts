export type BagStatus =
    | 'NEWLY_COLLECTED' | 'READY_TESTING' | 'TESTED_PASSED' | 'AVAILABLE' | 'LOCKED'
    | 'RESERVED' | 'ISSUED' | 'TRANSFUSED' | 'DISCARDED' | 'PROCESSED'

/** BloodBagDto */
export interface BloodBag {
    bagId: number
    barcode: string
    parentBagId?: number | null
    parentBarcode?: string | null
    componentTypeId: number
    componentTypeCode: string
    componentTypeName: string
    bloodType: string
    rhFactor: string
    bloodGroup: string
    volumeMl: number
    status: string
    storageLocationId?: number | null
    storageLocationName?: string | null
    collectedAt: string
    expiredAt: string
    createdAt: string
}

export interface BagQuery {
    status?: string
    bloodType?: string
    rhFactor?: string
    componentType?: string
    search?: string
    locationId?: number
    pageNumber?: number
    pageSize?: number
}

export interface InventoryOverview {
    totalBags: number
    totalVolumeMl: number
    availableBags: number
    expiringSoonBags: number
    disposalPendingBags: number
    testingOrProcessingBags: number
}

export interface InventoryBreakdownItem {
    bloodType: string
    rhFactor: string
    bloodGroup: string
    componentType: string
    componentTypeName?: string | null
    status: string
    totalBags: number
    totalVolumeMl: number
}

export interface InventorySummary {
    summaryDate: string
    overview: InventoryOverview
    breakdown: InventoryBreakdownItem[]
}

export interface ExpiringSoonBag {
    bagId: number
    barcode: string
    bloodType: string
    rhFactor: string
    bloodGroup: string
    componentTypeId: number
    componentTypeCode: string
    componentTypeName: string
    volumeMl: number
    status: string
    storageLocationId?: number | null
    storageLocationName?: string | null
    collectedAt: string
    expiredAt: string
    daysRemaining: number
    hoursRemaining: number
}

export interface ExpiringQuery {
    daysThreshold?: number
    bloodType?: string
    rhFactor?: string
    componentType?: string
    storageLocationId?: number
    status?: string
    pageNumber?: number
    pageSize?: number
}

export interface LocationQuery {
    search?: string
    isFull?: boolean
    isActive?: boolean
    availableOnly?: boolean
    pageNumber?: number
    pageSize?: number
}

export interface StorageLocationInput {
    refrigeratorName: string
    shelfNumber: string
    targetTemperature: number
    isFull: boolean
    isActive: boolean
}

export interface RelocateRequest {
    newStorageLocationId: number
    note?: string
}

export interface RelocateResponse {
    bagId: number
    barcode: string
    previousStorageLocationName?: string | null
    newStorageLocationName: string
    message: string
}

export interface TimelineStep {
    step: number
    statusCode: string
    name: string
    completed: boolean
    timestamp?: string | null
}

export interface BagScan {
    bagId: number
    barcode: string
    bloodType: string
    rhFactor: string
    bloodGroup: string
    componentType: string
    componentTypeCode: string
    volumeMl: number
    currentStatus: string
    currentStatusLabel: string
    currentStep: number
    nextStatus?: string | null
    nextStatusLabel?: string | null
    nextStep?: number | null
    isLocked: boolean
    isDiscarded: boolean
    isExpired: boolean
    expiredAt: string
    collectedAt: string
    storageLocation?: { locationId: number; locationName: string } | null
    timeline: TimelineStep[]
}

export interface TransitionRequest {
    barcode: string
    targetStatus: BagStatus
    storageLocationId?: number
    notes?: string
}

export interface TransitionResponse {
    barcode: string
    previousStatus: string
    previousStatusLabel: string
    currentStatus: string
    currentStatusLabel: string
    storageLocation?: { locationId: number; locationName: string } | null
    updatedAt: string
    message: string
}

export interface StatusLog {
    logId: number
    barcode: string
    bloodGroup: string
    previousStatus: string
    previousStatusName: string
    newStatus: string
    newStatusName: string
    performedBy: string
    notes?: string | null
    timestamp: string
}

export interface StatusLogsResponse {
    total: number
    items: StatusLog[]
}

/** Lệnh chuyển trạng thái bị mất mạng, lưu ở localStorage chờ gửi lại. */
export interface OfflineTransition extends TransitionRequest {
    id: string
    createdAt: string
    error?: string
}

/** Tối thiểu để đổi vị trí một túi (BloodBag và ExpiringSoonBag đều thỏa). */
export type RelocatableBag = Pick<
    BloodBag,
    'bagId' | 'barcode' | 'bloodGroup' | 'componentTypeCode' | 'storageLocationId' | 'storageLocationName'
>