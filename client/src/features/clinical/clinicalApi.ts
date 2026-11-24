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
    ConfirmReceiptRequest,
    ConfirmReceiptResponse,
    ConfirmTransfusionRequest,
    ConfirmTransfusionResponse,
    CreateBloodRequestDto,
    CreateBloodRequestResponse,
    CreateBloodReturnRequest,
    CreateBloodReturnResponse,
    HandoverSessionDto,
    PendingBloodReturnDto,
    PendingBloodReturnFilter,
    PendingClinicalReceiptFilter,
    PendingHandoverSessionFilter,
    PendingReceiptBagDto,
    SubmitBloodRequestResponse,
    TransfusionScanDto,
    UpdateBloodRequestDto,
} from './types'

export const FALLBACK_COMPONENT_TYPES: ComponentTypeOption[] = [
    { componentTypeId: 2, typeCode: 'PRBC', typeName: 'Khối hồng cầu (Hồng cầu lắng)', lifeDays: 42 },
    { componentTypeId: 3, typeCode: 'FFP', typeName: 'Huyết tương tươi đông lạnh', lifeDays: 365 },
    { componentTypeId: 4, typeCode: 'PLT', typeName: 'Khối tiểu cầu', lifeDays: 5 },
    { componentTypeId: 1, typeCode: 'WB', typeName: 'Máu toàn phần', lifeDays: 35 },
]

export const clinicalApi = {
    /** Lấy danh sách phiếu yêu cầu máu phía Lâm sàng (của bác sĩ đang đăng nhập) */
    getRequests: (params?: ClinicalBloodRequestFilter) =>
        axiosClient
            .get<PagedResult<ClinicalBloodRequestDto>>('/api/blood-requests/clinical', { params })
            .then((r) => r.data),

    /** Xem chi tiết phiếu yêu cầu máu */
    getRequestById: (id: number) =>
        axiosClient
            .get<BloodRequestDetailDto>(`/api/blood-requests/${id}`)
            .then((r) => r.data),

    /** Tạo mới hoặc lưu nháp phiếu yêu cầu máu */
    createRequest: (data: CreateBloodRequestDto) =>
        axiosClient
            .post<CreateBloodRequestResponse>('/api/blood-requests', data)
            .then((r) => r.data),

    /** Cập nhật nội dung phiếu nháp (DRAFT) theo ID gốc */
    updateDraftRequest: (id: number, data: UpdateBloodRequestDto) =>
        axiosClient
            .put<BloodRequestDetailDto>(`/api/blood-requests/${id}`, data)
            .then((r) => r.data),

    /** Gửi chính thức phiếu nháp (DRAFT -> PENDING) theo ID gốc */
    submitDraftRequest: (id: number) =>
        axiosClient
            .patch<SubmitBloodRequestResponse>(`/api/blood-requests/${id}/submit`)
            .then((r) => r.data),

    /** Lấy danh sách phiên bàn giao máu đang chờ xác nhận tại quầy (khoa hiện tại) */
    getPendingHandoverSessions: (params?: PendingHandoverSessionFilter) =>
        axiosClient
            .get<HandoverSessionDto[]>('/api/blood-handovers/sessions/pending', { params })
            .then((r) => r.data),

    /** Xem chi tiết một phiên bàn giao máu */
    getHandoverSessionById: (sessionId: number) =>
        axiosClient
            .get<HandoverSessionDto>(`/api/blood-handovers/sessions/${sessionId}`)
            .then((r) => r.data),

    /** Quét đối soát & xác nhận nhận lô túi máu trong phiên bàn giao (Pha 2 - CLIN) */
    confirmHandoverSession: (sessionId: number, data: ConfirmHandoverSessionRequest) =>
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

    /** @deprecated Sẽ gỡ ở bước FE-1.3 sau khi chuyển sang sessions/pending */
    getPendingReceipts: (params?: PendingClinicalReceiptFilter) =>
        axiosClient
            .get<PendingReceiptBagDto[]>('/api/blood-handovers/clinical/pending-receipt', { params })
            .then((r) => r.data),

    /** @deprecated Sẽ gỡ ở bước FE-1.3 sau khi chuyển sang sessions/{id}/confirm */
    confirmReceipt: (data: ConfirmReceiptRequest) =>
        axiosClient
            .post<ConfirmReceiptResponse>('/api/blood-handovers/clinical/confirm-receipt', data)
            .then((r) => r.data),

    /** Lập phiếu hoàn trả túi máu chưa sử dụng */
    createReturn: (data: CreateBloodReturnRequest) =>
        axiosClient
            .post<CreateBloodReturnResponse>('/api/blood-returns', data)
            .then((r) => r.data),

    /** Hủy phiếu hoàn trả lập nhầm khi Ngân hàng máu chưa kiểm định */
    cancelReturn: (returnId: number, data?: CancelBloodReturnRequest) =>
        axiosClient
            .post<CancelBloodReturnResponse>(`/api/blood-returns/${returnId}/cancel`, data ?? {})
            .then((r) => r.data),

    /** Lấy danh sách phiếu hoàn trả đang chờ kiểm định */
    getPendingReturns: (params?: PendingBloodReturnFilter) =>
        axiosClient
            .get<PagedResult<PendingBloodReturnDto>>('/api/blood-returns/pending', { params })
            .then((r) => r.data),

    /** Lấy danh sách loại chế phẩm máu (ưu tiên API, fallback mảng chuẩn nếu 403 do khác phân quyền) */
    getComponentTypes: async (): Promise<ComponentTypeOption[]> => {
        try {
            const res = await axiosClient.get<ComponentTypeOption[]>('/api/blood-processing/component-types')
            if (Array.isArray(res.data) && res.data.length > 0) {
                return res.data
            }
            return FALLBACK_COMPONENT_TYPES
        } catch {
            return FALLBACK_COMPONENT_TYPES
        }
    },
}
