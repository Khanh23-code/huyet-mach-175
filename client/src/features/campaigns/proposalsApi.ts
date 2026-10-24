import { axiosClient } from '@/services/axiosClient'
import type { CreateProposalRequest, Proposal, ProposalPage, ProposalQuery } from './types'

interface RawPage { items: Proposal[]; totalCount: number }

const toPage = (d: RawPage): ProposalPage => ({ items: d.items ?? [], total: d.totalCount ?? 0 })

export const proposalsApi = {
    create: (body: CreateProposalRequest) =>
        axiosClient.post<Proposal>('/api/proposals', body).then((r) => r.data),

    /** BBNK: đề xuất của chính mình */
    mine: (q: ProposalQuery) =>
        axiosClient.get<RawPage>('/api/proposals/mine', { params: q }).then((r) => toPage(r.data)),

    /** MGT: toàn bộ đề xuất */
    list: (q: ProposalQuery) =>
        axiosClient.get<RawPage>('/api/proposals', { params: q }).then((r) => toPage(r.data)),

    get: (id: number) => axiosClient.get<Proposal>(`/api/proposals/${id}`).then((r) => r.data),

    approve: (id: number) =>
        axiosClient.patch<Proposal>(`/api/proposals/${id}/approve`).then((r) => r.data),

    reject: (id: number, reason: string) =>
        axiosClient.patch<Proposal>(`/api/proposals/${id}/reject`, { reason }).then((r) => r.data),
}
