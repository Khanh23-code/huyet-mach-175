import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import {
    Alert,
    App,
    Badge,
    Button,
    Drawer,
    Empty,
    Input,
    Modal,
    Select,
    Spin,
    Table,
    Tabs,
    Tag,
    Tooltip,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
    CheckCircleOutlined,
    ClockCircleOutlined,
    CloseCircleOutlined,
    CopyOutlined,
    EyeOutlined,
    InboxOutlined,
    PlusOutlined,
    ReloadOutlined,
    RightOutlined,
    SearchOutlined,
} from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import { useCurrentUser } from '@/features/auth/useCurrentUser'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { stockApi } from '@/features/inventory/stockApi'
import { useBags, useRecentLogs } from '@/features/inventory/stockHooks'
import { BLOOD_GROUPS, bloodGroupParams } from '@/features/inventory/stockFormat'
import { BagStatusTag } from '@/features/inventory/components/StockBadges'
import type { BagScan, BloodBag, StatusLog } from '@/features/inventory/stockTypes'
import { getStatusMeta, getStatusTone } from '@/features/clinical/format'
import { parseApiError } from '@/services/apiError'
import { computeRemainingSeconds, formatCountdown, formatDateTime } from '@/shared/formatters/date'
import {
    BarcodeScanBox,
    BarcodeSvg,
    BloodGroupTag,
    PageContainer,
    PageHeader,
    StatusPill,
    UrgencyTag,
} from '@/shared/components'
import {
    useCancelHandoverSession,
    useCreateHandoverSession,
    usePendingHandoverSessions,
    useReservedHandoverRequests,
} from '../hooks'
import type {
    HandoverSessionDto,
    ReservedHandoverBagDto,
    ReservedHandoverRequestDto,
} from '../types'
import '@/shared/styles/HandoverLayout.css'

const COMPONENT_OPTIONS = [
    { value: 'ALL', label: 'Tất cả chế phẩm' },
    { value: 'PRBC', label: 'Hồng cầu lắng (PRBC)' },
    { value: 'PLT', label: 'Khối tiểu cầu (PLT)' },
    { value: 'FFP', label: 'Huyết tương (FFP)' },
    { value: 'WB', label: 'Máu toàn phần (WB)' },
]

const BLOOD_GROUP_OPTIONS = [
    { value: 'ALL', label: 'Tất cả nhóm máu' },
    ...BLOOD_GROUPS.map((g) => ({ value: g, label: g })),
]

const URGENCY_FILTER_OPTIONS = [
    { value: 'ALL', label: 'Tất cả mức khẩn' },
    { value: 'EMERGENCY', label: 'Cấp cứu (EMERGENCY)' },
    { value: 'URGENT', label: 'Khẩn (URGENT)' },
    { value: 'ROUTINE', label: 'Thường quy (ROUTINE)' },
]

const HANDED_OVER_STATUS_OPTIONS = [
    { value: 'ISSUED', label: 'Đã xuất kho bàn giao (ISSUED)' },
    { value: 'TRANSFUSED', label: 'Đã truyền cho người bệnh (TRANSFUSED)' },
]

const TIMEOUT_OPTIONS = [
    { value: 3, label: '3 phút' },
    { value: 4, label: '4 phút' },
    { value: 5, label: '5 phút (Mặc định)' },
]

interface StagedBagItem {
    bagId: number
    barcode: string
    bloodGroup: string
    componentTypeCode: string
    componentTypeName: string
    volumeMl: number
    requestId?: number
    requestCode?: string | null
}

function extractRequestCodeFromNotes(notes?: string | null): string | null {
    if (!notes) return null
    const match = notes.match(/\bYC-[A-Z0-9-]+\b/i)
    return match ? match[0].toUpperCase() : null
}

