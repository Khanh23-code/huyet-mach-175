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
    bagId: number
    barcode: string
    donorCode?: string | null
    donorName?: string | null
    componentTypeCode: string
    componentTypeName: string
    volumeMl: number
    expectedBloodType: string
    expectedRhFactor: string
    expectedBloodGroup: string
    storageLocationId?: number | null
    storageLocationName?: string | null
    collectedAt: string
    status: string
}

export interface LabQueueQuery {
    search?: string
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
    testId: number
    bagId: number
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
    technicianId: number
    technicianName: string
    testedAt: string
    message: string
}