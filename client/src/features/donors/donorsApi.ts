import { axiosClient } from '@/services/axiosClient'
import type { CreateDonorRequest, DonorDetail, DonorEligibility } from './types'

export const donorsApi = {
    get: (id: number) =>
        axiosClient.get<DonorDetail>(`/api/donors/${id}`).then((r) => r.data),

    /** Tra CCCD: chưa có -> donorId = null (BE không trả 404). */
    checkEligibility: (citizenId: string) =>
        axiosClient
            .get<DonorEligibility>('/api/donors/check-eligibility', { params: { citizenId } })
            .then((r) => r.data),

    create: (body: CreateDonorRequest) =>
        axiosClient.post<DonorDetail>('/api/donors', body).then((r) => r.data),
}