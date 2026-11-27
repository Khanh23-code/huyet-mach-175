export type ReturnFinalAction = 'PENDING_INSPECTION' | 'RESTOCK' | 'DISCARD' | 'CANCELLED'

export interface PendingBloodReturnFilter {
    search?: string
    pageNumber?: number
    pageSize?: number
}

export interface PendingBloodReturnDto {
    returnId: string
    returnCode: string
    allocationId: string
    bagId: string
    barcode: string
    componentCode: string
    componentName: string
    bloodType: string
    volumeMl: number
    expiredAt: string
    isExpired: boolean
    requestId: string
    requestCode: string
    patientCode: string
    patientName: string
    departmentName?: string | null
    returnedById: string
    returnedByName?: string | null
    returnReason: string
    finalAction: ReturnFinalAction
    bagStatus: string
    processedAt: string
    allowRestock: boolean
    previousRestockCount: number
    maxRestockCount: number
    maxOutsideMinutesForRestock?: number | null
    minTemp: number
    maxTemp: number
    storageTemperatureLabel: string
    requiresMandatoryDiscard: boolean
    mandatoryDiscardReasons: string[]
    clinicalChecklistWarnings: string[]
}

export interface ReturnInspectionDto {
    returnId: string
    returnCode: string
    allocationId: string
    bagId: string
    barcode: string
    componentTypeId: string
    componentCode: string
    componentName: string
    bloodType: string
    volumeMl: number
    collectedAt: string
    expiredAt: string
    isExpired: boolean
    bagStatus: string
    previousLocationId?: string | null
    previousLocationName?: string | null
    requestId: string
    requestCode: string
    patientCode: string
    patientName: string
    departmentName?: string | null
    doctorName?: string | null
    returnedById: string
    returnedByName?: string | null
    returnReason: string
    qualified: boolean
    finalAction: ReturnFinalAction
    returnedAt: string
    allowRestock: boolean
    previousRestockCount: number
    maxRestockCount: number
    maxOutsideMinutesForRestock?: number | null
    minutesSinceIssued?: number | null
    minTemp: number
    maxTemp: number
    storageTemperatureLabel: string
    requiresMandatoryDiscard: boolean
    mandatoryDiscardReasons: string[]
    clinicalChecklistWarnings: string[]
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
    newStorageLocationId?: string
    discardReason?: string
}

export interface ProcessBloodReturnResponse {
    returnId: string
    returnCode: string
    allocationId: string
    bagId: string
    barcode: string
    componentName: string
    bloodType: string
    qualified: boolean
    finalAction: ReturnFinalAction
    bagStatus: string
    allocationStatus: string
    newLocationId?: string | null
    newLocationName?: string | null
    discardReason?: string | null
    inspectedById: string
    inspectedByName?: string | null
    processedAt: string
    message: string
}
