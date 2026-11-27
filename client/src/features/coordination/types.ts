export type {
    CancelHandoverSessionRequest,
    CreateHandoverSessionRequest,
    HandoverSessionDto,
    HandoverSessionItemDto,
    HandoverSessionStatus,
    PendingHandoverSessionFilter,
    ReservedHandoverBagDto,
    ReservedHandoverFilter,
    ReservedHandoverRequestDto,
} from '@/shared/types/handover'

export type ComponentCode = 'PRBC' | 'PLT' | 'FFP' | 'WB'
export type UrgencyLevel = 'ROUTINE' | 'URGENT' | 'EMERGENCY'
export type RequestStatus = 'DRAFT' | 'PENDING' | 'READY' | 'COMPLETED' | 'REJECTED'

export interface ComponentTypeOption {
    componentTypeId: string
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
    componentTypeId: string
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
    requestItemId: string
    componentTypeId: string
    componentCode: ComponentCode | string
    componentName: string
    requestedUnits: number
    allocatedUnits: number
}

export interface CoordinationQueueItemDto {
    requestId: string
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
    allocationId: string
    bagId: string
    barcode: string
    componentTypeId: string
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
    requestId: string
    requestCode: string
    doctorId?: string | null
    doctorName?: string | null
    departmentId?: string | null
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
    requestId: string
    requestCode: string
    status: RequestStatus
    currentStep: number
    rejectionReason: string
    releasedBagCount: number
    rejectedAt: string
}

export interface AvailableBloodBagDto {
    bagId: string
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
    bagIds: string[]
}

export interface AllocatedBagSummaryDto {
    bagId: string
    barcode: string
    bloodType: string
    volumeMl: number
    expiresAt: string
    isExactMatch: boolean
}

export interface AllocateBloodBagsResponse {
    requestId: string
    requestCode: string
    requestItemId: string
    allocatedCount: number
    itemAllocatedUnits: number
    itemRequestedUnits: number
    requestStatus: RequestStatus
    currentStep: number
    allocatedBags: AllocatedBagSummaryDto[]
}

