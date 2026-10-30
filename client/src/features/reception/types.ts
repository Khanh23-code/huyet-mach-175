/** AppointmentStatus ở BE KHÔNG có JsonStringEnumConverter -> mặc định về số (0..3). */
export type AppointmentStatus = 'BOOKED' | 'CHECKED_IN' | 'CANCELLED' | 'NO_SHOW'

/** AppointmentListItemDto (chỉ lấy field FE đang dùng) */
export interface AppointmentListItem {
    appointmentId: number
    donorId: number
    donorName: string
    donorCitizenId: string
    donorPhoneNumber: string
    campaignId?: number | null
    campaignName?: string | null
    slotId?: number | null
    appointmentDate: string // "YYYY-MM-DD"
    timeSlot: string // "HH:mm-HH:mm", rỗng nếu không có slot
    qrCodeToken: string
    status: AppointmentStatus | number
    isWalkIn: boolean
    checkedInAt?: string | null
    hasPreScreeningSurvey: boolean
}

/** AppointmentFilterDto */
export interface AppointmentFilterQuery {
    date?: string // "YYYY-MM-DD"
    campaignId?: number
    slotId?: number
    status?: number // AppointmentStatus dạng số
    isWalkIn?: boolean
    search?: string
    page?: number
    pageSize?: number // 1..100, mặc định BE là 20
}

export interface PagedResult<T> {
    items: T[]
    totalCount: number
    pageNumber: number
    pageSize: number
    totalPages?: number
}

/** CreateWalkInAppointmentRequest: không truyền slotId thì BE tự chọn khung giờ của đợt */
export interface CreateWalkInRequest {
    donorId: number
    campaignId?: number
    slotId?: number
}

/** CheckInAppointmentRequest */
export interface CheckInRequest {
    qrCodeToken: string
    campaignId?: number
}

/** AppointmentDetailDto (chỉ lấy field FE đang dùng) */
export interface AppointmentDetail {
    appointmentId: number
    donorId: number
    donorName: string
    donorCitizenId: string
    campaignId?: number | null
    campaignName?: string | null
    timeSlot: string
    status: AppointmentStatus | number
    checkedInAt?: string | null
}