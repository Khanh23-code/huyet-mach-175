import type { BagLabSummary, BagSource, SourceFilterValue } from './bagSource'

export type ChildCode = 'PRBC' | 'FFP' | 'PLT'
export type ComponentCode = 'WB' | ChildCode
export type StorageZone = 'QUARANTINE' | 'RELEASED' | 'DISPOSAL_HOLD'
export type CompatibilityRuleType = 'RBC' | 'PLASMA' | 'PLATELET'

export interface StorageLocation {
    locationId: number
    refrigeratorName: string
    shelfNumber: string
    targetTemperature: number
    zone: StorageZone
    isFull: boolean
    isActive: boolean
    currentBagsCount: number
    expiringSoonBagsCount: number
}

/** Rút gọn từ BloodBagDto, dùng để giải thích vì sao túi không điều chế được. */
export interface BloodBagLite {
    bagId: number
    barcode: string
    bloodGroup: string
    componentTypeCode: string
    status: string
}

export interface RegisterExternalRequest {
    externalUnitName: string
    externalCode: string
    bloodType: 'A' | 'B' | 'AB' | 'O'
    rhFactor: 'POSITIVE' | 'NEGATIVE'
    volumeMl: number
    collectedAt: string // ISO UTC
    receivedTemperature: number
    storageLocationId: number
}

export interface RegisterExternalResponse {
    bagId: number
    barcode: string
    componentTypeName: string
    bloodGroup: string
    volumeMl: number
    status: string
    externalUnitName: string
    externalCode: string
    storageLocationId: number
    storageLocationName: string
    receivedTemperature: number
    collectedAt: string
    expiredAt: string
    receivedAt: string
    receivedBy: number
    message: string
}

export interface ProcessingQueueItem {
    bagId: number
    barcode: string
    donorCode?: string | null
    donorName?: string | null
    componentTypeCode: string
    componentTypeName: string
    volumeMl: number
    bloodType: string
    rhFactor: string
    bloodGroup: string
    storageLocationId?: number | null
    storageLocationName?: string | null
    collectedAt: string
    testedAt?: string | null
    expiredAt: string
    isExpired: boolean
    status: string
    source: BagSource
    labTest?: BagLabSummary | null
}

export interface ProcessingQueueQuery {
    search?: string
    sourceType?: SourceFilterValue
    pageNumber?: number
    pageSize?: number
}

export interface ComponentTypeOption {
    componentTypeId: number
    typeCode: string
    typeName: string
    lifeDays: number
    storageTemperature: string
}

export interface SeparateRequest {
    parentBarcode: string
    components: { componentType: ChildCode; volumeMl: number; storageLocationId: number }[]
}

export interface DerivedComponent {
    bagId: number
    barcode: string
    componentTypeCode: string
    componentTypeName: string
    bloodGroup: string
    volumeMl: number
    status: string
    storageLocationId: number
    storageLocationName: string
    collectedAt: string
    expiredAt: string
}

export interface SeparateResponse {
    parentBagId: number
    parentBarcode: string
    parentStatus: string
    processedAt: string
    technicianId: number
    technicianName: string
    derivedComponents: DerivedComponent[]
    message: string
}