export default function HandoverScanPage() {
    const { message } = App.useApp()
    const navigate = useNavigate()
    const { displayName, roleLabel } = useCurrentUser()
    const { can } = usePermission()
    const canScanBag = can(PERMISSIONS.INVENTORY_VIEW)
    const canApprove = can(PERMISSIONS.BLOOD_REQUEST_APPROVE)

    const [looking, setLooking] = useState(false)
    const [errorMsg, setErrorMsg] = useState<string | null>(null)
    const [activeTab, setActiveTab] = useState<'reserved' | 'sessions' | 'handed-over'>('reserved')
    const [nowMs, setNowMs] = useState(() => Date.now())

    const [stagedBags, setStagedBags] = useState<StagedBagItem[]>([])
    const [timeoutMinutes, setTimeoutMinutes] = useState<number>(5)

    const [cancelTargetSession, setCancelTargetSession] = useState<HandoverSessionDto | null>(null)
    const [cancelReason, setCancelReason] = useState('')

    const [reservedSearch, setReservedSearch] = useState('')
    const [reservedUrgency, setReservedUrgency] = useState('ALL')
    const [selectedRequestFilterId, setSelectedRequestFilterId] = useState<number | 'ALL'>('ALL')
    const [mountedExpandedKeys, setMountedExpandedKeys] = useState<number[]>([])
    const [openExpandedKeys, setOpenExpandedKeys] = useState<number[]>([])
    const collapseTimersRef = useRef<Record<number, number>>({})

    useEffect(() => {
        const timers = collapseTimersRef.current
        return () => {
            Object.values(timers).forEach((id) => window.clearTimeout(id))
        }
    }, [])

    const toggleRequestRow = useCallback(
        (requestId: number) => {
            const existingTimer = collapseTimersRef.current[requestId]
            if (existingTimer) {
                window.clearTimeout(existingTimer)
                delete collapseTimersRef.current[requestId]
            }

            if (openExpandedKeys.includes(requestId)) {
                setOpenExpandedKeys((prev) => prev.filter((k) => k !== requestId))
                collapseTimersRef.current[requestId] = window.setTimeout(() => {
                    setMountedExpandedKeys((prev) => prev.filter((k) => k !== requestId))
                    delete collapseTimersRef.current[requestId]
                }, 280)
            } else {
                setMountedExpandedKeys((prev) => (prev.includes(requestId) ? prev : [...prev, requestId]))
                window.requestAnimationFrame(() => {
                    window.requestAnimationFrame(() => {
                        setOpenExpandedKeys((prev) => (prev.includes(requestId) ? prev : [...prev, requestId]))
                    })
                })
            }
        },
        [openExpandedKeys],
    )

    const [handedSearch, setHandedSearch] = useState('')
    const [handedStatus, setHandedStatus] = useState<'ISSUED' | 'TRANSFUSED'>('ISSUED')
    const [handedBloodGroup, setHandedBloodGroup] = useState('ALL')
    const [handedComponent, setHandedComponent] = useState('ALL')
    const [handedPage, setHandedPage] = useState(1)

    const [detailBag, setDetailBag] = useState<BloodBag | null>(null)
    const [detailScan, setDetailScan] = useState<BagScan | null>(null)
    const [detailLoading, setDetailLoading] = useState(false)
    const [detailError, setDetailError] = useState<string | null>(null)

    const createSessionMutation = useCreateHandoverSession()
    const cancelSessionMutation = useCancelHandoverSession()

    const reservedRequestsQuery = useReservedHandoverRequests({
        search: reservedSearch.trim() || undefined,
        urgencyLevel: reservedUrgency === 'ALL' ? undefined : reservedUrgency,
    })

    const allReservedRequests = useMemo(
        () => reservedRequestsQuery.data ?? [],
        [reservedRequestsQuery.data],
    )

    const requestFilterOptions = useMemo(
        () => [
            { value: 'ALL' as const, label: `Tất cả đơn yêu cầu (${allReservedRequests.length})` },
            ...allReservedRequests.map((r) => ({
                value: r.requestId,
                label: `${r.requestCode} - ${r.patientName} (${r.totalReservedBags} túi)`,
            })),
        ],
        [allReservedRequests],
    )

    const filteredRequests = useMemo(() => {
        if (selectedRequestFilterId === 'ALL') return allReservedRequests
        return allReservedRequests.filter((r) => r.requestId === selectedRequestFilterId)
    }, [allReservedRequests, selectedRequestFilterId])

    const reservedBagLookup = useMemo(() => {
        const map = new Map<string, { request: ReservedHandoverRequestDto; bag: ReservedHandoverBagDto }>()
        for (const req of allReservedRequests) {
            for (const bag of req.bags) {
                map.set(bag.barcode.trim().toLowerCase(), { request: req, bag })
            }
        }
        return map
    }, [allReservedRequests])

    const pendingSessionsQuery = usePendingHandoverSessions(undefined, {
        refetchInterval: activeTab === 'sessions' ? 5000 : 15000,
    })
    const pendingSessions = useMemo(
        () => pendingSessionsQuery.data ?? [],
        [pendingSessionsQuery.data],
    )

    useEffect(() => {
        if (pendingSessions.length === 0 && activeTab !== 'reserved') return
        const timer = window.setInterval(() => {
            setNowMs(Date.now())
        }, 1000)
        return () => window.clearInterval(timer)
    }, [pendingSessions.length, activeTab])

    const pendingBagSessionMap = useMemo(() => {
        const map = new Map<string, HandoverSessionDto>()
        for (const session of pendingSessions) {
            for (const item of session.items) {
                map.set(item.barcode.trim().toLowerCase(), session)
            }
        }
        return map
    }, [pendingSessions])

    const handedQuery = useBags(
        {
            status: handedStatus,
            search: handedSearch.trim() || undefined,
            componentType: handedComponent === 'ALL' ? undefined : handedComponent,
            ...bloodGroupParams(handedBloodGroup),
            pageNumber: handedPage,
            pageSize: 10,
        },
        canScanBag,
    )

    const { data: recentLogsData, refetch: refetchLogs, isFetching: isLogsFetching } = useRecentLogs(100, canScanBag)

    const { reservedLogMap, issuedLogMap } = useMemo(() => {
        const resMap = new Map<string, StatusLog>()
        const issMap = new Map<string, StatusLog>()
        for (const log of recentLogsData?.items ?? []) {
            const key = log.barcode.trim().toLowerCase()
            if (log.newStatus === 'RESERVED' && !resMap.has(key)) {
                resMap.set(key, log)
            }
            if (log.newStatus === 'ISSUED' && !issMap.has(key)) {
                issMap.set(key, log)
            }
        }
        return { reservedLogMap: resMap, issuedLogMap: issMap }
    }, [recentLogsData])

    useEffect(() => {
        if (!detailBag) return
        let cancelled = false

        stockApi
            .scan(detailBag.barcode)
            .then((scanData) => {
                if (!cancelled) {
                    setDetailScan(scanData)
                    setDetailError(null)
                }
            })
            .catch((err) => {
                if (!cancelled) setDetailError(parseApiError(err).message)
            })
            .finally(() => {
                if (!cancelled) setDetailLoading(false)
            })

        return () => {
            cancelled = true
        }
    }, [detailBag])

    const handleCopyBarcode = useCallback(
        async (barcode: string) => {
            try {
                await navigator.clipboard.writeText(barcode)
                message.success(`Đã sao chép mã ${barcode}`)
            } catch {
                message.info(`Mã túi máu: ${barcode}`)
            }
        },
        [message],
    )

    const stagedBarcodeSet = useMemo(
        () => new Set(stagedBags.map((b) => b.barcode.trim().toLowerCase())),
        [stagedBags],
    )

    const addBagToTray = useCallback(
        (bag: StagedBagItem) => {
            const normalized = bag.barcode.trim().toLowerCase()
            const existingSession = pendingBagSessionMap.get(normalized)
            if (existingSession) {
                message.info(
                    `Túi ${bag.barcode} đang nằm trong phiên chờ ${existingSession.sessionCode}. Đang mở chi tiết phiên.`,
                )
                navigate(`/coordination/handover/${encodeURIComponent(bag.barcode)}?sessionId=${existingSession.handoverSessionId}`)
                return
            }

            if (stagedBarcodeSet.has(normalized)) {
                message.info(`Túi máu ${bag.barcode} đã có trong khay bàn giao.`)
                return
            }

            const currentTrayRequestId = stagedBags[0]?.requestId
            const currentTrayRequestCode = stagedBags[0]?.requestCode
            if (
                currentTrayRequestId &&
                bag.requestId &&
                bag.requestId !== currentTrayRequestId
            ) {
                message.warning(
                    `Khay đang chọn túi của đơn ${currentTrayRequestCode || `#${currentTrayRequestId}`}. Chỉ có thể mở phiên cho các túi cùng một đơn yêu cầu.`,
                )
                return
            }

            setStagedBags((prev) => {
                if (prev.some((b) => b.barcode.trim().toLowerCase() === normalized)) return prev
                return [...prev, bag]
            })
            message.success(`Đã thêm ${bag.barcode} vào khay mở phiên bàn giao.`)
        },
        [message, navigate, pendingBagSessionMap, stagedBags, stagedBarcodeSet],
    )

    const addRequestBagsToTray = useCallback(
        (request: ReservedHandoverRequestDto) => {
            const eligible = request.bags.filter(
                (b) => !b.isExpired && !b.activeSessionId && !pendingBagSessionMap.has(b.barcode.trim().toLowerCase()),
            )
            if (eligible.length === 0) {
                message.warning(`Đơn ${request.requestCode} không còn túi hợp lệ chưa mở phiên.`)
                return
            }

            const mapped: StagedBagItem[] = eligible.map((b) => ({
                bagId: b.bagId,
                barcode: b.barcode,
                bloodGroup: b.bloodType,
                componentTypeCode: String(b.componentCode),
                componentTypeName: b.componentName,
                volumeMl: b.volumeMl,
                requestId: request.requestId,
                requestCode: request.requestCode,
            }))

            setStagedBags(mapped)
            message.success(`Đã đưa ${mapped.length} túi thuộc đơn ${request.requestCode} vào khay bàn giao.`)
        },
        [message, pendingBagSessionMap],
    )

    const removeBagFromTray = useCallback((barcode: string) => {
        const normalized = barcode.trim().toLowerCase()
        setStagedBags((prev) => prev.filter((b) => b.barcode.trim().toLowerCase() !== normalized))
    }, [])

    const handleScan = useCallback(
        async (rawBarcode: string) => {
            const barcode = rawBarcode.trim()
            if (!barcode) return

            if (!canScanBag) {
                const msg = 'Tài khoản của bạn chưa có quyền INVENTORY_VIEW để tra cứu barcode túi máu.'
                setErrorMsg(msg)
                message.error(msg)
                return
            }

            const normalized = barcode.toLowerCase()
            const existingSession = pendingBagSessionMap.get(normalized)
            if (existingSession) {
                navigate(`/coordination/handover/${encodeURIComponent(barcode)}?sessionId=${existingSession.handoverSessionId}`)
                return
            }

            const matchedEntry = reservedBagLookup.get(normalized)
            if (matchedEntry) {
                if (matchedEntry.bag.isExpired) {
                    const msg = `Túi máu ${matchedEntry.bag.barcode} đã hết hạn sử dụng, không thể xuất kho bàn giao.`
                    setErrorMsg(msg)
                    message.error(msg)
                    return
                }
                setErrorMsg(null)
                addBagToTray({
                    bagId: matchedEntry.bag.bagId,
                    barcode: matchedEntry.bag.barcode,
                    bloodGroup: matchedEntry.bag.bloodType,
                    componentTypeCode: String(matchedEntry.bag.componentCode),
                    componentTypeName: matchedEntry.bag.componentName,
                    volumeMl: matchedEntry.bag.volumeMl,
                    requestId: matchedEntry.request.requestId,
                    requestCode: matchedEntry.request.requestCode,
                })
                return
            }

            setLooking(true)
            setErrorMsg(null)

            try {
                const bag = await stockApi.scan(barcode)

                if (bag.isDiscarded || bag.currentStatus === 'DISCARDED') {
                    const msg = `Túi máu ${bag.barcode} đã ở trạng thái tiêu hủy (${bag.currentStatusLabel}), không thể bàn giao.`
                    setErrorMsg(msg)
                    message.error(msg)
                    return
                }

                if (bag.isLocked || bag.currentStatus === 'LOCKED') {
                    const msg = `Túi máu ${bag.barcode} đang bị phong tỏa (${bag.currentStatusLabel}), không đủ điều kiện xuất kho bàn giao.`
                    setErrorMsg(msg)
                    message.error(msg)
                    return
                }

                if (bag.isExpired || new Date(bag.expiredAt).getTime() <= Date.now()) {
                    const msg = `Túi máu ${bag.barcode} đã hết hạn sử dụng, không thể xuất kho bàn giao.`
                    setErrorMsg(msg)
                    message.error(msg)
                    return
                }

                if (bag.currentStatus === 'ISSUED') {
                    const msg = `Túi máu ${bag.barcode} đã được xuất kho bàn giao trước đó.`
                    setErrorMsg(msg)
                    message.warning(msg)
                    return
                }

                if (bag.currentStatus !== 'RESERVED') {
                    const msg = `Túi máu ${bag.barcode} đang ở trạng thái "${bag.currentStatusLabel}" (${bag.currentStatus}). Chỉ túi máu đã giữ chỗ cho phiếu yêu cầu (RESERVED) mới đủ điều kiện bàn giao.`
                    setErrorMsg(msg)
                    message.error(msg)
                    return
                }

                const reservedLog = reservedLogMap.get(bag.barcode.trim().toLowerCase())
                addBagToTray({
                    bagId: bag.bagId,
                    barcode: bag.barcode,
                    bloodGroup: bag.bloodGroup,
                    componentTypeCode: bag.componentTypeCode,
                    componentTypeName: bag.componentType,
                    volumeMl: bag.volumeMl,
                    requestCode: extractRequestCodeFromNotes(reservedLog?.notes),
                })
            } catch (err) {
                const msg = parseApiError(err).message
                setErrorMsg(msg)
                message.error(msg)
            } finally {
                setLooking(false)
            }
        },
        [addBagToTray, canScanBag, message, navigate, pendingBagSessionMap, reservedBagLookup, reservedLogMap],
    )

    const handleCreateBatchSession = async () => {
        if (!canApprove || stagedBags.length === 0 || createSessionMutation.isPending) return
        setErrorMsg(null)
        try {
            const session = await createSessionMutation.mutateAsync({
                barcodes: stagedBags.map((b) => b.barcode),
                requestId: stagedBags[0]?.requestId,
                timeoutMinutes,
            })
            message.success(
                `Đã mở phiên ${session.sessionCode} (${session.items.length} túi, hiệu lực ${timeoutMinutes} phút). Chờ Lâm sàng xác nhận tại quầy!`,
            )
            setStagedBags([])
            setActiveTab('sessions')
        } catch (err) {
            const msg = parseApiError(err).message
            setErrorMsg(msg)
            message.error(msg)
        }
    }

    const handleConfirmCancelSession = async () => {
        if (!cancelTargetSession || cancelSessionMutation.isPending) return
        try {
            const res = await cancelSessionMutation.mutateAsync({
                sessionId: cancelTargetSession.handoverSessionId,
                data: cancelReason.trim() ? { cancelReason: cancelReason.trim() } : undefined,
            })
            message.success(`Đã hủy sớm phiên ${res.sessionCode}. Toàn bộ túi máu giữ nguyên RESERVED.`)
            setCancelTargetSession(null)
            setCancelReason('')
        } catch (err) {
            message.error(parseApiError(err).message)
        }
    }

    const handleRefreshLists = () => {
        setNowMs(Date.now())
        void reservedRequestsQuery.refetch()
        void pendingSessionsQuery.refetch()
        void handedQuery.refetch()
        void refetchLogs()
    }

    const openDetailDrawer = useCallback((bag: BloodBag) => {
        setDetailBag(bag)
        setDetailScan(null)
        setDetailError(null)
        setDetailLoading(true)
    }, [])

    const requestColumns: ColumnsType<ReservedHandoverRequestDto> = useMemo(
        () => [
            {
                title: 'ĐƠN YÊU CẦU',
                key: 'requestCode',
                width: 210,
                render: (_, req) => (
                    <div style={{ display: 'grid', gap: 4 }}>
                        <div className="handover-code-cell">
                            <span className="handover-code-mono">{req.requestCode}</span>
                        </div>
                        <div>
                            <UrgencyTag level={req.urgencyLevel} />
                        </div>
                    </div>
                ),
            },
            {
                title: 'NGƯỜI BỆNH / KHOA',
                key: 'patient',
                render: (_, req) => (
                    <div style={{ display: 'grid', gap: 2 }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                            <strong style={{ color: 'var(--c-text)' }}>{req.patientName}</strong>
                            <span className="text-mono" style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)' }}>
                                ({req.patientCode})
                            </span>
                            <BloodGroupTag value={req.patientBloodType} size="sm" />
                        </div>
                        <div style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)' }}>
                            {req.departmentName || 'Khoa điều trị'} - BS {req.doctorName || '—'}
                        </div>
                    </div>
                ),
            },
            {
                title: 'TÚI ĐANG GIỮ CHỖ',
                key: 'bagSummary',
                width: 210,
                render: (_, req) => (
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                        <StatusPill tone={req.readyForSessionCount > 0 ? 'info' : 'neutral'} dot={false}>
                            {req.readyForSessionCount} sẵn sàng mở phiên
                        </StatusPill>
                        {req.inPendingSessionCount > 0 && (
                            <StatusPill tone="warning">
                                {req.inPendingSessionCount} đang chờ phiên
                            </StatusPill>
                        )}
                    </div>
                ),
            },
            {
                title: 'TRẠNG THÁI ĐƠN',
                dataIndex: 'requestStatus',
                key: 'requestStatus',
                align: 'center',
                width: 150,
                render: (status: ReservedHandoverRequestDto['requestStatus']) => (
                    <StatusPill tone={getStatusTone(status)}>
                        {getStatusMeta(status).label}
                    </StatusPill>
                ),
            },
            {
                title: 'THAO TÁC',
                key: 'actions',
                align: 'right',
                width: 210,
                render: (_, req) => {
                    const readyValidBags = req.bags.filter(
                        (b) => !b.isExpired && !b.activeSessionId && !pendingBagSessionMap.has(b.barcode.trim().toLowerCase()),
                    )
                    const firstSessionBag = req.bags.find((b) => b.activeSessionId)

                    return (
                        <div
                            style={{ display: 'inline-flex', gap: 6 }}
                            onClick={(e) => e.stopPropagation()}
                        >
                            {readyValidBags.length > 0 && (
                                <Button
                                    type="primary"
                                    size="small"
                                    icon={<PlusOutlined />}
                                    disabled={!canApprove}
                                    onClick={() => addRequestBagsToTray(req)}
                                >
                                    Chọn cả đơn ({readyValidBags.length})
                                </Button>
                            )}
                            {firstSessionBag?.activeSessionId && (
                                <Button
                                    size="small"
                                    icon={<EyeOutlined />}
                                    onClick={() =>
                                        navigate(
                                            `/coordination/handover/${encodeURIComponent(firstSessionBag.barcode)}?sessionId=${firstSessionBag.activeSessionId}`,
                                        )
                                    }
                                >
                                    Theo dõi phiên
                                </Button>
                            )}
                        </div>
                    )
                },
            },
        ],
        [addRequestBagsToTray, canApprove, navigate, pendingBagSessionMap],
    )

    const buildBagColumnsForRequest = useCallback(
        (req: ReservedHandoverRequestDto): ColumnsType<ReservedHandoverBagDto> => [
            {
                title: 'MÃ TÚI MÁU',
                dataIndex: 'barcode',
                key: 'barcode',
                width: 195,
                render: (barcode: string) => (
                    <div className="handover-code-cell" style={{ whiteSpace: 'nowrap' }}>
                        <span className="handover-code-mono" style={{ whiteSpace: 'nowrap' }}>{barcode}</span>
                        <Tooltip title="Sao chép mã túi máu">
                            <Button
                                type="text"
                                size="small"
                                icon={<CopyOutlined />}
                                onClick={() => void handleCopyBarcode(barcode)}
                            />
                        </Tooltip>
                    </div>
                ),
            },
            {
                title: 'NHÓM MÁU',
                dataIndex: 'bloodType',
                key: 'bloodType',
                align: 'center',
                width: 90,
                render: (group: string) => <BloodGroupTag value={group} size="sm" />,
            },
            {
                title: 'CHẾ PHẨM / THỂ TÍCH',
                key: 'component',
                width: 210,
                render: (_, bag) => (
                    <div>
                        <div style={{ fontWeight: 600, color: 'var(--c-text)' }}>{bag.componentName}</div>
                        <div style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)' }}>
                            {bag.componentCode} - {bag.volumeMl} ml
                        </div>
                    </div>
                ),
            },
            {
                title: 'VỊ TRÍ LƯU TRỮ',
                dataIndex: 'locationName',
                key: 'locationName',
                render: (loc?: string | null) =>
                    loc ? <StatusPill tone="info" dot={false}>{loc}</StatusPill> : <span style={{ color: 'var(--c-text-subtle)' }}>—</span>,
            },
            {
                title: 'HẠN SỬ DỤNG',
                dataIndex: 'expiredAt',
                key: 'expiredAt',
                width: 165,
                render: (expiredAt: string, bag) => {
                    const expired = bag.isExpired || new Date(expiredAt).getTime() <= nowMs
                    return (
                        <div style={{ whiteSpace: 'nowrap' }}>
                            <div style={{ color: expired ? 'var(--c-danger)' : 'var(--c-text)', fontWeight: expired ? 600 : 500 }}>
                                {formatDateTime(expiredAt)}
                            </div>
                            {expired && <StatusPill tone="danger">Đã hết hạn</StatusPill>}
                        </div>
                    )
                },
            },
            {
                title: 'PHIÊN CHỜ',
                key: 'sessionState',
                width: 165,
                render: (_, bag) => {
                    const activeSession = pendingBagSessionMap.get(bag.barcode.trim().toLowerCase())
                    const sessionCode = activeSession?.sessionCode || bag.activeSessionCode
                    return sessionCode ? (
                        <StatusPill tone="warning">Đang chờ {sessionCode}</StatusPill>
                    ) : (
                        <StatusPill tone="success" dot={false}>Sẵn sàng mở phiên</StatusPill>
                    )
                },
            },
            {
                title: 'THAO TÁC',
                key: 'actions',
                align: 'right',
                width: 128,
                render: (_, bag) => {
                    const key = bag.barcode.trim().toLowerCase()
                    const expired = bag.isExpired || new Date(bag.expiredAt).getTime() <= nowMs
                    const activeSession = pendingBagSessionMap.get(key)
                    const sessionId = activeSession?.handoverSessionId || bag.activeSessionId
                    const isStaged = stagedBarcodeSet.has(key)

                    if (sessionId) {
                        return (
                            <Button
                                size="small"
                                icon={<EyeOutlined />}
                                onClick={() =>
                                    navigate(`/coordination/handover/${encodeURIComponent(bag.barcode)}?sessionId=${sessionId}`)
                                }
                            >
                                Theo dõi
                            </Button>
                        )
                    }

                    return (
                        <div className="handover-bag-actions-stack">
                            <Button
                                size="small"
                                block
                                type={isStaged ? 'default' : 'primary'}
                                ghost={!isStaged}
                                icon={isStaged ? <CloseCircleOutlined /> : <PlusOutlined />}
                                disabled={expired || !canApprove}
                                onClick={() => {
                                    if (isStaged) {
                                        removeBagFromTray(bag.barcode)
                                    } else {
                                        addBagToTray({
                                            bagId: bag.bagId,
                                            barcode: bag.barcode,
                                            bloodGroup: bag.bloodType,
                                            componentTypeCode: String(bag.componentCode),
                                            componentTypeName: bag.componentName,
                                            volumeMl: bag.volumeMl,
                                            requestId: req.requestId,
                                            requestCode: req.requestCode,
                                        })
                                    }
                                }}
                            >
                                {isStaged ? 'Bỏ chọn' : 'Vào khay'}
                            </Button>
                            <Button
                                size="small"
                                block
                                icon={<CheckCircleOutlined />}
                                disabled={expired}
                                onClick={() => navigate(`/coordination/handover/${encodeURIComponent(bag.barcode)}`)}
                            >
                                Chi tiết
                            </Button>
                        </div>
                    )
                },
            },
        ],
        [
            addBagToTray,
            canApprove,
            handleCopyBarcode,
            navigate,
            nowMs,
            pendingBagSessionMap,
            removeBagFromTray,
            stagedBarcodeSet,
        ],
    )

    const handedColumns: ColumnsType<BloodBag> = useMemo(
        () => [
            {
                title: 'MÃ TÚI MÁU',
                dataIndex: 'barcode',
                key: 'barcode',
                render: (barcode: string) => (
                    <div className="handover-code-cell">
                        <span className="handover-code-mono">{barcode}</span>
                        <Tooltip title="Sao chép mã túi máu">
                            <Button
                                type="text"
                                size="small"
                                icon={<CopyOutlined />}
                                onClick={() => void handleCopyBarcode(barcode)}
                            />
                        </Tooltip>
                    </div>
                ),
            },
            {
                title: 'NHÓM MÁU',
                dataIndex: 'bloodGroup',
                key: 'bloodGroup',
                align: 'center',
                width: 100,
                render: (group: string) => <BloodGroupTag value={group} size="sm" />,
            },
            {
                title: 'CHẾ PHẨM / THỂ TÍCH',
                key: 'component',
                render: (_, row) => (
                    <div>
                        <div style={{ fontWeight: 600, color: 'var(--c-text)' }}>{row.componentTypeName}</div>
                        <div style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)' }}>
                            {row.componentTypeCode} - {row.volumeMl} ml
                        </div>
                    </div>
                ),
            },
            {
                title: 'PHIẾU YÊU CẦU / GHI CHÚ',
                key: 'requestPatient',
                render: (_, row) => {
                    const key = row.barcode.trim().toLowerCase()
                    const issuedLog = issuedLogMap.get(key)
                    const reservedLog = reservedLogMap.get(key)
                    const reqCode =
                        extractRequestCodeFromNotes(issuedLog?.notes) ||
                        extractRequestCodeFromNotes(reservedLog?.notes)

                    if (reqCode || issuedLog?.notes) {
                        return (
                            <div>
                                {reqCode && <StatusPill tone="info" dot={false}>{reqCode}</StatusPill>}
                                {issuedLog?.notes && (
                                    <div style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-secondary)', marginTop: 2 }}>
                                        {issuedLog.notes}
                                    </div>
                                )}
                            </div>
                        )
                    }

                    return <span style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-subtle)' }}>Bấm &quot;Chi tiết&quot; để tra cứu vòng đời</span>
                },
            },
            {
                title: 'NGƯỜI & GIỜ BÀN GIAO',
                key: 'handoverMeta',
                render: (_, row) => {
                    const key = row.barcode.trim().toLowerCase()
                    const issuedLog = issuedLogMap.get(key)
                    if (!issuedLog?.performedBy && !issuedLog?.timestamp) {
                        return <span style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-subtle)' }}>—</span>
                    }
                    return (
                        <div>
                            <div style={{ fontSize: 'var(--text-sm)', fontWeight: 600, color: 'var(--c-text)' }}>
                                {issuedLog.performedBy || 'KTV BBNK'}
                            </div>
                            <div style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)' }}>
                                {issuedLog.timestamp ? formatDateTime(issuedLog.timestamp) : '—'}
                            </div>
                        </div>
                    )
                },
            },
            {
                title: 'NGÀY LẤY / HẠN DÙNG',
                key: 'dates',
                render: (_, row) => (
                    <div style={{ fontSize: 'var(--text-xs)' }}>
                        <div style={{ color: 'var(--c-text-secondary)' }}>Lấy: {formatDateTime(row.collectedAt)}</div>
                        <div style={{ color: 'var(--c-text)', fontWeight: 500 }}>Hạn: {formatDateTime(row.expiredAt)}</div>
                    </div>
                ),
            },
            {
                title: 'TRẠNG THÁI',
                dataIndex: 'status',
                key: 'status',
                align: 'center',
                width: 130,
                render: (status: string) => <BagStatusTag status={status} />,
            },
            {
                title: 'TRA CỨU',
                key: 'actions',
                align: 'right',
                width: 120,
                render: (_, row) => (
                    <Button
                        size="small"
                        icon={<EyeOutlined />}
                        onClick={() => openDetailDrawer(row)}
                    >
                        Chi tiết
                    </Button>
                ),
            },
        ],
        [handleCopyBarcode, issuedLogMap, openDetailDrawer, reservedLogMap],
    )

    const totalReservedBagsCount = useMemo(
        () => filteredRequests.reduce((sum, r) => sum + r.totalReservedBags, 0),
        [filteredRequests],
    )
    const handedTotal = handedQuery.data?.totalCount ?? 0

    const detailKey = detailBag?.barcode.trim().toLowerCase() ?? ''
    const detailIssuedLog = detailKey ? issuedLogMap.get(detailKey) : undefined
    const detailReservedLog = detailKey ? reservedLogMap.get(detailKey) : undefined
    const detailReqCode =
        extractRequestCodeFromNotes(detailIssuedLog?.notes) ||
        extractRequestCodeFromNotes(detailReservedLog?.notes)

    return (
        <PageContainer className="handover-page">
            <PageHeader
                breadcrumb={[
                    { title: 'Điều phối Kho máu', path: '/coordination/queue' },
                    { title: 'Bàn giao cấp phát máu' },
                ]}
                title="Bàn giao cấp phát máu"
                subtitle="Quét hoặc mở từng đơn yêu cầu để chọn lô túi máu RESERVED và khởi tạo phiên bàn giao 2 pha."
                extra={
                    <StatusPill tone={pendingSessions.length > 0 ? 'warning' : totalReservedBagsCount > 0 ? 'info' : 'neutral'}>
                        {pendingSessions.length > 0
                            ? `${pendingSessions.length} phiên đang chờ xác nhận`
                            : totalReservedBagsCount > 0
                              ? `${filteredRequests.length} đơn (${totalReservedBagsCount} túi giữ chỗ)`
                              : 'Sẵn sàng xuất kho'}
                    </StatusPill>
                }
            />

            <section className="handover-card">
                <div className="handover-role-bar">
                    <div className="handover-role-info">
                        <div className="handover-role-icon">
                            <InboxOutlined />
                        </div>
                        <div>
                            <div className="handover-role-title">Kỹ thuật viên Khoa Truyền máu</div>
                            <div className="handover-role-sub">
                                {displayName} - {roleLabel || 'Kho Trung tâm'}
                            </div>
                        </div>
                    </div>
                    <span className="handover-role-badge">BBNK</span>
                </div>

                <div className="handover-scan-grid">
                    <BarcodeScanBox
                        title="Quét mã túi máu vào khay bàn giao (Pha 1)"
                        description="Quét nhiều túi máu RESERVED thuộc cùng một đơn yêu cầu vào khay bên dưới, hoặc mở rộng từng đơn yêu cầu để chọn túi."
                        placeholder="Nhập mã barcode túi máu..."
                        submitLabel="Thêm vào khay"
                        loading={looking}
                        errorMessage={errorMsg}
                        onClearError={() => setErrorMsg(null)}
                        onScan={handleScan}
                    />

                    <aside className="handover-info-aside">
                        <div className="handover-info-aside__title">
                            <span className="handover-info-aside__num">1</span>
                            <span>Quy trình bàn giao đồng thời 2 pha</span>
                        </div>
                        <p>
                            <strong>Pha 1 (BBNK):</strong> Chỉ mở phiên cho các túi <strong>RESERVED</strong> thuộc <strong>cùng một đơn yêu cầu</strong> (hiệu lực 3–5 phút). Túi vẫn nằm trong tủ bảo quản.
                        </p>
                        <p>
                            <strong>Pha 2 (Lâm sàng):</strong> Chỉ trao túi máu vào thùng bảo ôn sau khi nhân sự Lâm sàng quét xác nhận xong và phiên chuyển sang <strong>CONFIRMED</strong>.
                        </p>
                    </aside>
                </div>

                {stagedBags.length > 0 && (
                    <div className="handover-staging-bar">
                        <div className="handover-staging-bar__left">
                            <div className="handover-staging-bar__title">
                                Khay chuẩn bị mở phiên bàn giao ({stagedBags.length} túi
                                {stagedBags[0]?.requestCode ? ` - Đơn ${stagedBags[0].requestCode}` : ''})
                            </div>
                            <div className="handover-staging-bar__tags">
                                {stagedBags.map((item) => (
                                    <Tag
                                        key={item.barcode}
                                        closable
                                        color="blue"
                                        onClose={() => removeBagFromTray(item.barcode)}
                                    >
                                        <span className="text-mono">{item.barcode}</span> ({item.componentTypeCode} - {item.bloodGroup}
                                        {item.requestCode ? ` - ${item.requestCode}` : ''})
                                    </Tag>
                                ))}
                            </div>
                        </div>

                        <div className="handover-staging-bar__actions">
                            <Select
                                value={timeoutMinutes}
                                options={TIMEOUT_OPTIONS}
                                style={{ width: 155 }}
                                onChange={setTimeoutMinutes}
                            />
                            <Button onClick={() => setStagedBags([])} disabled={createSessionMutation.isPending}>
                                Xóa khay
                            </Button>
                            <Button
                                type="primary"
                                icon={<CheckCircleOutlined />}
                                loading={createSessionMutation.isPending}
                                disabled={!canApprove || createSessionMutation.isPending}
                                onClick={() => void handleCreateBatchSession()}
                            >
                                Mở phiên bàn giao ({stagedBags.length} túi)
                            </Button>
                        </div>
                    </div>
                )}
            </section>

            <section className="handover-card">
                <Tabs
                    activeKey={activeTab}
                    onChange={(k) => setActiveTab(k as 'reserved' | 'sessions' | 'handed-over')}
                    tabBarExtraContent={
                        <Button
                            icon={
                                <ReloadOutlined
                                    spin={
                                        reservedRequestsQuery.isFetching ||
                                        pendingSessionsQuery.isFetching ||
                                        handedQuery.isFetching ||
                                        isLogsFetching
                                    }
                                />
                            }
                            onClick={handleRefreshLists}
                        >
                            Làm mới danh sách
                        </Button>
                    }
                    items={[
                        {
                            key: 'reserved',
                            label: (
                                <span className="handover-tab-label">
                                    <span>Đơn yêu cầu giữ chỗ chờ bàn giao</span>
                                    <Badge count={totalReservedBagsCount} showZero color="var(--c-primary)" />
                                </span>
                            ),
                            children: (
                                <div style={{ display: 'grid', gap: 14 }}>
                                    <div className="handover-list-toolbar">
                                        <div className="handover-list-filters">
                                            <Input
                                                allowClear
                                                placeholder="Tìm mã đơn, tên/mã NB, khoa hoặc barcode..."
                                                prefix={<SearchOutlined style={{ color: 'var(--c-text-subtle)' }} />}
                                                value={reservedSearch}
                                                onChange={(e) => setReservedSearch(e.target.value)}
                                                style={{ width: 290 }}
                                            />
                                            <Select
                                                showSearch
                                                optionFilterProp="label"
                                                value={selectedRequestFilterId}
                                                options={requestFilterOptions}
                                                style={{ width: 280 }}
                                                onChange={(val) => setSelectedRequestFilterId(val)}
                                            />
                                            <Select
                                                value={reservedUrgency}
                                                options={URGENCY_FILTER_OPTIONS}
                                                style={{ width: 190 }}
                                                onChange={(val) => setReservedUrgency(val)}
                                            />
                                        </div>
                                        <span style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)' }}>
                                            Bấm vào dòng đơn yêu cầu để mở danh sách túi máu bên trong.
                                        </span>
                                    </div>

                                    {!canApprove ? (
                                        <Alert
                                            type="warning"
                                            showIcon
                                            message="Tài khoản thiếu quyền BLOOD_REQUEST_APPROVE để xem danh sách đơn yêu cầu bàn giao."
                                        />
                                    ) : (
                                        <Table<ReservedHandoverRequestDto>
                                            className="handover-request-expand-table"
                                            columns={requestColumns}
                                            dataSource={filteredRequests}
                                            rowKey="requestId"
                                            loading={reservedRequestsQuery.isLoading || reservedRequestsQuery.isFetching}
                                            onRow={(record) => ({
                                                onClick: () => toggleRequestRow(record.requestId),
                                                style: { cursor: 'pointer' },
                                            })}
                                            expandable={{
                                                expandedRowKeys: mountedExpandedKeys,
                                                expandIcon: ({ record }) => {
                                                    const isOpen = openExpandedKeys.includes(record.requestId)
                                                    return (
                                                        <Button
                                                            type="text"
                                                            size="small"
                                                            aria-label={isOpen ? 'Thu gọn danh sách túi máu' : 'Mở danh sách túi máu'}
                                                            icon={
                                                                <RightOutlined
                                                                    style={{
                                                                        fontSize: 11,
                                                                        transition: 'transform 0.24s ease',
                                                                        transform: isOpen ? 'rotate(90deg)' : 'rotate(0deg)',
                                                                    }}
                                                                />
                                                            }
                                                            onClick={(e) => {
                                                                e.stopPropagation()
                                                                toggleRequestRow(record.requestId)
                                                            }}
                                                        />
                                                    )
                                                },
                                                expandedRowRender: (record) => (
                                                    <div
                                                        className={`handover-expand-collapse ${
                                                            openExpandedKeys.includes(record.requestId) ? 'is-open' : ''
                                                        }`}
                                                    >
                                                        <div className="handover-expand-collapse__inner">
                                                            <div className="handover-expand-collapse__content">
                                                                <Table<ReservedHandoverBagDto>
                                                                    size="small"
                                                                    columns={buildBagColumnsForRequest(record)}
                                                                    dataSource={record.bags}
                                                                    rowKey="bagId"
                                                                    pagination={false}
                                                                />
                                                            </div>
                                                        </div>
                                                    </div>
                                                ),
                                            }}
                                            locale={{
                                                emptyText: (
                                                    <Empty
                                                        image={Empty.PRESENTED_IMAGE_SIMPLE}
                                                        description="Hiện không có đơn yêu cầu nào có túi máu đang giữ chỗ (RESERVED)."
                                                    />
                                                ),
                                            }}
                                            pagination={{
                                                pageSize: 10,
                                                showTotal: (total, range) => `${range[0]}–${range[1]} / ${total} đơn yêu cầu`,
                                            }}
                                        />
                                    )}
                                </div>
                            ),
                        },
                        {
                            key: 'sessions',
                            label: (
                                <span className="handover-tab-label">
                                    <span>Phiên bàn giao chờ xác nhận</span>
                                    <Badge count={pendingSessions.length} showZero color="var(--c-warning)" />
                                </span>
                            ),
                            children: (
                                <div style={{ display: 'grid', gap: 14 }}>
                                    <Alert
                                        type="warning"
                                        showIcon
                                        message="Quy tắc vận hành tại quầy (SOP)"
                                        description="Túi máu trong các phiên PENDING vẫn đang giữ trạng thái RESERVED. Chỉ trao túi vào thùng bảo ôn sau khi Lâm sàng quét xác nhận xong (phiên chuyển CONFIRMED)."
                                    />

                                    {pendingSessionsQuery.isLoading ? (
                                        <div style={{ padding: 36, textAlign: 'center' }}>
                                            <Spin tip="Đang tải danh sách phiên bàn giao chờ xác nhận..." />
                                        </div>
                                    ) : pendingSessions.length === 0 ? (
                                        <Empty
                                            image={Empty.PRESENTED_IMAGE_SIMPLE}
                                            description="Hiện không có phiên bàn giao nào đang chờ Lâm sàng xác nhận."
                                        />
                                    ) : (
                                        <div className="handover-session-list">
                                            {pendingSessions.map((session) => {
                                                const remaining = computeRemainingSeconds(session.expiresAt, nowMs)
                                                const isExpiring = remaining <= 60
                                                const firstBarcode = session.items[0]?.barcode ?? ''
                                                return (
                                                    <div key={session.handoverSessionId} className="handover-session-card">
                                                        <div className="handover-session-card__head">
                                                            <div>
                                                                <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                                                                    <span className="handover-code-mono">{session.sessionCode}</span>
                                                                    <StatusPill tone="info" dot={false}>{session.requestCode}</StatusPill>
                                                                    <UrgencyTag level={session.urgencyLevel} />
                                                                </div>
                                                                <div className="handover-session-card__meta">
                                                                    NB: <strong>{session.patientName}</strong> ({session.patientCode}) -{' '}
                                                                    {session.departmentName || 'Khoa điều trị'} - BS {session.doctorName || '—'}
                                                                </div>
                                                            </div>
                                                            <div className="handover-session-card__badges">
                                                                <BloodGroupTag value={session.patientBloodType} size="sm" />
                                                                <StatusPill tone={isExpiring ? 'danger' : 'warning'} pulse={remaining > 0}>
                                                                    <ClockCircleOutlined />{' '}
                                                                    {remaining > 0 ? `Còn ${formatCountdown(remaining)}` : 'Hết giờ - đang hoàn tác'}
                                                                </StatusPill>
                                                            </div>
                                                        </div>

                                                        <div className="handover-session-items">
                                                            {session.items.map((item) => (
                                                                <div key={item.allocationId} className="handover-session-chip">
                                                                    <span className="text-mono" style={{ fontWeight: 700, color: 'var(--c-text)' }}>
                                                                        {item.barcode}
                                                                    </span>
                                                                    <span>
                                                                        {item.componentCode} - {item.bloodType} - {item.volumeMl}ml
                                                                    </span>
                                                                    {item.locationName && (
                                                                        <span style={{ color: 'var(--c-primary)' }}>
                                                                            ({item.locationName})
                                                                        </span>
                                                                    )}
                                                                </div>
                                                            ))}
                                                        </div>

                                                        <div className="handover-session-card__foot">
                                                            <span style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)' }}>
                                                                Mở lúc {formatDateTime(session.createdAt)} bởi {session.initiatedByName || displayName} - Hết hạn {formatDateTime(session.expiresAt)}
                                                            </span>
                                                            <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                                                                {firstBarcode && (
                                                                    <Button
                                                                        size="small"
                                                                        icon={<EyeOutlined />}
                                                                        onClick={() =>
                                                                            navigate(
                                                                                `/coordination/handover/${encodeURIComponent(firstBarcode)}?sessionId=${session.handoverSessionId}`,
                                                                            )
                                                                        }
                                                                    >
                                                                        Theo dõi phiên
                                                                    </Button>
                                                                )}
                                                                <Button
                                                                    size="small"
                                                                    danger
                                                                    icon={<CloseCircleOutlined />}
                                                                    disabled={!canApprove}
                                                                    onClick={() => {
                                                                        setCancelTargetSession(session)
                                                                        setCancelReason('')
                                                                    }}
                                                                >
                                                                    Hủy phiên sớm
                                                                </Button>
                                                            </div>
                                                        </div>
                                                    </div>
                                                )
                                            })}
                                        </div>
                                    )}
                                </div>
                            ),
                        },
                        {
                            key: 'handed-over',
                            label: (
                                <span className="handover-tab-label">
                                    <span>Danh sách đã bàn giao</span>
                                    <Badge count={handedTotal} showZero color="var(--c-purple)" />
                                </span>
                            ),
                            children: (
                                <div style={{ display: 'grid', gap: 14 }}>
                                    <div className="handover-list-toolbar">
                                        <div className="handover-list-filters">
                                            <Input
                                                allowClear
                                                placeholder="Tìm theo mã barcode túi máu..."
                                                prefix={<SearchOutlined style={{ color: 'var(--c-text-subtle)' }} />}
                                                value={handedSearch}
                                                onChange={(e) => {
                                                    setHandedSearch(e.target.value)
                                                    setHandedPage(1)
                                                }}
                                                style={{ width: 240 }}
                                            />
                                            <Select
                                                value={handedStatus}
                                                options={HANDED_OVER_STATUS_OPTIONS}
                                                style={{ width: 270 }}
                                                onChange={(val) => {
                                                    setHandedStatus(val)
                                                    setHandedPage(1)
                                                }}
                                            />
                                            <Select
                                                value={handedBloodGroup}
                                                options={BLOOD_GROUP_OPTIONS}
                                                style={{ width: 165 }}
                                                onChange={(val) => {
                                                    setHandedBloodGroup(val)
                                                    setHandedPage(1)
                                                }}
                                            />
                                            <Select
                                                value={handedComponent}
                                                options={COMPONENT_OPTIONS}
                                                style={{ width: 200 }}
                                                onChange={(val) => {
                                                    setHandedComponent(val)
                                                    setHandedPage(1)
                                                }}
                                            />
                                        </div>
                                    </div>

                                    {!canScanBag ? (
                                        <Alert
                                            type="warning"
                                            showIcon
                                            message="Tài khoản thiếu quyền INVENTORY_VIEW để tra cứu danh sách túi máu đã bàn giao."
                                        />
                                    ) : (
                                        <Table
                                            columns={handedColumns}
                                            dataSource={handedQuery.data?.items ?? []}
                                            rowKey="bagId"
                                            loading={handedQuery.isLoading || handedQuery.isFetching}
                                            locale={{
                                                emptyText: (
                                                    <Empty
                                                        image={Empty.PRESENTED_IMAGE_SIMPLE}
                                                        description="Chưa có túi máu nào phù hợp với bộ lọc đã bàn giao."
                                                    />
                                                ),
                                            }}
                                            pagination={{
                                                current: handedPage,
                                                pageSize: 10,
                                                total: handedTotal,
                                                onChange: (p) => setHandedPage(p),
                                                showTotal: (total, range) => `${range[0]}–${range[1]} / ${total} túi đã bàn giao`,
                                            }}
                                        />
                                    )}
                                </div>
                            ),
                        },
                    ]}
                />
            </section>

            <Modal
                title={
                    cancelTargetSession
                        ? `Hủy sớm phiên bàn giao ${cancelTargetSession.sessionCode}`
                        : 'Hủy sớm phiên bàn giao'
                }
                open={Boolean(cancelTargetSession)}
                onCancel={() => {
                    if (!cancelSessionMutation.isPending) {
                        setCancelTargetSession(null)
                        setCancelReason('')
                    }
                }}
                onOk={() => void handleConfirmCancelSession()}
                okText="Xác nhận hủy phiên"
                cancelText="Đóng"
                okButtonProps={{ danger: true, loading: cancelSessionMutation.isPending }}
            >
                <div style={{ display: 'grid', gap: 12 }}>
                    <Alert
                        type="info"
                        showIcon
                        message="Túi máu giữ nguyên RESERVED"
                        description="Khi hủy sớm phiên bàn giao, toàn bộ túi máu trong lô vẫn giữ nguyên giữ chỗ cho phiếu yêu cầu để mở lại phiên sau."
                    />
                    <Input.TextArea
                        rows={3}
                        maxLength={255}
                        showCount
                        placeholder="Nhập lý do hủy phiên tại quầy (không bắt buộc)..."
                        value={cancelReason}
                        onChange={(e) => setCancelReason(e.target.value)}
                    />
                </div>
            </Modal>

            <Drawer
                title={detailBag ? `Chi tiết bàn giao túi máu ${detailBag.barcode}` : 'Chi tiết bàn giao'}
                open={Boolean(detailBag)}
                width={620}
                onClose={() => {
                    setDetailBag(null)
                    setDetailScan(null)
                    setDetailError(null)
                }}
            >
                {detailBag && (
                    <div style={{ display: 'grid', gap: 18 }}>
                        <div className="handover-bag-banner">
                            <div className="handover-bag-banner__left">
                                <BloodGroupTag value={detailBag.bloodGroup} size="lg" />
                                <div>
                                    <div className="handover-bag-banner__label">Mã túi máu</div>
                                    <div className="handover-bag-banner__code">{detailBag.barcode}</div>
                                    <div className="handover-bag-banner__meta">
                                        {detailBag.componentTypeName} ({detailBag.componentTypeCode}) - {detailBag.volumeMl} ml
                                    </div>
                                </div>
                            </div>
                            <BagStatusTag status={detailScan?.currentStatus || detailBag.status} label={detailScan?.currentStatusLabel} />
                        </div>

                        <div style={{ width: 200 }}>
                            <BarcodeSvg value={detailBag.barcode} height={44} />
                        </div>

                        <div className="handover-detail-box" style={{ padding: 16 }}>
                            <div className="handover-detail-box__eyebrow">Thông tin túi máu & bàn giao</div>
                            <div className="handover-kv">
                                <span>Ngày lấy máu</span>
                                <strong>{formatDateTime(detailScan?.collectedAt || detailBag.collectedAt)}</strong>
                            </div>
                            <div className="handover-kv">
                                <span>Hạn sử dụng</span>
                                <strong>{formatDateTime(detailScan?.expiredAt || detailBag.expiredAt)}</strong>
                            </div>
                            <div className="handover-kv">
                                <span>Phiếu yêu cầu liên quan</span>
                                <strong>
                                    {detailReqCode ? <StatusPill tone="info" dot={false}>{detailReqCode}</StatusPill> : '—'}
                                </strong>
                            </div>
                            <div className="handover-kv">
                                <span>Người xuất kho bàn giao</span>
                                <strong>{detailIssuedLog?.performedBy || '—'}</strong>
                            </div>
                            <div className="handover-kv">
                                <span>Thời gian xuất kho</span>
                                <strong>{detailIssuedLog?.timestamp ? formatDateTime(detailIssuedLog.timestamp) : '—'}</strong>
                            </div>
                            {detailReservedLog && (
                                <div className="handover-kv" style={{ borderBottom: 'none' }}>
                                    <span>Giữ chỗ trước đó</span>
                                    <strong>
                                        {detailReservedLog.performedBy} - {formatDateTime(detailReservedLog.timestamp)}
                                    </strong>
                                </div>
                            )}
                        </div>

                        <div className="handover-detail-box" style={{ padding: 16 }}>
                            <div className="handover-detail-box__eyebrow">Vòng đời túi máu</div>
                            {detailLoading ? (
                                <div style={{ padding: 24, textAlign: 'center' }}>
                                    <Spin tip="Đang tải vòng đời túi máu..." />
                                </div>
                            ) : detailError ? (
                                <Alert type="error" showIcon message={detailError} />
                            ) : detailScan ? (
                                <ul className="handover-timeline-list">
                                    {detailScan.timeline.map((step) => (
                                        <li
                                            key={step.step}
                                            className={`handover-timeline-item ${step.completed ? 'is-done' : ''}`}
                                        >
                                            <div className="handover-timeline-item__left">
                                                <span className="handover-timeline-step">
                                                    {step.completed ? '✓' : step.step}
                                                </span>
                                                <span>{step.name}</span>
                                            </div>
                                            <span
                                                style={{
                                                    fontSize: 'var(--text-xs)',
                                                    color: step.completed ? 'var(--c-success)' : 'var(--c-text-subtle)',
                                                }}
                                            >
                                                {step.completed && step.timestamp
                                                    ? formatDateTime(step.timestamp)
                                                    : 'Chưa hoàn tất'}
                                            </span>
                                        </li>
                                    ))}
                                </ul>
                            ) : null}
                        </div>
                    </div>
                )}
            </Drawer>
        </PageContainer>
    )
}
