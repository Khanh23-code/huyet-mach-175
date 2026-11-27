/** AppointmentStatus ở BE KHÔNG có JsonStringEnumConverter -> mặc định về số (0..3). */
export type AppointmentStatus = 'BOOKED' | 'CHECKED_IN' | 'CANCELLED' | 'NO_SHOW'

/** AppointmentListItemDto (chỉ lấy field FE đang dùng) */
export interface AppointmentListItem {
    appointmentId: string
    donorId: string
    donorName: string
    donorCitizenId: string
    donorPhoneNumber: string
    campaignId?: string | null
    campaignName?: string | null
    slotId?: string | null
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
    campaignId?: string
    slotId?: string
    status?: number // AppointmentStatus dạng số
    isWalkIn?: boolean
    search?: string
    page?: number
    pageSize?: number // 1..100, mặc định BE là 20
}

export type { PagedResult } from '@/shared/types/api'

/** CreateWalkInAppointmentRequest: không truyền slotId thì BE tự chọn khung giờ của đợt */
export interface CreateWalkInRequest {
    donorId: string
    campaignId?: string
    slotId?: string
}

/** CheckInAppointmentRequest */
export interface CheckInRequest {
    qrCodeToken: string
    campaignId?: string
}

/** AppointmentDetailDto (chỉ lấy field FE đang dùng) */
export interface AppointmentDetail {
    appointmentId: string
    donorId: string
    donorName: string
    donorCitizenId: string
    campaignId?: string | null
    campaignName?: string | null
    timeSlot: string
    status: AppointmentStatus | number
    checkedInAt?: string | null
}