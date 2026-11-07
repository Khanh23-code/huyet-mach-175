import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { inventoryApi } from './inventoryApi'
import type { ProcessingQueueQuery, RegisterExternalRequest, SeparateRequest } from './types'

export const useStorageLocations = () =>
    useQuery({
        queryKey: ['inventory', 'locations'],
        queryFn: inventoryApi.storageLocations,
        staleTime: 60_000,
    })

export const useComponentTypes = () =>
    useQuery({
        queryKey: ['inventory', 'component-types'],
        queryFn: inventoryApi.componentTypes,
        staleTime: 5 * 60_000,
    })

export const useProcessingQueue = (
    filter: ProcessingQueueQuery,
    options?: { refetchInterval?: number | false },
) =>
    useQuery({
        queryKey: ['inventory', 'processing-queue', filter],
        queryFn: () => inventoryApi.processingQueue(filter),
        placeholderData: keepPreviousData,
        refetchInterval: options?.refetchInterval ?? false,
    })

export const useRegisterExternal = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: (body: RegisterExternalRequest) => inventoryApi.registerExternal(body),
        onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory'] }),
    })
}

export const useSeparate = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: (body: SeparateRequest) => inventoryApi.separate(body),
        onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory'] }),
    })
}