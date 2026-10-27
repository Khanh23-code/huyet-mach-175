/** Enum từ BE có thể về dạng số hoặc chuỗi tùy cấu hình JSON -> nhận cả hai. */
export type EnumValue = string | number

// Hai enum này có [JsonConverter(JsonStringEnumConverter)] -> luôn là chuỗi
export type SessionStatus = 'WAITING_COLLECTION' | 'COMPLETED' | 'ABORTED'
export type ScreeningStatus = 'PENDING' | 'QUALIFIED' | 'DEFERRED' | 'UNQUALIFIED'

/** DonorHistoryItemDto */
export interface DonorHistoryItem {
    appointmentId: number
    appointmentDate: string // "YYYY-MM-DD"
    timeSlot: string
    campaignId?: number | null
    campaignName?: string | null // null = hiến tại viện
    status: EnumValue // AppointmentStatus (BE trả số, xem reception/format.ts)
    isWalkIn: boolean
    actualVolumeMl?: number | null
    sessionStatus?: SessionStatus | null
    screeningStatus?: ScreeningStatus | null
    deferralReason?: string | null
    completedAt?: string | null
}

/** GET /api/donors/{id} và response của POST /api/donors (DonorDetailDto) */
export interface DonorDetail {
    donorId: number
    citizenId: string
    fullName: string
    dateOfBirth: string // "YYYY-MM-DD"
    gender: string
    phoneNumber: string
    email?: string | null
    bloodType?: EnumValue | null
    rhFactor?: EnumValue | null
    totalDonations: number
    lastDonationDate?: string | null
    donationHistory: DonorHistoryItem[]
}

/** GET /api/donors/check-eligibility?citizenId= (DonorEligibilityDto) */
export interface DonorEligibility {
    citizenId: string
    donorId?: number | null // null = CCCD chưa có trong hệ thống
    fullName?: string | null
    dateOfBirth?: string | null
    age?: number | null
    gender?: string | null
    phoneNumber?: string | null
    bloodType?: EnumValue | null
    rhFactor?: EnumValue | null
    totalDonations: number
    hasDonatedBefore: boolean
    lastDonationDate?: string | null
    nextEligibleDate?: string | null
    daysRemaining: number
    isEligible: boolean
    isAgeQualified: boolean
    message: string
}

/** POST /api/donors (CreateDonorRequest) */
export interface CreateDonorRequest {
    citizenId: string
    fullName: string
    dateOfBirth: string // "YYYY-MM-DD"
    gender: string
    phoneNumber: string
    email?: string
}