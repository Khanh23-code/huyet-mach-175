import { axiosClient } from '@/services/axiosClient'
import type { PagedResult } from '@/features/reception/types'
import type { LabQueueItem, LabQueueQuery, LabTestResult, RecordLabTestRequest } from './labTestsTypes'

export const labTestsApi = {
    queue: (params?: LabQueueQuery) =>
        axiosClient.get<PagedResult<LabQueueItem>>('/api/lab-tests/queue', { params }).then((r) => r.data),

    record: (body: RecordLabTestRequest) =>
        axiosClient.post<LabTestResult>('/api/lab-tests', body).then((r) => r.data),

    byBag: (identifier: string) =>
        axiosClient
            .get<LabTestResult>(`/api/lab-tests/bag/${encodeURIComponent(identifier)}`)
            .then((r) => r.data),
}