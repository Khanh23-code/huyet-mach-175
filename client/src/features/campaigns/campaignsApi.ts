import { axiosClient } from '@/services/axiosClient'
import type {
    ApproveCampaignRequest,
    CampaignDetail,
    CampaignFilterQuery,
    CampaignListItem,
    CampaignStatus,
    CreateCampaignRequest,
    PagedResult,
    UpdateCampaignRequest,
} from './types'

export const campaignsApi = {
    list: (params?: CampaignFilterQuery) =>
        axiosClient.get<PagedResult<CampaignListItem>>('/api/campaigns', { params }).then((r) => r.data),

    mine: (params?: CampaignFilterQuery) =>
        axiosClient.get<PagedResult<CampaignListItem>>('/api/campaigns/mine', { params }).then((r) => r.data),

    get: (id: number) =>
        axiosClient.get<CampaignDetail>(`/api/campaigns/${id}`).then((r) => r.data),

    create: (body: CreateCampaignRequest) =>
        axiosClient.post<CampaignDetail>('/api/campaigns', body).then((r) => r.data),

    update: (id: number, body: UpdateCampaignRequest) =>
        axiosClient.put<CampaignDetail>(`/api/campaigns/${id}`, body).then((r) => r.data),

    submitApproval: (id: number) =>
        axiosClient.patch<CampaignDetail>(`/api/campaigns/${id}/submit-approval`).then((r) => r.data),

    approve: (id: number, body: ApproveCampaignRequest) =>
        axiosClient.patch<CampaignDetail>(`/api/campaigns/${id}/approval`, body).then((r) => r.data),

    updateStatus: (id: number, status: CampaignStatus) =>
        axiosClient.patch<CampaignDetail>(`/api/campaigns/${id}/status`, { status }).then((r) => r.data),

    delete: (id: number) =>
        axiosClient.delete<void>(`/api/campaigns/${id}`).then((r) => r.data),
}

