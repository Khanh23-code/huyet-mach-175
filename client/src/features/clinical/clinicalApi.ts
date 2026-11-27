import { axiosClient } from '@/services/axiosClient'
import type { PagedResult } from '@/shared/types/api'
import type {
    BloodRequestDetailDto,
    CancelBloodReturnRequest,
    CancelBloodReturnResponse,
    ClinicalBloodRequestDto,
    ClinicalBloodRequestFilter,
    ComponentTypeOption,
    ConfirmHandoverSessionRequest,
    ConfirmTransfusionRequest,
    ConfirmTransfusionResponse,
    CreateBloodRequestDto,
    CreateBloodRequestResponse,
    CreateBloodReturnRequest,
    CreateBloodReturnResponse,
    HandoverSessionDto,
    PendingBloodReturnDto,
    PendingBloodReturnFilter,
    PendingHandoverSessionFilter,
    SubmitBloodRequestResponse,
    TransfusionScanDto,
    UpdateBloodRequestDto,
} from './types'

const CLINICAL_COMPONENT_ORDER = ['PRBC', 'FFP', 'PLT', 'WB']

export const clinicalApi = {
    /** Lấy danh sách phiếu yêu cầu máu phía Lâm sàng (của bác sĩ đang đăng nhập) */
    getRequests: (params?: ClinicalBloodRequestFilter) =>
        axiosClient
            .get<PagedResult<ClinicalBloodRequestDto>>('/api/blood-requests/clinical', { params })
            .then((r) => r.data),

    /** Xem chi tiết phiếu yêu cầu máu */
    getRequestById: (id: string) =>
        axiosClient
            .get<BloodRequestDetailDto>(`/api/blood-requests/${id}`)
            .then((r) => r.data),

    /** Tạo mới hoặc lưu nháp phiếu yêu cầu máu */
    createRequest: (data: CreateBloodRequestDto) =>
        axiosClient
            .post<CreateBloodRequestResponse>('/api/blood-requests', data)
            .then((r) => r.data),

    /** Cập nhật nội dung phiếu nháp (DRAFT) theo ID gốc */
    updateDraftRequest: (id: string, data: UpdateBloodRequestDto) =>
        axiosClient
            .put<BloodRequestDetailDto>(`/api/blood-requests/${id}`, data)
            .then((r) => r.data),

    /** Gửi chính thức phiếu nháp (DRAFT -> PENDING) theo ID gốc */
    submitDraftRequest: (id: string) =>
        axiosClient
            .patch<SubmitBloodRequestResponse>(`/api/blood-requests/${id}/submit`)
            .then((r) => r.data),

    /** Lấy danh sách phiên bàn giao máu đang chờ xác nhận tại quầy (khoa hiện tại) */
    getPendingHandoverSessions: (params?: PendingHandoverSessionFilter) =>
        axiosClient
            .get<HandoverSessionDto[]>('/api/blood-handovers/sessions/pending', { params })
            .then((r) => r.data),

    /** Xem chi tiết một phiên bàn giao máu */
    getHandoverSessionById: (sessionId: string) =>
        axiosClient
            .get<HandoverSessionDto>(`/api/blood-handovers/sessions/${sessionId}`)
            .then((r) => r.data),

    /** Quét đối soát & xác nhận nhận lô túi máu trong phiên bàn giao (Pha 2 - CLIN) */
    confirmHandoverSession: (sessionId: string, data: ConfirmHandoverSessionRequest) =>
        axiosClient
            .post<HandoverSessionDto>(`/api/blood-handovers/sessions/${sessionId}/confirm`, data)
            .then((r) => r.data),

    /** Quét barcode túi máu ISSUED đối chiếu thông tin bệnh nhân trước khi truyền tại giường */
    scanTransfusionBag: (barcode: string) =>
        axiosClient
            .get<TransfusionScanDto>(`/api/blood-handovers/clinical/transfusion-scan/${encodeURIComponent(barcode.trim())}`)
            .then((r) => r.data),

    /** Xác nhận bắt đầu truyền máu thực tế tại giường (ISSUED -> TRANSFUSED) */
    confirmTransfusion: (data: ConfirmTransfusionRequest) =>
        axiosClient
            .post<ConfirmTransfusionResponse>('/api/blood-handovers/clinical/confirm-transfusion', data)
            .then((r) => r.data),

    /** Lập phiếu hoàn trả túi máu chưa sử dụng */
    createReturn: (data: CreateBloodReturnRequest) =>
        axiosClient
            .post<CreateBloodReturnResponse>('/api/blood-returns', data)
            .then((r) => r.data),

    /** Hủy phiếu hoàn trả lập nhầm khi Ngân hàng máu chưa kiểm định */
    cancelReturn: (returnId: string, data?: CancelBloodReturnRequest) =>
        axiosClient
            .post<CancelBloodReturnResponse>(`/api/blood-returns/${returnId}/cancel`, data ?? {})
            .then((r) => r.data),

    /** Lấy danh sách phiếu hoàn trả đang chờ kiểm định */
    getPendingReturns: (params?: PendingBloodReturnFilter) =>
        axiosClient
            .get<PagedResult<PendingBloodReturnDto>>('/api/blood-returns/pending', { params })
            .then((r) => r.data),

    /** Lấy danh sách loại chế phẩm máu từ danh mục dùng chung */
    getComponentTypes: async (): Promise<ComponentTypeOption[]> => {
        const res = await axiosClient.get<ComponentTypeOption[]>('/api/inventory/blood-component-types')
        const items = Array.isArray(res.data) ? [...res.data] : []
        return items.sort((a, b) => {
            const ia = CLINICAL_COMPONENT_ORDER.indexOf(a.typeCode.toUpperCase())
            const ib = CLINICAL_COMPONENT_ORDER.indexOf(b.typeCode.toUpperCase())
            return (ia === -1 ? 99 : ia) - (ib === -1 ? 99 : ib)
        })
    },
}
