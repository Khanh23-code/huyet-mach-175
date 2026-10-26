import { axiosClient } from '@/services/axiosClient'
import type {
    ApproveCampaignRequest,
    CampaignDetail,
    CampaignFilterQuery,
    CampaignListItem,
    CampaignStatus,
    CampaignType,
    CreateCampaignRequest,
    PagedResult,
    UpdateCampaignRequest,
} from './types'

const toCampaignTypeNumber = (type: CampaignType | number): number => {
    if (typeof type === 'number') return type
    return type === 'RECURRING' ? 1 : 0
}

const toCampaignStatusNumber = (status: CampaignStatus | number): number => {
    if (typeof status === 'number') return status
    const map: Record<CampaignStatus, number> = {
        DRAFT: 0,
        PENDING_APPROVAL: 1,
        APPROVED: 2,
        REJECTED: 3,
        IN_PROGRESS: 4,
        COMPLETED: 5,
        CANCELLED: 6,
    }
    return map[status] ?? 0
}

export const campaignsApi = {
    list: (params?: CampaignFilterQuery) =>
        axiosClient.get<PagedResult<CampaignListItem>>('/api/campaigns', { params }).then((r) => r.data),

    mine: (params?: CampaignFilterQuery) =>
        axiosClient.get<PagedResult<CampaignListItem>>('/api/campaigns/mine', { params }).then((r) => r.data),

    get: (id: number) =>
        axiosClient.get<CampaignDetail>(`/api/campaigns/${id}`).then((r) => r.data),

    create: (body: CreateCampaignRequest) => {
        const payload = {
            ...body,
            campaignType: toCampaignTypeNumber(body.campaignType),
        }
        return axiosClient.post<CampaignDetail>('/api/campaigns', payload).then((r) => r.data)
    },

    update: (id: number, body: UpdateCampaignRequest) => {
        const payload = {
            ...body,
            campaignType: toCampaignTypeNumber(body.campaignType),
        }
        return axiosClient.put<CampaignDetail>(`/api/campaigns/${id}`, payload).then((r) => r.data)
    },

    submitApproval: (id: number) =>
        axiosClient.patch<CampaignDetail>(`/api/campaigns/${id}/submit-approval`).then((r) => r.data),

    approve: (id: number, body: ApproveCampaignRequest) =>
        axiosClient.patch<CampaignDetail>(`/api/campaigns/${id}/approval`, body).then((r) => r.data),

    updateStatus: (id: number, status: CampaignStatus) =>
        axiosClient
            .patch<CampaignDetail>(`/api/campaigns/${id}/status`, { status: toCampaignStatusNumber(status) })
            .then((r) => r.data),

    delete: (id: number) =>
        axiosClient.delete<void>(`/api/campaigns/${id}`).then((r) => r.data),
}
