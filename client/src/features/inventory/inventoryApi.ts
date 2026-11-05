import { axiosClient } from '@/services/axiosClient'
import type { PagedResult } from '@/features/reception/types'
import type {
    BloodBagLite,
    ComponentTypeOption,
    ProcessingQueueItem,
    ProcessingQueueQuery,
    RegisterExternalRequest,
    RegisterExternalResponse,
    SeparateRequest,
    SeparateResponse,
    StorageLocation,
} from './types'

export const inventoryApi = {
    storageLocations: () =>
        axiosClient
            .get<PagedResult<StorageLocation>>('/api/inventory/storage-locations', {
                params: { availableOnly: true, pageNumber: 1, pageSize: 100 },
            })
            .then((r) => r.data),

    searchBags: (params: { search: string; pageNumber?: number; pageSize?: number }) =>
        axiosClient.get<PagedResult<BloodBagLite>>('/api/blood-bags', { params }).then((r) => r.data),

    registerExternal: (body: RegisterExternalRequest) =>
        axiosClient.post<RegisterExternalResponse>('/api/blood-bags/external', body).then((r) => r.data),

    processingQueue: (params?: ProcessingQueueQuery) =>
        axiosClient
            .get<PagedResult<ProcessingQueueItem>>('/api/blood-processing/queue', { params })
            .then((r) => r.data),

    componentTypes: () =>
        axiosClient.get<ComponentTypeOption[]>('/api/blood-processing/component-types').then((r) => r.data),

    separate: (body: SeparateRequest) =>
        axiosClient.post<SeparateResponse>('/api/blood-processing/separate', body).then((r) => r.data),
}