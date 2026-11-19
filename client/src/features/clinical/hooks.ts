import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { message } from 'antd'
import { PERMISSIONS } from '@/config/permissions'
import { usePermission } from '@/features/auth/usePermission'
import { clinicalApi, FALLBACK_COMPONENT_TYPES } from './clinicalApi'
import type {
    ClinicalBloodRequestFilter,
    ConfirmReceiptRequest,
    CreateBloodRequestDto,
    CreateBloodReturnRequest,
    PendingBloodReturnFilter,
    PendingClinicalReceiptFilter,
    UpdateBloodRequestDto,
} from './types'

const SESSION_CACHE_OPTIONS = {
    staleTime: Infinity,
    gcTime: Infinity,
    refetchOnMount: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
} as const

export const CLINICAL_QUERY_KEYS = {
    requests: (filter?: ClinicalBloodRequestFilter) => ['clinical', 'requests', filter] as const,
    detail: (id: number) => ['clinical', 'request', id] as const,
    pendingReceipts: (filter?: PendingClinicalReceiptFilter) => ['clinical', 'pendingReceipts', filter] as const,
    pendingReturns: (filter?: PendingBloodReturnFilter) => ['clinical', 'pendingReturns', filter] as const,
    componentTypes: (canFetchFromProcessing: boolean) =>
        ['clinical', 'componentTypes', canFetchFromProcessing] as const,
}

/** Hook lấy danh sách phiếu yêu cầu máu phía Lâm sàng */
export function useClinicalRequests(
    filter: ClinicalBloodRequestFilter,
    options?: { enabled?: boolean },
) {
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)

    return useQuery({
        queryKey: CLINICAL_QUERY_KEYS.requests(filter),
        queryFn: () => clinicalApi.getRequests(filter),
        placeholderData: keepPreviousData,
        enabled: canView && (options?.enabled ?? true),
        ...SESSION_CACHE_OPTIONS,
    })
}

/** Hook lấy chi tiết 1 phiếu yêu cầu */
export function useClinicalRequestDetail(id: number | null) {
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)

    return useQuery({
        queryKey: CLINICAL_QUERY_KEYS.detail(id ?? 0),
        queryFn: () => clinicalApi.getRequestById(id!),
        enabled: canView && typeof id === 'number' && id > 0,
        ...SESSION_CACHE_OPTIONS,
    })
}

/** Hook tạo mới hoặc lưu nháp phiếu yêu cầu máu */
export function useCreateBloodRequest() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: (dto: CreateBloodRequestDto) => clinicalApi.createRequest(dto),
        onSuccess: (data, variables) => {
            queryClient.invalidateQueries({ queryKey: ['clinical', 'requests'] })
            queryClient.invalidateQueries({ queryKey: ['coordination'] })
            if (variables.isDraft) {
                message.success(`Đã lưu nháp phiếu yêu cầu thành công (${data.requestCode})`)
            } else {
                message.success(`Đã gửi phiếu yêu cầu ${data.requestCode} đến Ngân hàng máu!`)
            }
        },
        onError: (err: any) => {
            const msg = err?.response?.data?.message || err?.message || 'Có lỗi xảy ra khi tạo phiếu yêu cầu.'
            message.error(msg)
        },
    })
}

/** Hook cập nhật nội dung phiếu nháp (DRAFT) theo ID gốc */
export function useUpdateDraftRequest() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ id, dto }: { id: number; dto: UpdateBloodRequestDto }) =>
            clinicalApi.updateDraftRequest(id, dto),
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ['clinical', 'requests'] })
            queryClient.invalidateQueries({ queryKey: CLINICAL_QUERY_KEYS.detail(data.requestId) })
            queryClient.invalidateQueries({ queryKey: ['coordination'] })
        },
        onError: (err: any) => {
            const msg = err?.response?.data?.message || err?.message || 'Có lỗi xảy ra khi cập nhật phiếu nháp.'
            message.error(msg)
        },
    })
}

/** Hook gửi chính thức phiếu nháp (DRAFT -> PENDING) theo ID gốc */
export function useSubmitDraftRequest() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: (id: number) => clinicalApi.submitDraftRequest(id),
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ['clinical', 'requests'] })
            queryClient.invalidateQueries({ queryKey: CLINICAL_QUERY_KEYS.detail(data.requestId) })
            queryClient.invalidateQueries({ queryKey: ['coordination'] })
            message.success(`Đã gửi phiếu yêu cầu ${data.requestCode} đến Ngân hàng máu!`)
        },
        onError: (err: any) => {
            const msg = err?.response?.data?.message || err?.message || 'Có lỗi xảy ra khi gửi phiếu yêu cầu.'
            message.error(msg)
        },
    })
}

/** Hook lấy danh sách túi máu đang chờ nhận tại Lâm sàng */
export function usePendingReceipts(
    filter?: PendingClinicalReceiptFilter,
    options?: { enabled?: boolean },
) {
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)

    return useQuery({
        queryKey: CLINICAL_QUERY_KEYS.pendingReceipts(filter),
        queryFn: () => clinicalApi.getPendingReceipts(filter),
        enabled: canView && (options?.enabled ?? true),
        ...SESSION_CACHE_OPTIONS,
    })
}

/** Hook quét đối soát & xác nhận nhận túi máu */
export function useConfirmReceipt() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: (payload: ConfirmReceiptRequest) => clinicalApi.confirmReceipt(payload),
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ['clinical'] })
            queryClient.invalidateQueries({ queryKey: ['coordination'] })
            message.success(`Đã đối soát & xác nhận nhận thành công túi máu ${data.barcode}!`)
        },
        onError: (err: any) => {
            const msg = err?.response?.data?.message || err?.message || 'Đối soát nhận túi máu thất bại.'
            message.error(msg)
        },
    })
}

/** Hook lập phiếu hoàn trả túi máu */
export function useCreateReturn() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: (payload: CreateBloodReturnRequest) => clinicalApi.createReturn(payload),
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ['clinical'] })
            queryClient.invalidateQueries({ queryKey: ['coordination'] })
            message.success(`Đã lập phiếu hoàn trả thành công túi máu ${data.barcode} (Mã phiếu #${data.returnId})`)
        },
        onError: (err: any) => {
            const msg = err?.response?.data?.message || err?.message || 'Lập phiếu hoàn trả thất bại.'
            message.error(msg)
        },
    })
}

/** Hook lấy danh sách loại chế phẩm máu (chỉ gọi API processing khi có quyền PROCESSING_VIEW) */
export function useComponentTypes() {
    const { can } = usePermission()
    const canFetchFromProcessing = can(PERMISSIONS.PROCESSING_VIEW)

    return useQuery({
        queryKey: CLINICAL_QUERY_KEYS.componentTypes(canFetchFromProcessing),
        queryFn: () =>
            canFetchFromProcessing
                ? clinicalApi.getComponentTypes()
                : Promise.resolve(FALLBACK_COMPONENT_TYPES),
        ...SESSION_CACHE_OPTIONS,
    })
}

/** Hook lấy danh sách phiếu hoàn trả đang chờ kiểm định tại khoa */
export function usePendingBloodReturns(
    filter?: PendingBloodReturnFilter,
    options?: { enabled?: boolean },
) {
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_RETURN_VIEW)

    return useQuery({
        queryKey: CLINICAL_QUERY_KEYS.pendingReturns(filter),
        queryFn: () => clinicalApi.getPendingReturns(filter),
        enabled: canView && (options?.enabled ?? true),
        ...SESSION_CACHE_OPTIONS,
    })
}
