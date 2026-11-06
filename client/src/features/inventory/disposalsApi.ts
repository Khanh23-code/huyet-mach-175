import { axiosClient } from '@/services/axiosClient'
import type { PagedResult } from '@/features/reception/types'
import type {
    ApproveDisposalRequest,
    CandidateFilter,
    CreateDisposalRequest,
    CreateDisposalResponse,
    DisposalCandidate,
    DisposalDetail,
    DisposalFilter,
    DisposalRecord,
} from './disposalsTypes'

const base = '/api/blood-disposals'

export const disposalsApi = {
    list: (params?: DisposalFilter) =>
        axiosClient.get<PagedResult<DisposalRecord>>(base, { params }).then((r) => r.data),

    detail: (id: number) => axiosClient.get<DisposalDetail>(`${base}/${id}`).then((r) => r.data),

    candidates: (params?: CandidateFilter) =>
        axiosClient.get<PagedResult<DisposalCandidate>>(`${base}/candidates`, { params }).then((r) => r.data),

    create: (body: CreateDisposalRequest) =>
        axiosClient.post<CreateDisposalResponse>(base, body).then((r) => r.data),

    cancel: (id: number, reason?: string) =>
        axiosClient.patch<DisposalDetail>(`${base}/${id}/cancel`, { reason }).then((r) => r.data),

    approve: (id: number, body: ApproveDisposalRequest) =>
        axiosClient.patch<DisposalDetail>(`${base}/${id}/approval`, body).then((r) => r.data),

    execute: (id: number) => axiosClient.patch<DisposalDetail>(`${base}/${id}/execute`).then((r) => r.data),
}