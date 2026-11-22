import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { labTestsApi } from './labTestsApi'
import type { LabQueueQuery, RecordLabTestRequest } from './labTestsTypes'

export const useLabQueue = (filter: LabQueueQuery, options?: { refetchInterval?: number | false }) =>
    useQuery({
        queryKey: ['inventory', 'lab-queue', filter],
        queryFn: () => labTestsApi.queue(filter),
        placeholderData: keepPreviousData,
        refetchInterval: options?.refetchInterval ?? false,
    })

export const useRecordLabTest = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: (body: RecordLabTestRequest) => labTestsApi.record(body),
        onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory'] }),
    })
}