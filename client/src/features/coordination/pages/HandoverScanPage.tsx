import { useCallback, useEffect, useMemo, useState } from 'react'
import {
    Alert,
    App,
    Badge,
    Button,
    Drawer,
    Empty,
    Input,
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
    CopyOutlined,
    EyeOutlined,
    InboxOutlined,
    ReloadOutlined,
    SearchOutlined,
} from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import { useCurrentUser } from '@/features/auth/useCurrentUser'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { stockApi } from '@/features/inventory/stockApi'
import { useBags, useRecentLogs } from '@/features/inventory/stockHooks'
import { BLOOD_GROUPS, bloodGroupParams } from '@/features/inventory/stockFormat'
import { BagStatusTag, BloodGroupTag } from '@/features/inventory/components/StockBadges'
import type { BagScan, BloodBag, StatusLog } from '@/features/inventory/stockTypes'
import { getStatusMeta, getUrgencyMeta } from '@/features/clinical/format'
import { parseApiError } from '@/services/apiError'
import { formatDateTime } from '@/shared/formatters/date'
import BarcodeScanBox from '@/shared/components/BarcodeScanBox'
import BarcodeSvg from '@/shared/components/BarcodeSvg'
import { useDispatchedHandoverMap } from '../hooks'
import type { DispatchBloodBagResponse } from '../types'
import './HandoverPages.css'

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

