import { axiosClient } from '@/services/axiosClient'
import type { PagedResult } from '@/shared/types/api'
import type {
    AllocateBloodBagsRequest,
    AllocateBloodBagsResponse,
    AvailableBloodBagDto,
    BloodCoordinationQueueFilter,
    BloodRequestDetailDto,
    ComponentTypeOption,
    CoordinationQueueItemDto,
    DispatchBloodBagRequest,
    DispatchBloodBagResponse,
    RejectBloodRequestDto,
    RejectBloodRequestResponse,
    SupplyDemandResponse,
} from './types'

export const FALLBACK_COMPONENT_TYPES: ComponentTypeOption[] = [
    { componentTypeId: 2, typeCode: 'PRBC', typeName: 'Hồng cầu lắng', lifeDays: 42 },
    { componentTypeId: 4, typeCode: 'PLT', typeName: 'Khối tiểu cầu', lifeDays: 5 },
    { componentTypeId: 3, typeCode: 'FFP', typeName: 'Huyết tương', lifeDays: 365 },
    { componentTypeId: 1, typeCode: 'WB', typeName: 'Tủa lạnh', lifeDays: 35 },
]

export const coordinationApi = {
    /**
     * Thống kê Bảng điều phối Cung - Cầu theo loại chế phẩm
     * GET /api/blood-coordination/supply-demand?componentTypeId={id}
     */
    getSupplyDemand: (componentTypeId: number) =>
        axiosClient
            .get<SupplyDemandResponse>('/api/blood-coordination/supply-demand', {
                params: { componentTypeId },
            })
            .then((res) => res.data),

    /**
     * Lấy danh sách loại chế phẩm máu phục vụ chuyển tab
     */
    getComponentTypes: async (): Promise<ComponentTypeOption[]> => {
        try {
            const res = await axiosClient.get<ComponentTypeOption[]>('/api/blood-processing/component-types')
            if (Array.isArray(res.data) && res.data.length > 0) {
                const items = [...res.data]
                if (!items.some((item) => item.typeCode === 'WB')) {
                    const wb = FALLBACK_COMPONENT_TYPES.find((f) => f.typeCode === 'WB')
                    if (wb) items.push(wb)
                }
                return items
            }
            return FALLBACK_COMPONENT_TYPES
        } catch {
            return FALLBACK_COMPONENT_TYPES
        }
    },

    /**
     * Lấy danh sách hàng đợi yêu cầu máu chờ xử lý
     * GET /api/blood-coordination/queue
     */
    getQueue: (params?: BloodCoordinationQueueFilter) =>
        axiosClient
            .get<PagedResult<CoordinationQueueItemDto>>('/api/blood-coordination/queue', { params })
            .then((res) => res.data),

    /**
     * Xem thông tin chi tiết một phiếu yêu cầu máu
     * GET /api/blood-requests/{id}
     */
    getRequestDetail: (id: number) =>
        axiosClient
            .get<BloodRequestDetailDto>(`/api/blood-requests/${id}`)
            .then((res) => res.data),

    /**
     * Từ chối phiếu yêu cầu máu kèm lý do
     * PUT /api/blood-requests/{id}/reject
     */
    rejectRequest: (id: number, data: RejectBloodRequestDto) =>
        axiosClient
            .put<RejectBloodRequestResponse>(`/api/blood-requests/${id}/reject`, data)
            .then((res) => res.data),

    /**
     * Lấy danh sách túi máu khả dụng để giữ chỗ cho dòng y lệnh
     * GET /api/blood-coordination/requests/{requestId}/items/{requestItemId}/available-bags
     */
    getAvailableBags: (requestId: number, requestItemId: number, includeCompatible = false) =>
        axiosClient
            .get<AvailableBloodBagDto[]>(
                `/api/blood-coordination/requests/${requestId}/items/${requestItemId}/available-bags`,
                { params: { includeCompatible } },
            )
            .then((res) => res.data),

    /**
     * Xác nhận giữ chỗ các túi máu đã chọn cho dòng y lệnh
     * POST /api/blood-coordination/requests/{requestId}/items/{requestItemId}/allocate
     */
    allocateBags: (requestId: number, requestItemId: number, data: AllocateBloodBagsRequest) =>
        axiosClient
            .post<AllocateBloodBagsResponse>(
                `/api/blood-coordination/requests/${requestId}/items/${requestItemId}/allocate`,
                data,
            )
            .then((res) => res.data),

    /**
     * Xuất kho bàn giao túi máu đã giữ chỗ (RESERVED -> ISSUED)
     * POST /api/blood-handovers/dispatch
     */
    dispatchBag: (data: DispatchBloodBagRequest) =>
        axiosClient
            .post<DispatchBloodBagResponse>('/api/blood-handovers/dispatch', data)
            .then((res) => res.data),
}
