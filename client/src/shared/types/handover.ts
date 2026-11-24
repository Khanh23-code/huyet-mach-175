import type { AboGroup, RhValue } from './medical'

export type HandoverComponentCode = 'WB' | 'PRBC' | 'FFP' | 'PLT'
export type HandoverUrgencyLevel = 'ROUTINE' | 'URGENT' | 'EMERGENCY'
export type HandoverRequestStatus = 'DRAFT' | 'PENDING' | 'READY' | 'COMPLETED' | 'REJECTED'
export type HandoverSessionStatus = 'PENDING' | 'CONFIRMED' | 'CANCELLED' | 'EXPIRED'

export interface PendingHandoverSessionFilter {
    sessionCode?: string
    requestCode?: string
    doctorId?: number
    doctorName?: string
}

export interface CreateHandoverSessionRequest {
    barcodes: string[]
    requestId?: number
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
    allocationId: number
    bagId: number
    barcode: string
    componentTypeId: number
    componentCode: HandoverComponentCode | string
    componentName: string
    bloodType: string
    volumeMl: number
    expiredAt: string
    bagStatus: string
    locationName?: string | null
}

export interface HandoverSessionDto {
    handoverSessionId: number
    sessionCode: string
    status: HandoverSessionStatus
    createdAt: string
    expiresAt: string
    remainingSeconds: number
    confirmedAt?: string | null
    cancelledAt?: string | null
    cancelReason?: string | null
    initiatedById: number
    initiatedByName?: string | null
    confirmedById?: number | null
    confirmedByName?: string | null
    cancelledById?: number | null
    cancelledByName?: string | null
    requestId: number
    requestCode: string
    doctorId?: number | null
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
