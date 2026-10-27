import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { appointmentsApi } from './appointmentsApi'
import type { AppointmentFilterQuery, CreateWalkInRequest } from './types'

export const useAppointments = (filter?: AppointmentFilterQuery) =>
    useQuery({
        queryKey: ['appointments', 'list', filter],
        queryFn: () => appointmentsApi.list(filter),
        placeholderData: keepPreviousData,
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