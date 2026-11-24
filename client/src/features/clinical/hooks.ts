import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { message } from 'antd'
import { PERMISSIONS } from '@/config/permissions'
import { usePermission } from '@/features/auth/usePermission'
import { clinicalApi, FALLBACK_COMPONENT_TYPES } from './clinicalApi'
import { formatBloodGroup } from '@/shared/formatters/medical'
import { parseApiError } from '@/services/apiError'
import type {
    CancelBloodReturnRequest,
    ClinicalBloodRequestFilter,
    ConfirmHandoverSessionRequest,
    ConfirmReceiptRequest,
    ConfirmTransfusionRequest,
    CreateBloodRequestDto,
    CreateBloodReturnRequest,
    EligibleReturnBag,
    HandoverSessionDto,
    IneligibleReturnBag,
    PendingBloodReturnFilter,
    PendingClinicalReceiptFilter,
    PendingHandoverSessionFilter,
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
    pendingHandoverSessions: (filter?: PendingHandoverSessionFilter) =>
        ['clinical', 'handoverSessions', 'pending', filter] as const,
    handoverSessionDetail: (sessionId: number) =>
        ['clinical', 'handoverSessions', sessionId] as const,
    transfusionScan: (barcode: string) => ['clinical', 'transfusionScan', barcode] as const,
    pendingReceipts: (filter?: PendingClinicalReceiptFilter) => ['clinical', 'pendingReceipts', filter] as const,
    pendingReturns: (filter?: PendingBloodReturnFilter) => ['clinical', 'pendingReturns', filter] as const,
    eligibleReturnBags: () => ['clinical', 'eligibleReturnBags'] as const,
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

/** Hook lấy danh sách phiên bàn giao máu đang chờ xác nhận tại Lâm sàng */
export function usePendingHandoverSessions(
    filter?: PendingHandoverSessionFilter,
    options?: { enabled?: boolean; refetchInterval?: number | false },
) {
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)

    return useQuery({
        queryKey: CLINICAL_QUERY_KEYS.pendingHandoverSessions(filter),
        queryFn: () => clinicalApi.getPendingHandoverSessions(filter),
        enabled: canView && (options?.enabled ?? true),
        refetchInterval: options?.refetchInterval ?? false,
        ...SESSION_CACHE_OPTIONS,
    })
}

/** Hook xem chi tiết một phiên bàn giao máu */
export function useHandoverSessionDetail(
    sessionId: number | null,
    options?: {
        enabled?: boolean
        refetchInterval?: number | false | ((data?: HandoverSessionDto) => number | false)
    },
) {
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)
    const validId = typeof sessionId === 'number' && sessionId > 0 ? sessionId : 0
    const intervalOption = options?.refetchInterval

    return useQuery({
        queryKey: CLINICAL_QUERY_KEYS.handoverSessionDetail(validId),
        queryFn: () => clinicalApi.getHandoverSessionById(validId),
        enabled: canView && validId > 0 && (options?.enabled ?? true),
        refetchInterval:
            typeof intervalOption === 'function'
                ? (query) => intervalOption(query.state.data)
                : (intervalOption ?? false),
        ...SESSION_CACHE_OPTIONS,
    })
}

/** Hook quét đối soát & xác nhận nhận lô túi máu theo phiên (Pha 2 - CLIN) */
export function useConfirmHandoverSession() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ sessionId, data }: { sessionId: number; data: ConfirmHandoverSessionRequest }) =>
            clinicalApi.confirmHandoverSession(sessionId, data),
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ['clinical'] })
            queryClient.invalidateQueries({ queryKey: ['coordination'] })
            queryClient.invalidateQueries({ queryKey: ['inventory'] })
            message.success(`Đã xác nhận nhận lô ${data.items.length} túi máu thuộc phiên ${data.sessionCode}!`)
        },
        onError: (err: unknown) => {
            message.error(parseApiError(err).message)
        },
    })
}

