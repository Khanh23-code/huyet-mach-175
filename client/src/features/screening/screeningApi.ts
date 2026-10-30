import { axiosClient } from '@/services/axiosClient'
import type { PagedResult } from '@/features/reception/types'
import type {
    ClinicalScreeningResponse,
    ScreeningQueueItem,
    ScreeningQueueQuery,
    SurveyDetail,
    UpdateClinicalRequest,
} from './types'

export const screeningApi = {
    /** Hàng chờ sàng lọc: lịch hẹn CHECKED_IN trong ngày, FIFO theo giờ check-in. */
    queue: (params?: ScreeningQueueQuery) =>
        axiosClient
            .get<PagedResult<ScreeningQueueItem>>('/api/surveys/queue', { params })
            .then((r) => r.data),

    detail: (appointmentId: number) =>
        axiosClient
            .get<SurveyDetail>(`/api/surveys/appointment/${appointmentId}`)
            .then((r) => r.data),

    saveClinical: (appointmentId: number, body: UpdateClinicalRequest) =>
        axiosClient
            .put<ClinicalScreeningResponse>(`/api/surveys/appointment/${appointmentId}/clinical`, body)
            .then((r) => r.data),
}