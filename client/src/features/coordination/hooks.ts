import { useEffect, useMemo, useState } from 'react'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { PERMISSIONS } from '@/config/permissions'
import { usePermission } from '@/features/auth/usePermission'
import { coordinationApi, FALLBACK_COMPONENT_TYPES } from './coordinationApi'
import type {
    AllocateBloodBagsRequest,
    BloodCoordinationQueueFilter,
    DispatchBloodBagRequest,
    DispatchBloodBagResponse,
    RejectBloodRequestDto,
} from './types'

const REFRESH_INTERVAL_MS = 5 * 60 * 1000 // 5 phút

const SESSION_CACHE_OPTIONS = {
    staleTime: Infinity,
    gcTime: Infinity,
    refetchOnMount: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
} as const

export const COORDINATION_QUERY_KEYS = {
    componentTypes: (canFetchFromProcessing: boolean) =>
        ['coordination', 'componentTypes', canFetchFromProcessing] as const,
    supplyDemand: (componentTypeId: number) =>
        ['coordination', 'supplyDemand', componentTypeId] as const,
    queue: (filter: BloodCoordinationQueueFilter) =>
        ['coordination', 'queue', filter] as const,
    detail: (requestId: number) =>
        ['coordination', 'detail', requestId] as const,
    availableBags: (
        requestId: number,
        requestItemId: number,
        includeCompatible: boolean,
    ) => ['coordination', 'availableBags', requestId, requestItemId, includeCompatible] as const,
}

function extractErrorMessage(err: unknown, fallback: string): string | null {
    if (!err) return null
    const apiMsg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
    if (apiMsg) return apiMsg
    if (err instanceof Error && err.message) return err.message
    return fallback
}

export function useComponentTypes() {
    const { can } = usePermission()
    const canFetchFromProcessing = can(PERMISSIONS.PROCESSING_VIEW)
    const [selectedTypeId, setSelectedTypeId] = useState<number>(2) // Mặc định PRBC (ID 2)

    const query = useQuery({
        queryKey: COORDINATION_QUERY_KEYS.componentTypes(canFetchFromProcessing),
        queryFn: () =>
            canFetchFromProcessing
                ? coordinationApi.getComponentTypes()
                : Promise.resolve(FALLBACK_COMPONENT_TYPES),
        ...SESSION_CACHE_OPTIONS,
    })

    const types = query.data && query.data.length > 0 ? query.data : FALLBACK_COMPONENT_TYPES

    useEffect(() => {
        if (!types.some((t) => t.componentTypeId === selectedTypeId)) {
            const prbc = types.find((t) => t.typeCode === 'PRBC')
            setSelectedTypeId(prbc ? prbc.componentTypeId : types[0].componentTypeId)
        }
    }, [types, selectedTypeId])

    return {
        types,
        selectedTypeId,
        setSelectedTypeId,
        loading: query.isLoading,
        error: query.isError ? extractErrorMessage(query.error, 'Không thể tải danh sách chế phẩm máu') : null,
    }
}

export function useSupplyDemand(componentTypeId: number | null) {
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)
    const validId = typeof componentTypeId === 'number' && componentTypeId > 0 ? componentTypeId : 0

    const query = useQuery({
        queryKey: COORDINATION_QUERY_KEYS.supplyDemand(validId),
        queryFn: () => coordinationApi.getSupplyDemand(validId),
        enabled: canView && validId > 0,
        refetchInterval: REFRESH_INTERVAL_MS,
        ...SESSION_CACHE_OPTIONS,
    })

    const lastUpdated = useMemo(
        () => (query.dataUpdatedAt ? new Date(query.dataUpdatedAt) : null),
        [query.dataUpdatedAt],
    )

    return {
        data: query.data ?? null,
        loading: query.isLoading,
        isFetching: query.isFetching,
        error: query.isError ? extractErrorMessage(query.error, 'Không thể tải bảng Cung - Cầu') : null,
        lastUpdated,
        refetch: query.refetch,
    }
}

export function useCoordinationQueue(initialFilter?: BloodCoordinationQueueFilter) {
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)

    const [filter, setFilter] = useState<BloodCoordinationQueueFilter>({
        pageNumber: 1,
        pageSize: 10,
        search: '',
        urgencyLevel: undefined,
        ...initialFilter,
    })

    const normalizedFilter = useMemo<BloodCoordinationQueueFilter>(
        () => ({
            ...filter,
            search: filter.search?.trim() || undefined,
            urgencyLevel: filter.urgencyLevel || undefined,
        }),
        [filter],
    )

    const query = useQuery({
        queryKey: COORDINATION_QUERY_KEYS.queue(normalizedFilter),
        queryFn: () => coordinationApi.getQueue(normalizedFilter),
        placeholderData: keepPreviousData,
        enabled: canView,
        ...SESSION_CACHE_OPTIONS,
    })

    return {
        data: query.data ?? null,
        loading: query.isLoading,
        isFetching: query.isFetching,
        error: query.isError ? extractErrorMessage(query.error, 'Không thể tải danh sách hàng đợi yêu cầu máu') : null,
        filter,
        setFilter,
        refetch: query.refetch,
    }
}

