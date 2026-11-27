import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { sessionsApi } from './sessionsApi'
import type { SessionQueueQuery, UpdateCollectionRequest } from './types'

interface QueueOptions {
    enabled?: boolean
    refetchInterval?: number | false
}

export const useSessionQueue = (filter: SessionQueueQuery, options?: QueueOptions) =>
    useQuery({
        queryKey: ['sessions', 'queue', filter],
        queryFn: () => sessionsApi.queue(filter),
        placeholderData: keepPreviousData,
        enabled: options?.enabled ?? true,
        refetchInterval: options?.refetchInterval ?? false,
    })

export const useSessionDetail = (sessionId: string | null) =>
    useQuery({
        queryKey: ['sessions', 'detail', sessionId],
        queryFn: () => sessionsApi.detail(sessionId as string),
        enabled: Boolean(sessionId),
    })

export const useStartCollection = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: (sessionId: string) => sessionsApi.start(sessionId),
        onSuccess: () => qc.invalidateQueries({ queryKey: ['sessions'] }),
    })
}

export const useCompleteCollection = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: ({ sessionId, body }: { sessionId: string; body: UpdateCollectionRequest }) =>
            sessionsApi.complete(sessionId, body),
        onSuccess: () => qc.invalidateQueries({ queryKey: ['sessions'] }),
    })
}