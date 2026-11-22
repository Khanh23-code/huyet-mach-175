import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { screeningApi } from './screeningApi'
import type { ScreeningQueueQuery, UpdateClinicalRequest } from './types'

interface QueueOptions {
    enabled?: boolean
    refetchInterval?: number | false
}

export const useScreeningQueue = (filter: ScreeningQueueQuery, options?: QueueOptions) =>
    useQuery({
        queryKey: ['screening', 'queue', filter],
        queryFn: () => screeningApi.queue(filter),
        placeholderData: keepPreviousData,
        enabled: options?.enabled ?? true,
        refetchInterval: options?.refetchInterval ?? false,
    })

export const useSurveyDetail = (appointmentId: number | null, enabled = true) =>
    useQuery({
        queryKey: ['screening', 'detail', appointmentId],
        queryFn: () => screeningApi.detail(appointmentId as number),
        enabled: enabled && appointmentId !== null,
    })

export const useSaveClinical = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: ({ appointmentId, body }: { appointmentId: number; body: UpdateClinicalRequest }) =>
            screeningApi.saveClinical(appointmentId, body),
        onSuccess: async () => {
            await Promise.all([
                qc.invalidateQueries({ queryKey: ['screening'] }),
                qc.invalidateQueries({ queryKey: ['appointments'] }),
            ])
        },
    })
}