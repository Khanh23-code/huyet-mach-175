export type ScreeningStatus = 'PENDING' | 'QUALIFIED' | 'DEFERRED' | 'UNQUALIFIED'
export type Conclusion = Exclude<ScreeningStatus, 'PENDING'>
import type { TargetVolume } from '@/shared/types/medical'
export type { TargetVolume }
export type AboGroup = 'A' | 'B' | 'AB' | 'O'
export type RhValue = 'POSITIVE' | 'NEGATIVE'

/** SurveyQueueItemDto (ScreeningStatus là chuỗi) */
export interface ScreeningQueueItem {
    appointmentId: string
    donorId: string
    donorCode: string
    donorName: string
    checkedInAt?: string | null
    isWalkIn: boolean
    hasSubmittedSurvey: boolean
    hasRisk: boolean
    screeningStatus: ScreeningStatus
}

/** SurveyQueueFilterDto */
export interface ScreeningQueueQuery {
    date?: string // "YYYY-MM-DD"
    screeningStatus?: ScreeningStatus
    campaignId?: string
    search?: string
    page?: number
    pageSize?: number
}

export interface SurveyDonorSummary {
    donorId: string
    donorCode: string
    fullName: string
    citizenId: string
    dateOfBirth: string
    age: number
    gender: string
    phoneNumber: string
    bloodType?: string | null // "A" | "B" | "AB" | "O"
    rhFactor?: string | null // "Rh+" | "Rh-"
    totalDonations: number
    lastDonationDate?: string | null
    isEligible84Days: boolean
}

export interface SurveyOnlineDetail {
    surveyId?: string | null
    hasSubmittedSurvey: boolean
    surveySubmittedAt?: string | null
    riskScore: number
    hasRisk: boolean
    surveyAnswers?: Record<string, unknown> | null
}

export interface SurveyClinicalDetail {
    weightKg?: number | null
    systolicBloodPressure?: number | null
    diastolicBloodPressure?: number | null
    pulse?: number | null
    hemoglobinLevel?: number | null
    bodyTemp?: number | null
    rapidHbsAgNegative?: boolean | null
    clinicalNotes?: string | null
    screeningStatus: ScreeningStatus
    deferralReason?: string | null
    targetVolumeMl?: TargetVolume | null
    doctorId?: string | null
    doctorName?: string | null
    screenedAt?: string | null
}

/** GET /api/surveys/appointment/{id} */
export interface SurveyDetail {
    appointmentId: string
    appointmentDate: string
    timeSlot: string
    isWalkIn: boolean
    checkedInAt?: string | null
    campaignName?: string | null
    donor: SurveyDonorSummary
    survey: SurveyOnlineDetail
    clinical: SurveyClinicalDetail
}

/** PUT /api/surveys/appointment/{id}/clinical (UpdateClinicalScreeningRequest) */
export interface UpdateClinicalRequest {
    surveyAnswers?: Record<string, boolean>
    weightKg?: number
    systolicBloodPressure?: number
    diastolicBloodPressure?: number
    pulse?: number
    hemoglobinLevel?: number
    bodyTemp?: number
    rapidHbsAgNegative?: boolean
    clinicalNotes?: string
    screeningStatus: Conclusion
    deferralReason?: string
    targetVolumeMl?: TargetVolume
    bloodType?: AboGroup
    rhFactor?: RhValue
}

export interface ClinicalScreeningResponse {
    appointmentId: string
    screeningStatus: ScreeningStatus
    sessionId?: string | null
    sessionStatus?: string | null
    targetVolumeMl?: TargetVolume | null
    screenedAt: string
    message: string
}