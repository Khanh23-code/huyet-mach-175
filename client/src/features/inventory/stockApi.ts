import { axiosClient } from '@/services/axiosClient'
import type { PagedResult } from '@/features/reception/types'
import type { StorageLocation } from './types'
import type {
    BagQuery, BagScan, BloodBag, ExpiringQuery, ExpiringSoonBag, InventorySummary, LocationQuery,
    RelocateRequest, RelocateResponse, StatusLogsResponse, StorageLocationInput, TransitionRequest,
    TransitionResponse,
} from './stockTypes'

export const stockApi = {
    summary: (params?: { storageLocationId?: number }) =>
        axiosClient.get<InventorySummary>('/api/inventory/summary', { params }).then((r) => r.data),

    expiringSoon: (params: ExpiringQuery) =>
        axiosClient.get<PagedResult<ExpiringSoonBag>>('/api/inventory/expiring-soon', { params }).then((r) => r.data),

    bags: (params: BagQuery) =>
        axiosClient.get<PagedResult<BloodBag>>('/api/blood-bags', { params }).then((r) => r.data),

    relocate: (bagId: number, body: RelocateRequest) =>
        axiosClient.put<RelocateResponse>(`/api/inventory/bags/${bagId}/storage-location`, body).then((r) => r.data),

    scan: (barcode: string) =>
        axiosClient.get<BagScan>(`/api/blood-bags/scan/${encodeURIComponent(barcode)}`).then((r) => r.data),

    transition: (body: TransitionRequest) =>
        axiosClient.post<TransitionResponse>('/api/blood-bags/transition-status', body).then((r) => r.data),

    recentLogs: (limit: number) =>
        axiosClient.get<StatusLogsResponse>('/api/blood-bags/audit-logs/recent', { params: { limit } }).then((r) => r.data),
}

export const locationsApi = {
    list: (params: LocationQuery) =>
        axiosClient.get<PagedResult<StorageLocation>>('/api/inventory/storage-locations', { params }).then((r) => r.data),

    create: (body: StorageLocationInput) =>
        axiosClient.post<StorageLocation>('/api/inventory/storage-locations', body).then((r) => r.data),

    update: (id: number, body: StorageLocationInput) =>
        axiosClient.put<StorageLocation>(`/api/inventory/storage-locations/${id}`, body).then((r) => r.data),

    remove: (id: number) => axiosClient.delete(`/api/inventory/storage-locations/${id}`).then(() => undefined),
}