export type ComponentCode = 'PRBC' | 'PLT' | 'FFP' | 'WB'
export type UrgencyLevel = 'ROUTINE' | 'URGENT' | 'EMERGENCY'
export type RequestStatus = 'DRAFT' | 'PENDING' | 'READY' | 'COMPLETED' | 'REJECTED'

export interface ComponentTypeOption {
    componentTypeId: number
    typeCode: string
    typeName: string
    lifeDays?: number
    storageTemperature?: string
}

export interface SupplyDemandDetailDto {
    bloodType: string // "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-"
    stockUnits: number
    demandUnits: number
    isShortage: boolean
    shortageUnits: number
}

export interface SupplyDemandResponse {
    componentTypeId: number
    componentCode: ComponentCode | string
    componentName: string
    totalAvailableStock: number
    totalPendingDemand: number
    criticalShortages: string[]
    details: SupplyDemandDetailDto[]
}

export interface BloodCoordinationQueueFilter {
    search?: string
    urgencyLevel?: string
    pageNumber?: number
    pageSize?: number
}

export interface BloodRequestItemDetailDto {
    requestItemId: number
    componentTypeId: number
    componentCode: ComponentCode | string
    componentName: string
    requestedUnits: number
    allocatedUnits: number
}

export interface CoordinationQueueItemDto {
    requestId: number
    requestCode: string
    departmentName?: string | null
    doctorName?: string | null
    patientCode: string
    patientName: string
    patientBloodType: string
    patientRh: string
    urgencyLevel: UrgencyLevel
    status: RequestStatus
    totalRequestedUnits: number
    totalAllocatedUnits: number
    createdAt: string
    items: BloodRequestItemDetailDto[]
}

export interface BloodRequestAllocationDto {
    allocationId: number
    bagId: number
    barcode: string
    componentTypeId: number
    componentCode?: ComponentCode | string
    componentName: string
    bloodType?: string
    rhFactor?: string
    volumeMl: number
    expiredAt?: string
    bagStatus?: string
    status: string
    reservedAt?: string
    reservedByName?: string
    issuedAt?: string
    issuedByName?: string
    receivedByName?: string
}

export interface BloodRequestDetailDto {
    requestId: number
    requestCode: string
    doctorId?: number | null
    doctorName?: string | null
    departmentId?: number | null
    departmentName?: string | null
    patientCode: string
    patientName: string
    patientBloodType: string
    patientRh: string
    urgencyLevel: UrgencyLevel
    diagnosis?: string | null
    status: RequestStatus
    currentStep: number
    rejectionReason?: string | null
    totalRequestedUnits: number
    totalAllocatedUnits: number
    createdAt: string
    items: BloodRequestItemDetailDto[]
    allocations: BloodRequestAllocationDto[]
}

export interface RejectBloodRequestDto {
    rejectionReason: string
}

export interface RejectBloodRequestResponse {
    requestId: number
    requestCode: string
    status: RequestStatus
    currentStep: number
    rejectionReason: string
    releasedBagCount: number
    rejectedAt: string
}

export interface AvailableBloodBagDto {
    bagId: number
    barcode: string
    volumeMl: number
    expiresAt: string
    locationName?: string | null
    bloodType: string
    bloodTypeCode: string
    rhFactor: string
    isExactMatch: boolean
    isCompatible: boolean
}

export interface AllocateBloodBagsRequest {
    bagIds: number[]
}

export interface AllocatedBagSummaryDto {
    bagId: number
    barcode: string
    bloodType: string
    volumeMl: number
    expiresAt: string
    isExactMatch: boolean
}

export interface AllocateBloodBagsResponse {
    requestId: number
    requestCode: string
    requestItemId: number
    allocatedCount: number
    itemAllocatedUnits: number
    itemRequestedUnits: number
    requestStatus: RequestStatus
    currentStep: number
    allocatedBags: AllocatedBagSummaryDto[]
}

export interface DispatchBloodBagRequest {
    barcode: string
}

export interface DispatchBloodBagResponse {
    allocationId: number
    bagId: number
    barcode: string
    componentTypeId: number
    componentCode: ComponentCode | string
    componentName: string
    bloodType: string
    volumeMl: number
    expiredAt: string
    bagStatus: string
    requestId: number
    requestCode: string
    patientCode: string
    patientName: string
    patientBloodType: string
    departmentName?: string | null
    doctorName?: string | null
    urgencyLevel: UrgencyLevel
    requestStatus: RequestStatus
    issuedById: number
    issuedByName?: string | null
    issuedAt: string
}