/** Hook xác nhận bắt đầu truyền máu thực tế tại giường (ISSUED -> TRANSFUSED) */
export function useConfirmTransfusion() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: (payload: ConfirmTransfusionRequest) => clinicalApi.confirmTransfusion(payload),
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ['clinical'] })
            queryClient.invalidateQueries({ queryKey: ['coordination'] })
            queryClient.invalidateQueries({ queryKey: ['inventory'] })
            message.success(`Đã xác nhận bắt đầu truyền túi máu ${data.barcode} cho NB ${data.patientName}.`)
        },
        onError: (err: unknown) => {
            message.error(parseApiError(err).message)
        },
    })
}

/** @deprecated Sẽ gỡ ở bước FE-1.3 */
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
            queryClient.invalidateQueries({ queryKey: ['inventory'] })
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
            queryClient.invalidateQueries({ queryKey: ['inventory'] })
            if (data.isEligibleForRestockEvaluation) {
                message.success(`Đã lập phiếu hoàn trả thành công túi máu ${data.barcode} (Mã phiếu #${data.returnId})`)
            } else {
                message.warning(
                    `Đã lập phiếu hoàn trả #${data.returnId} cho túi ${data.barcode} kèm cảnh báo không đạt tái nhập kho (${data.warningFlags.join('; ')}).`,
                )
            }
        },
        onError: (err: unknown) => {
            message.error(parseApiError(err).message)
        },
    })
}

