import type { AboGroup, RhValue } from './medical'

export type HandoverComponentCode = 'WB' | 'PRBC' | 'FFP' | 'PLT'
export type HandoverUrgencyLevel = 'ROUTINE' | 'URGENT' | 'EMERGENCY'
export type HandoverRequestStatus = 'DRAFT' | 'PENDING' | 'READY' | 'COMPLETED' | 'REJECTED'
export type HandoverSessionStatus = 'PENDING' | 'CONFIRMED' | 'CANCELLED' | 'EXPIRED'

export interface PendingHandoverSessionFilter {
    sessionCode?: string
    requestCode?: string
    doctorId?: string
    doctorName?: string
}

export interface CreateHandoverSessionRequest {
    barcodes: string[]
    requestId?: string
    timeoutMinutes?: number
}

export interface ConfirmHandoverSessionRequest {
    scannedBarcodes: string[]
    expectedPatientCode?: string
}

export interface CancelHandoverSessionRequest {
    cancelReason?: string
}

export interface HandoverSessionItemDto {
    allocationId: string
    bagId: string
    barcode: string
    componentTypeId: string
    componentCode: HandoverComponentCode | string
    componentName: string
    bloodType: string
    volumeMl: number
    expiredAt: string
    bagStatus: string
    locationName?: string | null
}

export interface HandoverSessionDto {
    handoverSessionId: string
    sessionCode: string
    status: HandoverSessionStatus
    createdAt: string
    expiresAt: string
    remainingSeconds: number
    confirmedAt?: string | null
    cancelledAt?: string | null
    cancelReason?: string | null
    initiatedById: string
    initiatedByName?: string | null
    confirmedById?: string | null
    confirmedByName?: string | null
    cancelledById?: string | null
    cancelledByName?: string | null
    requestId: string
    requestCode: string
    doctorId?: string | null
    doctorName?: string | null
    departmentName?: string | null
    isOwnRequest: boolean
    urgencyLevel: HandoverUrgencyLevel
    patientCode: string
    patientName: string
    patientBloodType: string
    requestStatus: HandoverRequestStatus
    currentStep: number
    items: HandoverSessionItemDto[]
}

export type HandoverBloodGroup = {
    bloodType: AboGroup
    rhFactor: RhValue
}

export interface ReservedHandoverFilter {
    search?: string
    urgencyLevel?: string
}

export interface ReservedHandoverBagDto {
    allocationId: string
    bagId: string
    barcode: string
    componentTypeId: string
    componentCode: HandoverComponentCode | string
    componentName: string
    bloodType: string
    volumeMl: number
    expiredAt: string
    isExpired: boolean
    locationName?: string | null
    reservedAt: string
    reservedByName?: string | null
    activeSessionId?: string | null
    activeSessionCode?: string | null
}

export interface ReservedHandoverRequestDto {
    requestId: string
    requestCode: string
    requestStatus: HandoverRequestStatus
    urgencyLevel: HandoverUrgencyLevel
    patientCode: string
    patientName: string
    patientBloodType: string
    departmentName?: string | null
    doctorName?: string | null
    createdAt: string
    totalReservedBags: number
    readyForSessionCount: number
    inPendingSessionCount: number
    bags: ReservedHandoverBagDto[]
}
