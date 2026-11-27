import type { BagSource, SourceFilterValue } from './bagSource'

export type TestResultValue = 'NEGATIVE' | 'POSITIVE'
export type AboType = 'A' | 'B' | 'AB' | 'O'
export type RhValue = 'POSITIVE' | 'NEGATIVE'

export type IndicatorKey =
    | 'hivResult'
    | 'hbvResult'
    | 'hcvResult'
    | 'syphilisResult'
    | 'irregularAntibody'

export interface LabQueueItem {
    bagId: string
    barcode: string
    donorCode?: string | null
    donorName?: string | null
    componentTypeCode: string
    componentTypeName: string
    volumeMl: number
    expectedBloodType: string
    expectedRhFactor: string
    expectedBloodGroup: string
    storageLocationId?: string | null
    storageLocationName?: string | null
    collectedAt: string
    expiredAt: string
    isExpired: boolean
    status: string
    source: BagSource
}

export interface LabQueueQuery {
    search?: string
    sourceType?: SourceFilterValue
    pageNumber?: number
    pageSize?: number
}

export interface RecordLabTestRequest {
    barcode: string
    hivResult: TestResultValue
    hbvResult: TestResultValue
    hcvResult: TestResultValue
    syphilisResult: TestResultValue
    irregularAntibody: TestResultValue
    confirmedBloodType: AboType
    confirmedRhFactor: RhValue
}

export interface LabTestResult {
    testId: string
    bagId: string
    barcode: string
    hivResult: string
    hbvResult: string
    hcvResult: string
    syphilisResult: string
    irregularAntibody: string
    confirmedBloodType: string
    confirmedRhFactor: string
    confirmedBloodGroup: string
    overallConclusion: 'PASSED' | 'FAILED'
    newBagStatus: string
    discardReason?: string | null
    technicianId: string
    technicianName: string
    testedAt: string
    message: string
}