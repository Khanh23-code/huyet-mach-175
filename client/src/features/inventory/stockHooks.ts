import { useCallback, useState } from 'react'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { parseApiError } from '@/services/apiError'
import { locationsApi, stockApi } from './stockApi'
import { markOfflineError, removeOffline, toRequest, useOfflineQueue } from './offlineQueue'
import { isNetworkError } from './stockFormat'
import type {
    BagQuery, ExpiringQuery, LocationQuery, OfflineTransition, RelocateRequest, StorageLocationInput,
    TransitionRequest,
} from './stockTypes'

// Mọi key nằm dưới ['inventory', ...] để mutation cũ/mới invalidate chung.

export const useInventorySummary = () =>
    useQuery({ queryKey: ['inventory', 'summary'], queryFn: () => stockApi.summary(), staleTime: 30_000 })

export const useBags = (params: BagQuery, enabled = true) =>
    useQuery({
        queryKey: ['inventory', 'bags', params],
        queryFn: () => stockApi.bags(params),
        placeholderData: keepPreviousData,
        enabled,
    })

export const useExpiringSoon = (params: ExpiringQuery, refetchMs?: number) =>
    useQuery({
        queryKey: ['inventory', 'expiring', params],
        queryFn: () => stockApi.expiringSoon(params),
        placeholderData: keepPreviousData,
        refetchInterval: refetchMs ?? false,
        staleTime: 0, // luôn lấy mới khi vào lại trang
    })

/** Danh sách tủ cho trang quản lý (khác useStorageLocations chỉ lấy tủ còn chỗ). */
export const useManagedLocations = (params: LocationQuery) =>
    useQuery({
        queryKey: ['inventory', 'locations', 'managed', params],
        queryFn: () => locationsApi.list(params),
        placeholderData: keepPreviousData,
    })

export const useRecentLogs = (limit: number) =>
    useQuery({
        queryKey: ['inventory', 'status-logs', limit],
        queryFn: () => stockApi.recentLogs(limit),
        refetchInterval: 30_000,
    })

const useInvalidate = () => {
    const qc = useQueryClient()
    return () => qc.invalidateQueries({ queryKey: ['inventory'] })
}

export const useCreateLocation = () => {
    const done = useInvalidate()
    return useMutation({ mutationFn: (b: StorageLocationInput) => locationsApi.create(b), onSuccess: done })
}

export const useUpdateLocation = () => {
    const done = useInvalidate()
    return useMutation({
        mutationFn: (p: { id: number; body: StorageLocationInput }) => locationsApi.update(p.id, p.body),
        onSuccess: done,
    })
}

export const useDeleteLocation = () => {
    const done = useInvalidate()
    return useMutation({ mutationFn: (id: number) => locationsApi.remove(id), onSuccess: done })
}

export const useRelocateBag = () => {
    const done = useInvalidate()
    return useMutation({
        mutationFn: (p: { bagId: number; body: RelocateRequest }) => stockApi.relocate(p.bagId, p.body),
        onSuccess: done,
    })
}

export const useTransitionBag = () => {
    const done = useInvalidate()
    return useMutation({ mutationFn: (b: TransitionRequest) => stockApi.transition(b), onSuccess: done })
}

/** Gửi lại các lệnh trong hàng đợi ngoại tuyến. */
export const useOfflineSync = () => {
    const items = useOfflineQueue()
    const done = useInvalidate()
    const [busyId, setBusyId] = useState<string | null>(null)
    const [syncing, setSyncing] = useState(false)

    /** true = gửi xong; false = giữ lại (mất mạng hoặc BE từ chối). */
    const syncOne = useCallback(
        async (item: OfflineTransition) => {
            setBusyId(item.id)
            try {
                await stockApi.transition(toRequest(item))
                removeOffline(item.id)
                await done()
                return true
            } catch (e) {
                if (!isNetworkError(e)) markOfflineError(item.id, parseApiError(e).message)
                return false
            } finally {
                setBusyId(null)
            }
        },
        // eslint-disable-next-line react-hooks/exhaustive-deps
        [],
    )

    const syncAll = useCallback(async () => {
        setSyncing(true)
        let ok = 0
        for (const it of items) {
            if (await syncOne(it)) ok++
            else if (!navigator.onLine) break
        }
        setSyncing(false)
        return ok
    }, [items, syncOne])

    return { items, busyId, syncing, syncOne, syncAll }
}