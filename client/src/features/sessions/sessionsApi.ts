import { axiosClient } from '@/services/axiosClient'
import type { PagedResult } from '@/features/reception/types'
import type {
    CollectionResponse,
    SessionDetail,
    SessionQueueItem,
    SessionQueueQuery,
    StartCollectionResponse,
    UpdateCollectionRequest,
} from './types'

export const sessionsApi = {
    queue: (params?: SessionQueueQuery) =>
        axiosClient
            .get<PagedResult<SessionQueueItem>>('/api/donation-sessions', { params })
            .then((r) => r.data),

    detail: (sessionId: number) =>
        axiosClient.get<SessionDetail>(`/api/donation-sessions/${sessionId}`).then((r) => r.data),

    start: (sessionId: number) =>
        axiosClient
            .post<StartCollectionResponse>(`/api/donation-sessions/${sessionId}/start`)
            .then((r) => r.data),

    complete: (sessionId: number, body: UpdateCollectionRequest) =>
        axiosClient
            .put<CollectionResponse>(`/api/donation-sessions/${sessionId}/collection`, body)
            .then((r) => r.data),
}