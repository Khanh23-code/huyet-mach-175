import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { PERMISSIONS } from '@/config/permissions'
import { usePermission } from '@/features/auth/usePermission'
import { returnsApi } from './returnsApi'
import type { PendingBloodReturnFilter, ProcessBloodReturnRequest } from './returnsTypes'

export const RETURN_QUERY_KEYS = {
    pending: (filter?: PendingBloodReturnFilter) => ['inventory', 'returns', 'pending', filter] as const,
}

export function usePendingReturns(
    filter: PendingBloodReturnFilter = { pageNumber: 1, pageSize: 20 },
    options?: { enabled?: boolean },
) {
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_RETURN_VIEW) || can(PERMISSIONS.BLOOD_RETURN_APPROVE)

    return useQuery({
        queryKey: RETURN_QUERY_KEYS.pending(filter),
        queryFn: () => returnsApi.pending(filter),
        placeholderData: keepPreviousData,
        enabled: canView && (options?.enabled ?? true),
    })
}

export function useProcessBloodReturn() {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: ({ returnId, body }: { returnId: string; body: ProcessBloodReturnRequest }) =>
            returnsApi.process(returnId, body),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ['inventory'] })
            qc.invalidateQueries({ queryKey: ['clinical'] })
            qc.invalidateQueries({ queryKey: ['coordination'] })
        },
    })
}
