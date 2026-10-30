import { axiosClient } from '@/services/axiosClient'
import type {
    AppointmentDetail,
    AppointmentFilterQuery,
    AppointmentListItem,
    CheckInRequest,
    CreateWalkInRequest,
    PagedResult,
} from './types'

export const appointmentsApi = {
    list: (params?: AppointmentFilterQuery) =>
        axiosClient
            .get<PagedResult<AppointmentListItem>>('/api/appointments', { params })
            .then((r) => r.data),

    /** Tiếp nhận vãng lai: BE tự set isWalkIn = true, status = CHECKED_IN. */
    createWalkIn: (body: CreateWalkInRequest) =>
        axiosClient
            .post<{ appointmentId: number }>('/api/appointments/walk-in', body)
            .then((r) => r.data),

    /** Quét mã QR: BOOKED -> CHECKED_IN. Lỗi 400/404 có message tiếng Việt từ BE. */
    checkIn: (body: CheckInRequest) =>
        axiosClient
            .post<AppointmentDetail>('/api/appointments/check-in', body)
            .then((r) => r.data),
}