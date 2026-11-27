import type { TargetVolume } from '@/shared/types/medical'

export type { TargetVolume }
export type SessionStatus = 'WAITING_COLLECTION' | 'COMPLETED' | 'ABORTED'

/** SessionQueueItemDto */
export interface SessionQueueItem {
    sessionId: string
    appointmentId: string
    donorCode: string
    donorName: string
    bloodType?: string | null
    rhFactor?: string | null
    targetVolumeMl: TargetVolume
    status: SessionStatus
    screenedAt?: string | null
    doctorName?: string | null
}

export interface SessionQueueQuery {
    date?: string
    status?: SessionStatus
    search?: string
    pageNumber?: number
    pageSize?: number
}

export interface SessionBloodBag {
    bagId: string
    barcode: string
    bloodGroup: string
    volumeMl: number
    collectedAt: string
    status: string
}

export interface SessionDonor {
    donorId: string
    donorCode: string
    fullName: string
    citizenId: string
    gender: string
    bloodType?: string | null
    rhFactor?: string | null
}

export interface SessionScreeningSummary {
    weightKg?: number | null
    bloodPressure?: string | null
    pulse?: number | null
    hemoglobinLevel?: number | null
    clinicalNotes?: string | null
    doctorName?: string | null
    screenedAt?: string | null
}

export interface SessionDetail {
    sessionId: string
    appointmentId: string
    status: SessionStatus
    targetVolumeMl: TargetVolume
    actualVolumeMl?: number | null
    collectionIncident?: string | null
    phlebotomistId?: string | null
    phlebotomistName?: string | null
    completedAt?: string | null
    bloodBagBarcode?: string | null
    bloodBag?: SessionBloodBag | null
    donor: SessionDonor
    screeningSummary: SessionScreeningSummary
}

export interface StartCollectionResponse {
    sessionId: string
    bagId: string
    barcode: string
    bloodGroup: string
    volumeMl: number
    collectedAt: string
    status: string
    isNewBag: boolean
    message: string
}

export interface UpdateCollectionRequest {
    status: Exclude<SessionStatus, 'WAITING_COLLECTION'>
    actualVolumeMl?: number
    collectionIncident?: string
}

export interface CollectionResponse {
    sessionId: string
    status: SessionStatus
    actualVolumeMl?: number | null
    completedAt: string
    bloodBagId?: string | null
    message: string
}