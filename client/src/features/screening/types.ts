export type ScreeningStatus = 'PENDING' | 'QUALIFIED' | 'DEFERRED' | 'UNQUALIFIED'
export type Conclusion = Exclude<ScreeningStatus, 'PENDING'>
export type TargetVolume = 'VOL_250' | 'VOL_350' | 'VOL_450'

/** SurveyQueueItemDto (ScreeningStatus là chuỗi) */
export interface ScreeningQueueItem {
    appointmentId: number
    donorId: number
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
    campaignId?: number
    search?: string
    page?: number
    pageSize?: number
}

export interface SurveyDonorSummary {
    donorId: number
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
    surveyId?: number | null
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
    doctorId?: number | null
    doctorName?: string | null
    screenedAt?: string | null
}

/** GET /api/surveys/appointment/{id} */
export interface SurveyDetail {
    appointmentId: number
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
}

export interface ClinicalScreeningResponse {
    appointmentId: number
    screeningStatus: ScreeningStatus
    sessionId?: number | null
    sessionStatus?: string | null
    targetVolumeMl?: TargetVolume | null
    screenedAt: string
    message: string
}