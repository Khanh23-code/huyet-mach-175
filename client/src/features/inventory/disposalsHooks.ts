import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { disposalsApi } from './disposalsApi'
import type { ApproveDisposalRequest, CandidateFilter, CreateDisposalRequest, DisposalFilter } from './disposalsTypes'

export const useDisposals = (filter: DisposalFilter, options?: { refetchInterval?: number | false }) =>
    useQuery({
        queryKey: ['inventory', 'disposals', filter],
        queryFn: () => disposalsApi.list(filter),
        placeholderData: keepPreviousData,
        refetchInterval: options?.refetchInterval ?? false,
    })

export const useDisposalDetail = (id: number | null) =>
    useQuery({
        queryKey: ['inventory', 'disposal', id],
        queryFn: () => disposalsApi.detail(id as number),
        enabled: id !== null,
    })

export const useDisposalCandidates = (filter: CandidateFilter) =>
    useQuery({
        queryKey: ['inventory', 'disposal-candidates', filter],
        queryFn: () => disposalsApi.candidates(filter),
        placeholderData: keepPreviousData,
    })

const useInvalidating = <TVars, TData>(fn: (v: TVars) => Promise<TData>) => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: fn,
        onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory'] }),
    })
}

export const useCreateDisposal = () => useInvalidating((b: CreateDisposalRequest) => disposalsApi.create(b))

export const useCancelDisposal = () =>
    useInvalidating((v: { id: number; reason?: string }) => disposalsApi.cancel(v.id, v.reason))

export const useApproveDisposal = () =>
    useInvalidating((v: { id: number; body: ApproveDisposalRequest }) => disposalsApi.approve(v.id, v.body))

export const useExecuteDisposal = () => useInvalidating((id: number) => disposalsApi.execute(id))