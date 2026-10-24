import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { proposalsApi } from './proposalsApi'
import { sitesApi } from './sitesApi'
import type { ProposalQuery } from './types'

export const useSites = () =>
    useQuery({ queryKey: ['sites', 'list'], queryFn: () => sitesApi.list(true) })

export const useMyProposals = (q: ProposalQuery) =>
    useQuery({
        queryKey: ['proposals', 'mine', q],
        queryFn: () => proposalsApi.mine(q),
        placeholderData: keepPreviousData,
    })

export const usePendingProposals = (enabled = true) =>
    useQuery({
        queryKey: ['proposals', 'pending'],
        queryFn: () => proposalsApi.list({ status: 'PENDING', pageNumber: 1, pageSize: 50 }),
        enabled,
    })

export const useProposal = (id: number | null) =>
    useQuery({
        queryKey: ['proposals', 'detail', id],
        queryFn: () => proposalsApi.get(id as number),
        enabled: id !== null,
    })
