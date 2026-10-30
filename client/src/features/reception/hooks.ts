import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { appointmentsApi } from './appointmentsApi'
import type { AppointmentFilterQuery, CheckInRequest, CreateWalkInRequest } from './types'

interface ListOptions {
    enabled?: boolean
    /** ms; false = không tự làm mới */
    refetchInterval?: number | false
}

export const useAppointments = (filter?: AppointmentFilterQuery, options?: ListOptions) =>
    useQuery({
        queryKey: ['appointments', 'list', filter],
        queryFn: () => appointmentsApi.list(filter),
        placeholderData: keepPreviousData,
        enabled: options?.enabled ?? true,
        refetchInterval: options?.refetchInterval ?? false,
    })

export const useCreateWalkIn = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: (body: CreateWalkInRequest) => appointmentsApi.createWalkIn(body),
        onSuccess: async () => {
            await qc.invalidateQueries({ queryKey: ['appointments'] }) // làm mới hàng chờ
        },
    })
}

export const useCheckIn = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: (body: CheckInRequest) => appointmentsApi.checkIn(body),
        onSuccess: async () => {
            await qc.invalidateQueries({ queryKey: ['appointments'] })
        },
    })
}