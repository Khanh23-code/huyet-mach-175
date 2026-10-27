import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { donorsApi } from './donorsApi'
import type { CreateDonorRequest } from './types'

export const useDonorDetail = (id: number | null) =>
    useQuery({
        queryKey: ['donors', 'detail', id],
        queryFn: () => donorsApi.get(id as number),
        enabled: id !== null && id > 0,
    })

/** Bấm "Tìm kiếm" mới gọi -> dùng mutation thay vì query. */
export const useCheckEligibility = () =>
    useMutation({ mutationFn: (citizenId: string) => donorsApi.checkEligibility(citizenId) })

export const useCreateDonor = () => {
    const qc = useQueryClient()
    return useMutation({
        mutationFn: (body: CreateDonorRequest) => donorsApi.create(body),
        onSuccess: async () => {
            await qc.invalidateQueries({ queryKey: ['donors'] })
        },
    })
}