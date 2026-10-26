import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { campaignsApi } from './campaignsApi'
import type {
    ApproveCampaignRequest,
    CampaignFilterQuery,
    CampaignStatus,
    CreateCampaignRequest,
    UpdateCampaignRequest,
} from './types'

export const useCampaigns = (filter?: CampaignFilterQuery) =>
    useQuery({
        queryKey: ['campaigns', 'list', filter],
        queryFn: () => campaignsApi.list(filter),
        placeholderData: keepPreviousData,
    })

export const useMyCampaigns = (filter?: CampaignFilterQuery) =>
    useQuery({
        queryKey: ['campaigns', 'mine', filter],
        queryFn: () => campaignsApi.mine(filter),
        placeholderData: keepPreviousData,
    })

export const useCampaignDetail = (id: number | null) =>
    useQuery({
        queryKey: ['campaigns', 'detail', id],
        queryFn: () => campaignsApi.get(id as number),
        enabled: id !== null && id > 0,
    })

export const usePendingCampaigns = (enabled = true) =>
    useQuery({
        queryKey: ['campaigns', 'pending'],
        queryFn: () => campaignsApi.list({ status: 'PENDING_APPROVAL', pageNumber: 1, pageSize: 50 }),
        enabled,
    })

export const useCreateCampaign = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: (body: CreateCampaignRequest) => campaignsApi.create(body),
        onSuccess: async () => {
            await qc.invalidateQueries({ queryKey: ['campaigns'] })
        },
    })
}

export const useUpdateCampaign = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: ({ id, body }: { id: number; body: UpdateCampaignRequest }) =>
            campaignsApi.update(id, body),
        onSuccess: async (_, variables) => {
            await Promise.all([
                qc.invalidateQueries({ queryKey: ['campaigns'] }),
                qc.invalidateQueries({ queryKey: ['campaigns', 'detail', variables.id] }),
            ])
        },
    })
}

export const useSubmitApproval = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: (id: number) => campaignsApi.submitApproval(id),
        onSuccess: async (_, id) => {
            await Promise.all([
                qc.invalidateQueries({ queryKey: ['campaigns'] }),
                qc.invalidateQueries({ queryKey: ['campaigns', 'detail', id] }),
            ])
        },
    })
}

export const useApproveCampaign = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: ({ id, body }: { id: number; body: ApproveCampaignRequest }) =>
            campaignsApi.approve(id, body),
        onSuccess: async (_, variables) => {
            await Promise.all([
                qc.invalidateQueries({ queryKey: ['campaigns'] }),
                qc.invalidateQueries({ queryKey: ['campaigns', 'detail', variables.id] }),
            ])
        },
    })
}

export const useUpdateCampaignStatus = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: ({ id, status }: { id: number; status: CampaignStatus }) =>
            campaignsApi.updateStatus(id, status),
        onSuccess: async (_, variables) => {
            await Promise.all([
                qc.invalidateQueries({ queryKey: ['campaigns'] }),
                qc.invalidateQueries({ queryKey: ['campaigns', 'detail', variables.id] }),
            ])
        },
    })
}

export const useDeleteCampaign = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: (id: number) => campaignsApi.delete(id),
        onSuccess: async () => {
            await qc.invalidateQueries({ queryKey: ['campaigns'] })
        },
    })
}
