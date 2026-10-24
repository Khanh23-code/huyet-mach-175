import { axiosClient } from '@/services/axiosClient'
import type { Site } from './types'

export const sitesApi = {
    list: (isActive: boolean | undefined = true) =>
        axiosClient.get<Site[]>('/api/sites', { params: { isActive } }).then((r) => r.data),

    get: (id: number) => axiosClient.get<Site>(`/api/sites/${id}`).then((r) => r.data),
}