const HANDED_OVER_STATUS_OPTIONS = [
    { value: 'ISSUED', label: 'Đã xuất kho bàn giao (ISSUED)' },
    { value: 'TRANSFUSED', label: 'Đã truyền cho người bệnh (TRANSFUSED)' },
]

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

    const [looking, setLooking] = useState(false)
    const [verifyingBarcode, setVerifyingBarcode] = useState<string | null>(null)
    const [errorMsg, setErrorMsg] = useState<string | null>(null)
    const [activeTab, setActiveTab] = useState<'reserved' | 'handed-over'>('reserved')
    const [nowMs, setNowMs] = useState(() => Date.now())

    // Bộ lọc danh sách túi đang giữ chỗ (RESERVED)
    const [reservedSearch, setReservedSearch] = useState('')
    const [reservedBloodGroup, setReservedBloodGroup] = useState('ALL')
    const [reservedComponent, setReservedComponent] = useState('ALL')
    const [reservedPage, setReservedPage] = useState(1)

    // Bộ lọc danh sách đã bàn giao (ISSUED / TRANSFUSED)
    const [handedSearch, setHandedSearch] = useState('')
    const [handedStatus, setHandedStatus] = useState<'ISSUED' | 'TRANSFUSED'>('ISSUED')
    const [handedBloodGroup, setHandedBloodGroup] = useState('ALL')
    const [handedComponent, setHandedComponent] = useState('ALL')
    const [handedPage, setHandedPage] = useState(1)

    // Drawer tra cứu chi tiết túi máu đã bàn giao
    const [detailBag, setDetailBag] = useState<BloodBag | null>(null)
    const [detailScan, setDetailScan] = useState<BagScan | null>(null)
    const [detailLoading, setDetailLoading] = useState(false)
    const [detailError, setDetailError] = useState<string | null>(null)

    const { data: dispatchedMap = {} } = useDispatchedHandoverMap()

    const reservedQuery = useBags(
        {
            status: 'RESERVED',
            search: reservedSearch.trim() || undefined,
            componentType: reservedComponent === 'ALL' ? undefined : reservedComponent,
            ...bloodGroupParams(reservedBloodGroup),
            pageNumber: reservedPage,
            pageSize: 10,
        },
        canScanBag,
    )

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

    // Lập map nhật ký giữ chỗ (RESERVED) & xuất kho bàn giao (ISSUED) gần nhất theo barcode
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

    // Tải chi tiết vòng đời túi máu khi mở Drawer tra cứu
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

            setLooking(true)
            setVerifyingBarcode(barcode)
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

                navigate(`/coordination/handover/${encodeURIComponent(bag.barcode)}`, {
                    state: { scannedBag: bag },
                })
            } catch (err) {
                const msg = parseApiError(err).message
                setErrorMsg(msg)
                message.error(msg)
            } finally {
                setLooking(false)
                setVerifyingBarcode(null)
            }
        },
        [canScanBag, message, navigate],
    )

    const handleRefreshLists = () => {
        setNowMs(Date.now())
        void reservedQuery.refetch()
        void handedQuery.refetch()
        void refetchLogs()
    }

    const openDetailDrawer = useCallback((bag: BloodBag) => {
        setDetailBag(bag)
        setDetailScan(null)
        setDetailError(null)
        setDetailLoading(true)
    }, [])

    const reservedColumns: ColumnsType<BloodBag> = useMemo(
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
                width: 105,
                render: (group: string) => <BloodGroupTag value={group} />,
            },
            {
                title: 'CHẾ PHẨM / THỂ TÍCH',
                key: 'component',
                render: (_, row) => (
                    <div>
                        <div style={{ fontWeight: 600, color: '#0f172a' }}>{row.componentTypeName}</div>
                        <div style={{ fontSize: 12, color: '#64748b' }}>
                            {row.componentTypeCode} · {row.volumeMl} ml
                        </div>
                    </div>
                ),
            },
            {
                title: 'VỊ TRÍ LƯU TRỮ',
                dataIndex: 'storageLocationName',
                key: 'location',
                render: (loc?: string | null) =>
                    loc ? <Tag color="blue">{loc}</Tag> : <span style={{ color: '#94a3b8' }}>—</span>,
            },
            {
                title: 'THÔNG TIN GIỮ CHỖ',
                key: 'reservationInfo',
                render: (_, row) => {
                    const log = reservedLogMap.get(row.barcode.trim().toLowerCase())
                    const reqCode = extractRequestCodeFromNotes(log?.notes)
                    if (!log) {
                        return <span style={{ fontSize: 12, color: '#94a3b8' }}>Đang giữ chỗ cho phiếu yêu cầu</span>
                    }
                    return (
                        <div>
                            <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}>
                                {reqCode && <Tag color="geekblue" style={{ marginInlineEnd: 0 }}>{reqCode}</Tag>}
                                <span style={{ fontSize: 12, fontWeight: 600, color: '#0f172a' }}>
                                    {log.performedBy || 'BBNK'}
                                </span>
                            </div>
                            <div style={{ fontSize: 12, color: '#64748b' }}>
                                Giữ chỗ: {formatDateTime(log.timestamp)}
                            </div>
                        </div>
                    )
                },
            },
            {
                title: 'HẠN SỬ DỤNG',
                dataIndex: 'expiredAt',
                key: 'expiredAt',
                render: (expiredAt: string) => {
                    const expired = new Date(expiredAt).getTime() <= nowMs
                    return (
                        <div>
                            <div style={{ color: expired ? '#dc2626' : '#0f172a', fontWeight: expired ? 600 : 500 }}>
                                {formatDateTime(expiredAt)}
                            </div>
                            {expired && <Tag color="error">Đã hết hạn</Tag>}
                        </div>
                    )
                },
            },
            {
                title: 'THAO TÁC',
                key: 'actions',
                align: 'right',
                width: 170,
                render: (_, row) => {
                    const expired = new Date(row.expiredAt).getTime() <= nowMs
                    const isRowVerifying = looking && verifyingBarcode === row.barcode
                    return (
                        <Button
                            type="primary"
                            size="small"
                            icon={<CheckCircleOutlined />}
                            className={!expired ? 'handover-btn-primary' : undefined}
                            danger={expired}
                            loading={isRowVerifying}
                            disabled={looking && !isRowVerifying}
                            onClick={() => void handleScan(row.barcode)}
                        >
                            {expired ? 'Kiểm tra' : 'Chọn bàn giao'}
                        </Button>
                    )
                },
            },
        ],
        [handleCopyBarcode, handleScan, looking, nowMs, reservedLogMap, verifyingBarcode],
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
                render: (group: string) => <BloodGroupTag value={group} />,
            },
            {
                title: 'CHẾ PHẨM / THỂ TÍCH',
                key: 'component',
                render: (_, row) => (
                    <div>
                        <div style={{ fontWeight: 600, color: '#0f172a' }}>{row.componentTypeName}</div>
                        <div style={{ fontSize: 12, color: '#64748b' }}>
                            {row.componentTypeCode} · {row.volumeMl} ml
                        </div>
                    </div>
                ),
            },
            {
                title: 'PHIẾU YÊU CẦU / NGƯỜI BỆNH',
                key: 'requestPatient',
                render: (_, row) => {
                    const key = row.barcode.trim().toLowerCase()
                    const dispatchInfo: DispatchBloodBagResponse | undefined = dispatchedMap[key]
                    const issuedLog = issuedLogMap.get(key)
                    const reservedLog = reservedLogMap.get(key)
                    const reqCode =
                        dispatchInfo?.requestCode ||
                        extractRequestCodeFromNotes(issuedLog?.notes) ||
                        extractRequestCodeFromNotes(reservedLog?.notes)

                    if (dispatchInfo) {
                        const urgency = getUrgencyMeta(dispatchInfo.urgencyLevel)
                        return (
                            <div>
                                <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}>
                                    <Tag color="geekblue" style={{ marginInlineEnd: 0 }}>{dispatchInfo.requestCode}</Tag>
                                    <Tag color={urgency.color} style={{ marginInlineEnd: 0 }}>{urgency.label}</Tag>
                                </div>
                                <div style={{ fontSize: 13, fontWeight: 600, color: '#0f172a', marginTop: 2 }}>
                                    {dispatchInfo.patientName} ({dispatchInfo.patientCode})
                                </div>
                                <div style={{ fontSize: 12, color: '#64748b' }}>
                                    {dispatchInfo.departmentName || 'Khoa điều trị'} · {dispatchInfo.doctorName || 'Bác sĩ'}
                                </div>
                            </div>
                        )
                    }

                    if (reqCode || issuedLog?.notes) {
                        return (
                            <div>
                                {reqCode && <Tag color="geekblue">{reqCode}</Tag>}
                                {issuedLog?.notes && (
                                    <div style={{ fontSize: 12, color: '#475569', marginTop: 2 }}>{issuedLog.notes}</div>
                                )}
                            </div>
                        )
                    }

                    return <span style={{ fontSize: 12, color: '#94a3b8' }}>Bấm &quot;Chi tiết&quot; để tra cứu vòng đời</span>
                },
            },
            {
                title: 'NGƯỜI & GIỜ BÀN GIAO',
                key: 'handoverMeta',
                render: (_, row) => {
                    const key = row.barcode.trim().toLowerCase()
                    const dispatchInfo = dispatchedMap[key]
                    const issuedLog = issuedLogMap.get(key)
                    const actor = dispatchInfo?.issuedByName || issuedLog?.performedBy
                    const issuedAt = dispatchInfo?.issuedAt || issuedLog?.timestamp
                    if (!actor && !issuedAt) {
                        return <span style={{ fontSize: 12, color: '#94a3b8' }}>—</span>
                    }
                    return (
                        <div>
                            <div style={{ fontSize: 13, fontWeight: 600, color: '#0f172a' }}>{actor || 'KTV BBNK'}</div>
                            <div style={{ fontSize: 12, color: '#64748b' }}>
                                {issuedAt ? formatDateTime(issuedAt) : '—'}
                            </div>
                        </div>
                    )
                },
            },
            {
                title: 'NGÀY LẤY / HẠN DÙNG',
                key: 'dates',
                render: (_, row) => (
                    <div style={{ fontSize: 12 }}>
                        <div style={{ color: '#475569' }}>Lấy: {formatDateTime(row.collectedAt)}</div>
                        <div style={{ color: '#0f172a', fontWeight: 500 }}>Hạn: {formatDateTime(row.expiredAt)}</div>
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
        [dispatchedMap, handleCopyBarcode, issuedLogMap, openDetailDrawer, reservedLogMap],
    )

    const reservedTotal = reservedQuery.data?.totalCount ?? 0
    const handedTotal = handedQuery.data?.totalCount ?? 0

    const detailKey = detailBag?.barcode.trim().toLowerCase() ?? ''
    const detailDispatched = detailKey ? dispatchedMap[detailKey] : undefined
    const detailIssuedLog = detailKey ? issuedLogMap.get(detailKey) : undefined
    const detailReservedLog = detailKey ? reservedLogMap.get(detailKey) : undefined
    const detailReqCode =
        detailDispatched?.requestCode ||
        extractRequestCodeFromNotes(detailIssuedLog?.notes) ||
        extractRequestCodeFromNotes(detailReservedLog?.notes)

    return (
        <div className="handover-page">
            <div className="handover-head">
                <div>
                    <div className="handover-breadcrumb">Điều phối Kho máu / W4.3 · Bàn giao cấp phát máu</div>
                    <h1 className="handover-title">Bàn giao cấp phát máu</h1>
                    <p className="handover-subtitle">
                        Quét túi xuất kho, chọn nhanh túi máu đang giữ chỗ hoặc tra cứu danh sách túi máu đã bàn giao.
                    </p>
                </div>
                <span className="handover-status-pill">
                    <i /> {reservedTotal > 0 ? `${reservedTotal} túi đang giữ chỗ` : 'Sẵn sàng xuất kho'}
                </span>
            </div>

            <section className="handover-card">
                <div className="handover-role-bar">
                    <div className="handover-role-info">
                        <div className="handover-role-icon">
                            <InboxOutlined />
                        </div>
                        <div>
                            <div className="handover-role-title">Kỹ thuật viên Khoa Truyền máu</div>
                            <div className="handover-role-sub">
                                {displayName} · {roleLabel || 'Kho Trung tâm'}
                            </div>
                        </div>
                    </div>
                    <span className="handover-role-badge">BBNK</span>
                </div>

                <div className="handover-scan-grid">
                    <BarcodeScanBox
                        title="Quét mã túi máu xuất kho"
                        description="Quét camera, tải ảnh mã QR/Barcode, nhập mã thủ công hoặc chọn túi đang giữ chỗ ở danh sách bên dưới."
                        placeholder="Nhập mã barcode túi máu..."
                        submitLabel="Quét & đối chiếu"
                        loading={looking}
                        errorMessage={errorMsg}
                        onClearError={() => setErrorMsg(null)}
                        onScan={handleScan}
                    />

                    <aside className="handover-info-aside">
                        <div className="handover-info-aside__title">
                            <span className="handover-info-aside__num">1</span>
                            <span>Quy trình bàn giao 2 bước</span>
                        </div>
                        <p>
                            Hệ thống sẽ xác minh túi máu đang ở trạng thái <strong>Đã giữ chỗ (RESERVED)</strong> và còn hạn sử dụng trước khi chuyển sang màn hình thông tin xác nhận xuất kho.
                        </p>
                        <p>
                            Nếu không quét QR trực tiếp được, bạn có thể dùng nút <strong>Quét từ ảnh</strong> hoặc bấm <strong>Chọn bàn giao</strong> ngay trong bảng túi đang giữ chỗ bên dưới.
                        </p>
                    </aside>
                </div>
            </section>

            <section className="handover-card">
                <Tabs
                    activeKey={activeTab}
                    onChange={(k) => setActiveTab(k as 'reserved' | 'handed-over')}
                    tabBarExtraContent={
                        <Button
                            icon={<ReloadOutlined spin={reservedQuery.isFetching || handedQuery.isFetching || isLogsFetching} />}
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
                                    <span>Túi máu đang giữ chỗ chờ bàn giao</span>
                                    <Badge count={reservedTotal} showZero color="#1a7534" />
                                </span>
                            ),
                            children: (
                                <div style={{ display: 'grid', gap: 14 }}>
                                    <div className="handover-list-toolbar">
                                        <div className="handover-list-filters">
                                            <Input
                                                allowClear
                                                placeholder="Tìm theo mã barcode túi máu..."
                                                prefix={<SearchOutlined style={{ color: '#94a3b8' }} />}
                                                value={reservedSearch}
                                                onChange={(e) => {
                                                    setReservedSearch(e.target.value)
                                                    setReservedPage(1)
                                                }}
                                                style={{ width: 250 }}
                                            />
                                            <Select
                                                value={reservedBloodGroup}
                                                options={BLOOD_GROUP_OPTIONS}
                                                style={{ width: 165 }}
                                                onChange={(val) => {
                                                    setReservedBloodGroup(val)
                                                    setReservedPage(1)
                                                }}
                                            />
                                            <Select
                                                value={reservedComponent}
                                                options={COMPONENT_OPTIONS}
                                                style={{ width: 210 }}
                                                onChange={(val) => {
                                                    setReservedComponent(val)
                                                    setReservedPage(1)
                                                }}
                                            />
                                        </div>
                                        <span style={{ fontSize: 12, color: '#64748b' }}>
                                            Bấm &quot;Chọn bàn giao&quot; sẽ xác minh túi qua API trước khi sang màn hình xác nhận.
                                        </span>
                                    </div>

                                    {!canScanBag ? (
                                        <Alert
                                            type="warning"
                                            showIcon
                                            message="Tài khoản thiếu quyền INVENTORY_VIEW để xem danh sách túi máu trong kho."
                                        />
                                    ) : (
                                        <Table
                                            columns={reservedColumns}
                                            dataSource={reservedQuery.data?.items ?? []}
                                            rowKey="bagId"
                                            loading={reservedQuery.isLoading || reservedQuery.isFetching}
                                            locale={{
                                                emptyText: (
                                                    <Empty
                                                        image={Empty.PRESENTED_IMAGE_SIMPLE}
                                                        description="Hiện không có túi máu nào đang ở trạng thái giữ chỗ (RESERVED)."
                                                    />
                                                ),
                                            }}
                                            pagination={{
                                                current: reservedPage,
                                                pageSize: 10,
                                                total: reservedTotal,
                                                onChange: (p) => setReservedPage(p),
                                                showTotal: (total, range) => `${range[0]}–${range[1]} / ${total} túi đang giữ chỗ`,
                                            }}
                                        />
                                    )}
                                </div>
                            ),
                        },
                        {
                            key: 'handed-over',
                            label: (
                                <span className="handover-tab-label">
                                    <span>Danh sách đã bàn giao</span>
                                    <Badge count={handedTotal} showZero color="#7c3aed" />
                                </span>
                            ),
                            children: (
                                <div style={{ display: 'grid', gap: 14 }}>
                                    <div className="handover-list-toolbar">
                                        <div className="handover-list-filters">
                                            <Input
                                                allowClear
                                                placeholder="Tìm theo mã barcode túi máu..."
                                                prefix={<SearchOutlined style={{ color: '#94a3b8' }} />}
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

            {/* Drawer tra cứu chi tiết túi máu đã bàn giao */}
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
                                <div className="handover-blood-badge">{detailBag.bloodGroup}</div>
                                <div>
                                    <div className="handover-bag-banner__label">Mã túi máu</div>
                                    <div className="handover-bag-banner__code">{detailBag.barcode}</div>
                                    <div className="handover-bag-banner__meta">
                                        {detailBag.componentTypeName} ({detailBag.componentTypeCode}) · {detailBag.volumeMl} ml
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
                                    {detailReqCode ? <Tag color="geekblue">{detailReqCode}</Tag> : '—'}
                                </strong>
                            </div>
                            {detailDispatched && (
                                <>
                                    <div className="handover-kv">
                                        <span>Người bệnh</span>
                                        <strong>{detailDispatched.patientName} ({detailDispatched.patientCode})</strong>
                                    </div>
                                    <div className="handover-kv">
                                        <span>Khoa / Bác sĩ</span>
                                        <strong>
                                            {detailDispatched.departmentName || '—'} · {detailDispatched.doctorName || '—'}
                                        </strong>
                                    </div>
                                    <div className="handover-kv">
                                        <span>Trạng thái phiếu yêu cầu</span>
                                        <strong>
                                            <Tag color={getStatusMeta(detailDispatched.requestStatus).color}>
                                                {getStatusMeta(detailDispatched.requestStatus).label}
                                            </Tag>
                                        </strong>
                                    </div>
                                </>
                            )}
                            <div className="handover-kv">
                                <span>Người xuất kho bàn giao</span>
                                <strong>{detailDispatched?.issuedByName || detailIssuedLog?.performedBy || '—'}</strong>
                            </div>
                            <div className="handover-kv">
                                <span>Thời gian xuất kho</span>
                                <strong>
                                    {detailDispatched?.issuedAt
                                        ? formatDateTime(detailDispatched.issuedAt)
                                        : detailIssuedLog?.timestamp
                                          ? formatDateTime(detailIssuedLog.timestamp)
                                          : '—'}
                                </strong>
                            </div>
                            {detailReservedLog && (
                                <div className="handover-kv" style={{ borderBottom: 'none' }}>
                                    <span>Giữ chỗ trước đó</span>
                                    <strong>
                                        {detailReservedLog.performedBy} · {formatDateTime(detailReservedLog.timestamp)}
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
                                            <span style={{ fontSize: 12, color: step.completed ? '#15803d' : '#94a3b8' }}>
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
        </div>
    )
}