/** Hook hủy phiếu hoàn trả lập nhầm (chưa kiểm định) */
export function useCancelReturn() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ returnId, data }: { returnId: number; data?: CancelBloodReturnRequest }) =>
            clinicalApi.cancelReturn(returnId, data),
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ['clinical'] })
            queryClient.invalidateQueries({ queryKey: ['coordination'] })
            queryClient.invalidateQueries({ queryKey: ['inventory'] })
            message.success(`Đã hủy phiếu hoàn trả #${data.returnId}; túi ${data.barcode} được khôi phục về ISSUED.`)
        },
        onError: (err: unknown) => {
            message.error(parseApiError(err).message)
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

function resolveIneligibleReason(bagStatus?: string, allocationStatus?: string, hasPendingReturn?: boolean): string {
    if (hasPendingReturn || bagStatus === 'LOCKED') {
        return 'Túi máu đang bị phong tỏa / đã có phiếu hoàn trả chờ kiểm định'
    }
    if (allocationStatus === 'RETURNED') {
        return 'Túi máu đã hoàn trả trước đó'
    }
    if (bagStatus === 'RESERVED' || allocationStatus === 'RESERVED') {
        return 'Túi máu mới giữ chỗ (RESERVED), chưa xuất kho từ BBNK'
    }
    if (bagStatus === 'TRANSFUSED') {
        return 'Túi máu đã truyền cho người bệnh'
    }
    if (bagStatus === 'DISCARDED') {
        return 'Túi máu đã tiêu hủy'
    }
    return `Trạng thái hiện tại không đủ điều kiện (${bagStatus || allocationStatus || 'Không rõ'})`
}

/**
 * Tổng hợp danh sách túi máu đủ điều kiện hoàn trả (và túi không đủ điều kiện để khóa chọn)
 * dựa trên đúng các API Backend hiện có:
 * - GET /api/blood-handovers/clinical/pending-receipt (túi ISSUED chưa xác nhận nhận thuộc khoa)
 * - GET /api/blood-requests/clinical + GET /api/blood-requests/{id} (túi đã xác nhận nhận COMPLETED & ISSUED)
 * - GET /api/blood-returns/pending (loại trừ lượt phân bổ đã lập phiếu hoàn trả)
 */
export function useEligibleReturnBags(options?: { enabled?: boolean }) {
    const { can } = usePermission()
    const canViewRequests = can(PERMISSIONS.BLOOD_REQUEST_VIEW)
    const canViewReturns = can(PERMISSIONS.BLOOD_RETURN_VIEW)

    return useQuery({
        queryKey: CLINICAL_QUERY_KEYS.eligibleReturnBags(),
        enabled: canViewRequests && (options?.enabled ?? true),
        queryFn: async () => {
            const [requestsPage, pendingReturnsPage] = await Promise.all([
                clinicalApi.getRequests({ pageNumber: 1, pageSize: 100 }),
                canViewReturns
                    ? clinicalApi.getPendingReturns({ pageNumber: 1, pageSize: 100 })
                    : Promise.resolve({ items: [], totalCount: 0, pageNumber: 1, pageSize: 100, totalPages: 0 }),
            ])

            const pendingReturnAllocIds = new Set<number>(
                (pendingReturnsPage?.items || []).map((r) => r.allocationId),
            )

            const requestsWithAllocations = (requestsPage?.items || []).filter(
                (r) => r.totalAllocatedUnits > 0 && r.status !== 'DRAFT' && r.status !== 'REJECTED',
            )

            const details = await Promise.all(
                requestsWithAllocations.map((r) => clinicalApi.getRequestById(r.requestId).catch(() => null)),
            )

            const eligibleMap = new Map<number, EligibleReturnBag>()
            const ineligibleMap = new Map<number, IneligibleReturnBag>()
            const nowMs = Date.now()

            for (const req of details) {
                if (!req) continue
                for (const alloc of req.allocations || []) {
                    const hasPendingReturn = pendingReturnAllocIds.has(alloc.allocationId)
                    const isEligible =
                        !hasPendingReturn &&
                        alloc.bagStatus === 'ISSUED' &&
                        (alloc.status === 'ISSUED' || alloc.status === 'COMPLETED')

                    const bloodGroup =
                        alloc.bloodType && alloc.rhFactor
                            ? formatBloodGroup(alloc.bloodType, alloc.rhFactor)
                            : alloc.bloodType || '—'

                    if (isEligible) {
                        const isExpired = Boolean(alloc.expiredAt && new Date(alloc.expiredAt).getTime() <= nowMs)
                        eligibleMap.set(alloc.allocationId, {
                            allocationId: alloc.allocationId,
                            bagId: alloc.bagId,
                            barcode: alloc.barcode,
                            componentCode: alloc.componentCode,
                            componentName: alloc.componentName,
                            bloodType: bloodGroup,
                            volumeMl: alloc.volumeMl,
                            expiredAt: alloc.expiredAt,
                            isExpired,
                            requestId: req.requestId,
                            requestCode: req.requestCode,
                            patientCode: req.patientCode,
                            patientName: req.patientName,
                            allocationStatus: alloc.status,
                            bagStatus: 'ISSUED',
                            issuedAt: alloc.issuedAt,
                            receivedByName: alloc.receivedByName,
                            isReceivedAtClinical: alloc.status === 'COMPLETED',
                        })
                    } else {
                        ineligibleMap.set(alloc.allocationId, {
                            allocationId: alloc.allocationId,
                            bagId: alloc.bagId,
                            barcode: alloc.barcode,
                            componentName: alloc.componentName,
                            bloodType: bloodGroup,
                            volumeMl: alloc.volumeMl,
                            requestCode: req.requestCode,
                            patientCode: req.patientCode,
                            patientName: req.patientName,
                            allocationStatus: alloc.status,
                            bagStatus: alloc.bagStatus,
                            ineligibleReason: resolveIneligibleReason(
                                alloc.bagStatus,
                                alloc.status,
                                hasPendingReturn,
                            ),
                        })
                    }
                }
            }

            return {
                eligibleBags: Array.from(eligibleMap.values()),
                ineligibleBags: Array.from(ineligibleMap.values()),
            }
        },
        ...SESSION_CACHE_OPTIONS,
    })
}
