import type { AboGroup, RhValue } from '@/shared/types/medical'
export type {
    ConfirmHandoverSessionRequest,
    HandoverSessionDto,
    HandoverSessionItemDto,
    HandoverSessionStatus,
    PendingHandoverSessionFilter,
} from '@/shared/types/handover'

export type BloodType = AboGroup // 'A' | 'B' | 'AB' | 'O'
export type RhFactor = RhValue // 'POSITIVE' | 'NEGATIVE'

export type UrgencyLevel = 'ROUTINE' | 'URGENT' | 'EMERGENCY'

export type RequestStatus = 'DRAFT' | 'PENDING' | 'READY' | 'COMPLETED' | 'REJECTED'

export type ComponentCode = 'WB' | 'PRBC' | 'FFP' | 'PLT'

export type AllocationStatus = 'RESERVED' | 'ISSUED' | 'COMPLETED' | 'RETURNED'

export type ReturnFinalAction = 'PENDING_INSPECTION' | 'RESTOCK' | 'DISCARD' | 'CANCELLED'

export interface ClinicalBloodRequestDto {
    requestId: string
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
    componentTypeId: string
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
    requestId: string
    requestCode: string
    status: RequestStatus
    message: string
}

export interface SubmitBloodRequestResponse {
    requestId: string
    requestCode: string
    status: RequestStatus
    currentStep: number
    submittedAt: string
}

export interface BloodRequestItemDetailDto {
    requestItemId: string
    componentTypeId: string
    componentCode: ComponentCode
    componentName: string
    requestedUnits: number
    allocatedUnits: number
}

export interface BloodRequestAllocationDto {
    allocationId: string
    bagId: string
    barcode: string
    componentTypeId: string
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
    requestId: string
    requestCode: string
    doctorId?: string
    doctorName?: string
    departmentId?: string
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

export interface TransfusionScanDto {
    allocationId: string
    bagId: string
    barcode: string
    componentTypeId: string
    componentCode: ComponentCode
    componentName: string
    bloodType: string
    volumeMl: number
    expiredAt: string
    isExpired: boolean
    bagStatus: string
    requestId: string
    requestCode: string
    patientName: string
    patientCode: string
    patientBloodType: string
    departmentName?: string | null
    doctorName?: string | null
    isOwnRequest: boolean
    issuedAt?: string | null
    receivedByName?: string | null
}

export interface ConfirmTransfusionRequest {
    barcode: string
    allocationId?: string
    expectedPatientCode: string
}

export interface ConfirmTransfusionResponse {
    bagId: string
    barcode: string
    bagStatus: string
    allocationId: string
    requestId: string
    requestCode: string
    patientCode: string
    patientName: string
    patientBloodType: string
    bloodType: string
    componentName: string
    confirmedById: string
    confirmedByName?: string | null
    transfusedAt: string
}

export interface BloodReturnChecklistDto {
    isUnusedFullVolume: boolean
    isBagIntact: boolean
    isLabelClear: boolean
    isStoredUnder30Mins: boolean
}

export interface CreateBloodReturnRequest {
    allocationId: string
    returnReason: string
    checklist: BloodReturnChecklistDto
}

export interface CreateBloodReturnResponse {
    returnId: string
    returnCode: string
    allocationId: string
    requestId: string
    requestCode: string
    patientCode: string
    patientName: string
    bagId: string
    barcode: string
    componentName: string
    bloodType: string
    returnReason: string
    qualified: boolean
    finalAction: ReturnFinalAction
    bagStatus: string
    returnedById: string
    returnedByName?: string
    processedAt: string
    isEligibleForRestockEvaluation: boolean
    warningFlags: string[]
}

export interface CancelBloodReturnRequest {
    cancelReason?: string
}

export interface CancelBloodReturnResponse {
    returnId: string
    returnCode: string
    allocationId: string
    bagId: string
    barcode: string
    finalAction: ReturnFinalAction
    bagStatus: string
    cancelReason?: string | null
    cancelledById: string
    cancelledByName?: string | null
    cancelledAt: string
}

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
    componentCode: ComponentCode
    componentName: string
    bloodType: string
    volumeMl: number
    expiredAt: string
    isExpired: boolean
    requestId: string
    requestCode: string
    patientCode: string
    patientName: string
    departmentName?: string
    returnedById: string
    returnedByName?: string
    returnReason: string
    finalAction: ReturnFinalAction
    bagStatus: string
    processedAt: string
    allowRestock?: boolean
    previousRestockCount?: number
    maxRestockCount?: number
    maxOutsideMinutesForRestock?: number | null
    minTemp?: number
    maxTemp?: number
    storageTemperatureLabel?: string
    requiresMandatoryDiscard?: boolean
    mandatoryDiscardReasons?: string[]
    clinicalChecklistWarnings?: string[]
}

export interface ComponentTypeOption {
    componentTypeId: string
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
    allocationId: string
    bagId: string
    barcode: string
    componentCode?: ComponentCode | string
    componentName: string
    bloodType: string
    volumeMl: number
    expiredAt?: string
    isExpired: boolean
    requestId: string
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
    allocationId: string
    bagId: string
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

