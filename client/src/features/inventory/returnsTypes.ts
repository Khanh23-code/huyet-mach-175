export type ReturnFinalAction = 'PENDING_INSPECTION' | 'RESTOCK' | 'DISCARD'

export interface PendingBloodReturnFilter {
    search?: string
    pageNumber?: number
    pageSize?: number
}

export interface PendingBloodReturnDto {
    returnId: number
    allocationId: number
    bagId: number
    barcode: string
    componentCode: string
    componentName: string
    bloodType: string
    volumeMl: number
    expiredAt: string
    isExpired: boolean
    requestId: number
    requestCode: string
    patientCode: string
    patientName: string
    departmentName?: string | null
    returnedById: number
    returnedByName?: string | null
    returnReason: string
    finalAction: ReturnFinalAction
    bagStatus: string
    processedAt: string
}

export interface ReturnInspectionDto {
    returnId: number
    allocationId: number
    bagId: number
    barcode: string
    componentTypeId: number
    componentCode: string
    componentName: string
    bloodType: string
    volumeMl: number
    collectedAt: string
    expiredAt: string
    isExpired: boolean
    bagStatus: string
    previousLocationId?: number | null
    previousLocationName?: string | null
    requestId: number
    requestCode: string
    patientCode: string
    patientName: string
    departmentName?: string | null
    doctorName?: string | null
    returnedById: number
    returnedByName?: string | null
    returnReason: string
    qualified: boolean
    finalAction: ReturnFinalAction
    returnedAt: string
}

export interface BloodReturnInspectionChecklistDto {
    isBagIntact: boolean
    isLabelClear: boolean
    isTempInRange: boolean
    isOutsideTimeValid: boolean
    isUnusedFullVolume: boolean
}

export interface ProcessBloodReturnRequest {
    action: 'RESTOCK' | 'DISCARD'
    checklist?: BloodReturnInspectionChecklistDto
    newStorageLocationId?: number
    discardReason?: string
}

export interface ProcessBloodReturnResponse {
    returnId: number
    allocationId: number
    bagId: number
    barcode: string
    componentName: string
    bloodType: string
    qualified: boolean
    finalAction: ReturnFinalAction
    bagStatus: string
    allocationStatus: string
    newLocationId?: number | null
    newLocationName?: string | null
    discardReason?: string | null
    inspectedById: number
    inspectedByName?: string | null
    processedAt: string
    message: string
}
