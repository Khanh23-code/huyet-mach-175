import type { AboGroup, RhValue } from '@/shared/types/medical'

export type BloodType = AboGroup // 'A' | 'B' | 'AB' | 'O'
export type RhFactor = RhValue // 'POSITIVE' | 'NEGATIVE'

export type UrgencyLevel = 'ROUTINE' | 'URGENT' | 'EMERGENCY'

export type RequestStatus = 'DRAFT' | 'PENDING' | 'READY' | 'COMPLETED' | 'REJECTED'

export type ComponentCode = 'WB' | 'PRBC' | 'FFP' | 'PLT'

export type AllocationStatus = 'RESERVED' | 'ISSUED' | 'COMPLETED' | 'RETURNED'

export type ReturnFinalAction = 'PENDING_INSPECTION' | 'RESTOCK' | 'DISCARD'

export interface ClinicalBloodRequestDto {
    requestId: number
    requestCode: string
    patientCode: string
    patientName: string
    patientBloodType: BloodType
    patientRh: RhFactor
    totalRequestedUnits: number
    totalAllocatedUnits: number
    urgencyLevel: UrgencyLevel
    status: RequestStatus
    currentStep: number
    diagnosis?: string | null
    createdAt: string
}

export interface ClinicalBloodRequestFilter {
    search?: string
    urgencyLevel?: string
    status?: string
    pageNumber?: number
    pageSize?: number
}

export interface CreateBloodRequestItemDto {
    componentTypeId: number
    requestedUnits: number
}

export interface CreateBloodRequestDto {
    patientCode: string
    patientName: string
    patientBloodType: BloodType
    patientRh: RhFactor
    urgencyLevel: UrgencyLevel
    diagnosis?: string
    isDraft: boolean
    items: CreateBloodRequestItemDto[]
}

export type UpdateBloodRequestDto = Omit<CreateBloodRequestDto, 'isDraft'>

export interface CreateBloodRequestResponse {
    requestId: number
    requestCode: string
    status: RequestStatus
    message: string
}

export interface SubmitBloodRequestResponse {
    requestId: number
    requestCode: string
    status: RequestStatus
    currentStep: number
    submittedAt: string
}

export interface BloodRequestItemDetailDto {
    requestItemId: number
    componentTypeId: number
    componentCode: ComponentCode
    componentName: string
    requestedUnits: number
    allocatedUnits: number
}

export interface BloodRequestAllocationDto {
    allocationId: number
    bagId: number
    barcode: string
    componentTypeId: number
    componentCode?: ComponentCode
    componentName: string
    bloodType?: BloodType
    rhFactor?: RhFactor
    volumeMl: number
    expiredAt?: string
    bagStatus?: string
    status: AllocationStatus
    reservedAt?: string
    reservedByName?: string
    issuedAt?: string
    issuedByName?: string
    receivedByName?: string
}

export interface BloodRequestDetailDto {
    requestId: number
    requestCode: string
    doctorId?: number
    doctorName?: string
    departmentId?: number
    departmentName?: string
    patientCode: string
    patientName: string
    patientBloodType: BloodType
    patientRh: RhFactor
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

export interface PendingClinicalReceiptFilter {
    doctorId?: number
    doctorName?: string
    requestCode?: string
}

export interface PendingReceiptBagDto {
    allocationId: number
    requestId: number
    requestCode: string
    doctorId?: number
    doctorName?: string
    departmentName?: string
    isOwnRequest: boolean
    urgencyLevel: UrgencyLevel
    patientName: string
    patientCode: string
    bagId: number
    barcode: string
    componentCode: ComponentCode
    componentName: string
    bloodType: string
    volumeMl: number
    expiresAt: string
    issuedAt?: string
    issuedByName?: string
}

export interface ConfirmReceiptRequest {
    allocationId: number
    scannedBarcode: string
    expectedPatientCode?: string
}

export interface ConfirmReceiptResponse {
    allocationId: number
    requestId: number
    requestCode: string
    patientCode: string
    patientName: string
    bagId: number
    barcode: string
    componentCode: ComponentCode
    componentName: string
    bloodType: string
    volumeMl: number
    allocationStatus: AllocationStatus
    requestStatus: RequestStatus
    currentStep: number
    receivedById: number
    receivedByName?: string
    confirmedAt: string
}

export interface BloodReturnChecklistDto {
    isUnusedFullVolume: boolean
    isBagIntact: boolean
    isLabelClear: boolean
    isStoredUnder30Mins: boolean
}

export interface CreateBloodReturnRequest {
    allocationId: number
    returnReason: string
    checklist: BloodReturnChecklistDto
}

export interface CreateBloodReturnResponse {
    returnId: number
    allocationId: number
    requestId: number
    requestCode: string
    patientCode: string
    patientName: string
    bagId: number
    barcode: string
    componentName: string
    bloodType: string
    returnReason: string
    qualified: boolean
    finalAction: ReturnFinalAction
    bagStatus: string
    returnedById: number
    returnedByName?: string
    processedAt: string
}

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
    componentCode: ComponentCode
    componentName: string
    bloodType: string
    volumeMl: number
    expiredAt: string
    isExpired: boolean
    requestId: number
    requestCode: string
    patientCode: string
    patientName: string
    departmentName?: string
    returnedById: number
    returnedByName?: string
    returnReason: string
    finalAction: ReturnFinalAction
    bagStatus: string
    processedAt: string
}

export interface ComponentTypeOption {
    componentTypeId: number
    typeCode: string
    typeName: string
    lifeDays?: number
    storageTemperature?: string
    description?: string
}

/** Bệnh nhân đồng bộ từ hệ thống HIS / EMR (Mockdata theo yêu cầu) */
export interface HisPatientDto {
    patientCode: string
    patientName: string
    age: number
    gender: 'Nam' | 'Nữ'
    room: string
    bed: string
    diagnosis: string
    bloodType: BloodType
    rhFactor: RhFactor
    bloodGroupDisplay: string // "O+", "A+",...
    initials: string
}

export interface EligibleReturnBag {
    allocationId: number
    bagId: number
    barcode: string
    componentCode?: ComponentCode | string
    componentName: string
    bloodType: string
    volumeMl: number
    expiredAt?: string
    isExpired: boolean
    requestId: number
    requestCode: string
    patientCode: string
    patientName: string
    allocationStatus: AllocationStatus
    bagStatus: 'ISSUED'
    issuedAt?: string
    receivedByName?: string
    isReceivedAtClinical: boolean
}

export interface IneligibleReturnBag {
    allocationId: number
    bagId: number
    barcode: string
    componentName: string
    bloodType: string
    volumeMl: number
    requestCode: string
    patientCode: string
    patientName: string
    allocationStatus: AllocationStatus
    bagStatus?: string
    ineligibleReason: string
}

