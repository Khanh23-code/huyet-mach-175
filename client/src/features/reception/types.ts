/** AppointmentStatus ở BE KHÔNG có JsonStringEnumConverter -> mặc định về số (0..3). */
export type AppointmentStatus = 'BOOKED' | 'CHECKED_IN' | 'CANCELLED' | 'NO_SHOW'

/** AppointmentListItemDto (chỉ lấy field FE đang dùng) */
export interface AppointmentListItem {
    appointmentId: number
    donorId: number
    donorName: string
    donorCitizenId: string
    donorPhoneNumber: string
    appointmentDate: string // "YYYY-MM-DD"
    timeSlot: string
    status: AppointmentStatus | number
    isWalkIn: boolean
    checkedInAt?: string | null
}

/** AppointmentFilterDto */
export interface AppointmentFilterQuery {
    date?: string // "YYYY-MM-DD"
    campaignId?: number
    status?: number // AppointmentStatus dạng số
    timeSlot?: string
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

/** CreateWalkInAppointmentRequest */
export interface CreateWalkInRequest {
    donorId: number
    campaignId?: number
    timeSlot?: string
}