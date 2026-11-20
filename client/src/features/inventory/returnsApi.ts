import { axiosClient } from '@/services/axiosClient'
import type { PagedResult } from '@/shared/types/api'
import type {
    PendingBloodReturnDto,
    PendingBloodReturnFilter,
    ProcessBloodReturnRequest,
    ProcessBloodReturnResponse,
    ReturnInspectionDto,
} from './returnsTypes'

export const returnsApi = {
    pending: (params?: PendingBloodReturnFilter) =>
        axiosClient
            .get<PagedResult<PendingBloodReturnDto>>('/api/blood-returns/pending', { params })
            .then((r) => r.data),

    scan: (barcode: string) =>
        axiosClient
            .get<ReturnInspectionDto>(`/api/blood-returns/scan/${encodeURIComponent(barcode.trim())}`)
            .then((r) => r.data),

    process: (returnId: number, body: ProcessBloodReturnRequest) =>
        axiosClient
            .put<ProcessBloodReturnResponse>(`/api/blood-returns/${returnId}/process`, body)
            .then((r) => r.data),
}
