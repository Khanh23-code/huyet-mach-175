import { useEffect, useMemo, useState } from 'react'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { PERMISSIONS } from '@/config/permissions'
import { usePermission } from '@/features/auth/usePermission'
import { coordinationApi } from './coordinationApi'
import type {
    AllocateBloodBagsRequest,
    BloodCoordinationQueueFilter,
    CancelHandoverSessionRequest,
    CreateHandoverSessionRequest,
    HandoverSessionDto,
    PendingHandoverSessionFilter,
    RejectBloodRequestDto,
    ReservedHandoverFilter,
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
    componentTypes: () => ['coordination', 'componentTypes'] as const,
    supplyDemand: (componentTypeId: string) =>
        ['coordination', 'supplyDemand', componentTypeId] as const,
    queue: (filter: BloodCoordinationQueueFilter) =>
        ['coordination', 'queue', filter] as const,
    detail: (requestId: string) =>
        ['coordination', 'detail', requestId] as const,
    availableBags: (
        requestId: string,
        requestItemId: string,
        includeCompatible: boolean,
    ) => ['coordination', 'availableBags', requestId, requestItemId, includeCompatible] as const,
    pendingSessions: (filter?: PendingHandoverSessionFilter) =>
        ['coordination', 'handoverSessions', 'pending', filter] as const,
    reservedRequests: (filter?: ReservedHandoverFilter) =>
        ['coordination', 'reservedRequests', filter] as const,
    sessionDetail: (sessionId: string) =>
        ['coordination', 'handoverSessions', sessionId] as const,
}

function extractErrorMessage(err: unknown, fallback: string): string | null {
    if (!err) return null
    const apiMsg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
    if (apiMsg) return apiMsg
    if (err instanceof Error && err.message) return err.message
    return fallback
}

export function useComponentTypes() {
    const [selectedTypeId, setSelectedTypeId] = useState<string>('')

    const query = useQuery({
        queryKey: COORDINATION_QUERY_KEYS.componentTypes(),
        queryFn: () => coordinationApi.getComponentTypes(),
        ...SESSION_CACHE_OPTIONS,
    })

    const types = useMemo(() => query.data ?? [], [query.data])

    useEffect(() => {
        if (types.length > 0 && !types.some((t) => t.componentTypeId === selectedTypeId)) {
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

export function useSupplyDemand(componentTypeId: string | null) {
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)
    const validId = componentTypeId?.trim() || ''

    const query = useQuery({
        queryKey: COORDINATION_QUERY_KEYS.supplyDemand(validId),
        queryFn: () => coordinationApi.getSupplyDemand(validId),
        enabled: canView && validId.length > 0,
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

export function useRequestDetail(requestId: string | null) {
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)
    const validId = requestId?.trim() || ''

    const query = useQuery({
        queryKey: COORDINATION_QUERY_KEYS.detail(validId),
        queryFn: () => coordinationApi.getRequestDetail(validId),
        enabled: canView && validId.length > 0,
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
    requestId: string | null,
    requestItemId: string | null,
    includeCompatible: boolean,
    enabled = true,
) {
    const { can } = usePermission()
    const canApprove = can(PERMISSIONS.BLOOD_REQUEST_APPROVE)
    const validRequestId = requestId?.trim() || ''
    const validItemId = requestItemId?.trim() || ''

    const query = useQuery({
        queryKey: COORDINATION_QUERY_KEYS.availableBags(validRequestId, validItemId, includeCompatible),
        queryFn: () => coordinationApi.getAvailableBags(validRequestId, validItemId, includeCompatible),
        enabled: enabled && canApprove && validRequestId.length > 0 && validItemId.length > 0,
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
            requestId: string
            requestItemId: string
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
            requestId: string
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

export function useReservedHandoverRequests(
    filter?: ReservedHandoverFilter,
    options?: { enabled?: boolean },
) {
    const { can } = usePermission()
    const canApprove = can(PERMISSIONS.BLOOD_REQUEST_APPROVE)

    return useQuery({
        queryKey: COORDINATION_QUERY_KEYS.reservedRequests(filter),
        queryFn: () => coordinationApi.getReservedRequests(filter),
        enabled: canApprove && (options?.enabled ?? true),
        ...SESSION_CACHE_OPTIONS,
    })
}

export function usePendingHandoverSessions(
    filter?: PendingHandoverSessionFilter,
    options?: { enabled?: boolean; refetchInterval?: number | false },
) {
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)

    return useQuery({
        queryKey: COORDINATION_QUERY_KEYS.pendingSessions(filter),
        queryFn: () => coordinationApi.getPendingHandoverSessions(filter),
        enabled: canView && (options?.enabled ?? true),
        refetchInterval: options?.refetchInterval ?? false,
        ...SESSION_CACHE_OPTIONS,
    })
}

export function useHandoverSessionDetail(
    sessionId: string | null,
    options?: {
        enabled?: boolean
        refetchInterval?: number | false | ((data?: HandoverSessionDto) => number | false)
    },
) {
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)
    const validId = sessionId?.trim() || ''
    const intervalOption = options?.refetchInterval

    return useQuery({
        queryKey: COORDINATION_QUERY_KEYS.sessionDetail(validId),
        queryFn: () => coordinationApi.getHandoverSessionById(validId),
        enabled: canView && validId.length > 0 && (options?.enabled ?? true),
        refetchInterval:
            typeof intervalOption === 'function'
                ? (query) => intervalOption(query.state.data)
                : (intervalOption ?? false),
        ...SESSION_CACHE_OPTIONS,
    })
}

export function useCreateHandoverSession() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: (data: CreateHandoverSessionRequest) => coordinationApi.createHandoverSession(data),
        onSuccess: (res) => {
            queryClient.invalidateQueries({ queryKey: ['coordination'] })
            queryClient.invalidateQueries({ queryKey: COORDINATION_QUERY_KEYS.detail(res.requestId) })
            queryClient.invalidateQueries({ queryKey: ['clinical'] })
            queryClient.invalidateQueries({ queryKey: ['inventory'] })
        },
    })
}

export function useCancelHandoverSession() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ sessionId, data }: { sessionId: string; data?: CancelHandoverSessionRequest }) =>
            coordinationApi.cancelHandoverSession(sessionId, data),
        onSuccess: (res) => {
            queryClient.invalidateQueries({ queryKey: ['coordination'] })
            queryClient.invalidateQueries({ queryKey: COORDINATION_QUERY_KEYS.detail(res.requestId) })
            queryClient.invalidateQueries({ queryKey: COORDINATION_QUERY_KEYS.sessionDetail(res.handoverSessionId) })
            queryClient.invalidateQueries({ queryKey: ['clinical'] })
            queryClient.invalidateQueries({ queryKey: ['inventory'] })
        },
    })
}