export function useRequestDetail(requestId: number | null) {
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)
    const validId = typeof requestId === 'number' && requestId > 0 ? requestId : 0

    const query = useQuery({
        queryKey: COORDINATION_QUERY_KEYS.detail(validId),
        queryFn: () => coordinationApi.getRequestDetail(validId),
        enabled: canView && validId > 0,
        ...SESSION_CACHE_OPTIONS,
    })

    return {
        data: query.data ?? null,
        loading: query.isLoading,
        isFetching: query.isFetching,
        error: query.isError ? extractErrorMessage(query.error, 'Không thể tải chi tiết phiếu yêu cầu máu') : null,
        refetch: query.refetch,
    }
}

export function useAvailableBags(
    requestId: number | null,
    requestItemId: number | null,
    includeCompatible: boolean,
    enabled = true,
) {
    const { can } = usePermission()
    const canApprove = can(PERMISSIONS.BLOOD_REQUEST_APPROVE)
    const validRequestId = typeof requestId === 'number' && requestId > 0 ? requestId : 0
    const validItemId = typeof requestItemId === 'number' && requestItemId > 0 ? requestItemId : 0

    const query = useQuery({
        queryKey: COORDINATION_QUERY_KEYS.availableBags(validRequestId, validItemId, includeCompatible),
        queryFn: () => coordinationApi.getAvailableBags(validRequestId, validItemId, includeCompatible),
        enabled: enabled && canApprove && validRequestId > 0 && validItemId > 0,
        ...SESSION_CACHE_OPTIONS,
    })

    return {
        bags: query.data ?? [],
        loading: query.isLoading,
        isFetching: query.isFetching,
        error: query.isError ? extractErrorMessage(query.error, 'Không thể tải danh sách túi máu khả dụng') : null,
        refetch: query.refetch,
    }
}

export function useAllocateBags() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({
            requestId,
            requestItemId,
            data,
        }: {
            requestId: number
            requestItemId: number
            data: AllocateBloodBagsRequest
        }) => coordinationApi.allocateBags(requestId, requestItemId, data),
        onSuccess: (_res, variables) => {
            queryClient.invalidateQueries({ queryKey: ['coordination', 'queue'] })
            queryClient.invalidateQueries({ queryKey: COORDINATION_QUERY_KEYS.detail(variables.requestId) })
            queryClient.invalidateQueries({ queryKey: ['coordination', 'availableBags'] })
            queryClient.invalidateQueries({ queryKey: ['coordination', 'supplyDemand'] })
            queryClient.invalidateQueries({ queryKey: ['inventory'] })
            queryClient.invalidateQueries({ queryKey: ['clinical'] })
        },
    })
}

export function useRejectBloodRequest() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({
            requestId,
            data,
        }: {
            requestId: number
            data: RejectBloodRequestDto
        }) => coordinationApi.rejectRequest(requestId, data),
        onSuccess: (_res, variables) => {
            queryClient.invalidateQueries({ queryKey: ['coordination', 'queue'] })
            queryClient.invalidateQueries({ queryKey: COORDINATION_QUERY_KEYS.detail(variables.requestId) })
            queryClient.invalidateQueries({ queryKey: ['coordination', 'availableBags'] })
            queryClient.invalidateQueries({ queryKey: ['coordination', 'supplyDemand'] })
            queryClient.invalidateQueries({ queryKey: ['inventory'] })
            queryClient.invalidateQueries({ queryKey: ['clinical'] })
        },
    })
}

const DISPATCHED_HANDOVER_CACHE_KEY = ['handover-dispatched-cache'] as const

export function useDispatchedHandoverMap() {
    return useQuery<Record<string, DispatchBloodBagResponse>>({
        queryKey: DISPATCHED_HANDOVER_CACHE_KEY,
        queryFn: () => ({}),
        staleTime: Infinity,
        gcTime: Infinity,
        enabled: false,
        initialData: {},
    })
}

export function useDispatchBag() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: (data: DispatchBloodBagRequest) => coordinationApi.dispatchBag(data),
        onSuccess: (res) => {
            queryClient.setQueryData<Record<string, DispatchBloodBagResponse>>(
                DISPATCHED_HANDOVER_CACHE_KEY,
                (prev) => ({
                    ...(prev ?? {}),
                    [res.barcode.trim().toLowerCase()]: res,
                }),
            )
            queryClient.invalidateQueries({ queryKey: ['inventory'] })
            queryClient.invalidateQueries({ queryKey: ['coordination'] })
            queryClient.invalidateQueries({ queryKey: COORDINATION_QUERY_KEYS.detail(res.requestId) })
            queryClient.invalidateQueries({ queryKey: ['clinical'] })
        },
    })
}
