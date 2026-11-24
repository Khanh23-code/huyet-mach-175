import { useEffect, useMemo, useState } from 'react'
import { Alert, App, Button, Input, Modal, Result, Select, Spin } from 'antd'
import {
    ArrowLeftOutlined,
    CheckCircleOutlined,
    ClockCircleOutlined,
    CloseCircleOutlined,
    InboxOutlined,
} from '@ant-design/icons'
import { useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useCurrentUser } from '@/features/auth/useCurrentUser'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { stockApi } from '@/features/inventory/stockApi'
import type { BagScan } from '@/features/inventory/stockTypes'
import { parseApiError } from '@/services/apiError'
import { computeRemainingSeconds, formatCountdown, formatDateTime } from '@/shared/formatters/date'
import { getStatusMeta, getStatusTone } from '@/features/clinical/format'
import {
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
    useHandoverSessionDetail,
    usePendingHandoverSessions,
} from '../hooks'
import type { HandoverSessionStatus } from '../types'
import '@/shared/styles/HandoverLayout.css'

interface LocationState {
    scannedBag?: BagScan
}

const TIMEOUT_OPTIONS = [
    { value: 3, label: '3 phút' },
    { value: 4, label: '4 phút' },
    { value: 5, label: '5 phút (Mặc định)' },
]

function getSessionStatusBadge(status: HandoverSessionStatus): {
    label: string
    tone: 'info' | 'success' | 'warning' | 'danger'
} {
    switch (status) {
        case 'CONFIRMED':
            return { label: 'Đã xác nhận bàn giao (CONFIRMED)', tone: 'success' }
        case 'CANCELLED':
            return { label: 'Đã hủy sớm (CANCELLED)', tone: 'warning' }
        case 'EXPIRED':
            return { label: 'Đã hết hạn - tự hoàn tác (EXPIRED)', tone: 'danger' }
        default:
            return { label: 'Đang chờ Lâm sàng xác nhận (PENDING)', tone: 'info' }
    }
}

export default function HandoverDetailPage() {
    const { message } = App.useApp()
    const navigate = useNavigate()
    const { barcode: rawParam } = useParams<{ barcode: string }>()
    const [searchParams, setSearchParams] = useSearchParams()
    const location = useLocation()
    const state = (location.state as LocationState | null) ?? null

    const { displayName, roleLabel } = useCurrentUser()
    const { can } = usePermission()
    const canApprove = can(PERMISSIONS.BLOOD_REQUEST_APPROVE)

    const createSessionMutation = useCreateHandoverSession()
    const cancelSessionMutation = useCancelHandoverSession()

    const decodedBarcode = rawParam ? decodeURIComponent(rawParam) : ''
    const querySessionId = Number(searchParams.get('sessionId') || 0)

    const [bag, setBag] = useState<BagScan | null>(() =>
        state?.scannedBag && state.scannedBag.barcode === decodedBarcode ? state.scannedBag : null,
    )
    const [loadingBag, setLoadingBag] = useState<boolean>(
        () => !(state?.scannedBag && state.scannedBag.barcode === decodedBarcode),
    )
    const [loadError, setLoadError] = useState<string | null>(null)
    const [timeoutMinutes, setTimeoutMinutes] = useState<number>(5)
    const [createdSessionId, setCreatedSessionId] = useState<number | null>(
        Number.isInteger(querySessionId) && querySessionId > 0 ? querySessionId : null,
    )
    const [confirmError, setConfirmError] = useState<string | null>(null)
    const [cancelModalOpen, setCancelModalOpen] = useState(false)
    const [cancelReason, setCancelReason] = useState('')
    const [nowMs, setNowMs] = useState(() => Date.now())

    const pendingSessionsQuery = usePendingHandoverSessions(undefined, {
        enabled: !createdSessionId && Boolean(decodedBarcode),
    })

    const matchedPendingSessionId = useMemo(() => {
        if (createdSessionId) return createdSessionId
        if (!decodedBarcode) return null
        const normalized = decodedBarcode.trim().toLowerCase()
        const hit = (pendingSessionsQuery.data ?? []).find((s) =>
            s.items.some((i) => i.barcode.trim().toLowerCase() === normalized),
        )
        return hit?.handoverSessionId ?? null
    }, [createdSessionId, decodedBarcode, pendingSessionsQuery.data])

    const sessionDetailQuery = useHandoverSessionDetail(matchedPendingSessionId, {
        refetchInterval: (data) => (data?.status === 'PENDING' ? 4000 : false),
    })
    const activeSession = sessionDetailQuery.data ?? null

    useEffect(() => {
        if (activeSession?.status !== 'PENDING') return
        const timer = window.setInterval(() => {
            setNowMs(Date.now())
        }, 1000)
        return () => window.clearInterval(timer)
    }, [activeSession?.status])

    useEffect(() => {
        if (state?.scannedBag && state.scannedBag.barcode === decodedBarcode) {
            return
        }

        if (!decodedBarcode) {
            setLoadingBag(false)
            setLoadError('Không có mã barcode túi máu để hiển thị.')
            return
        }

        let cancelled = false
        setLoadingBag(true)
        setLoadError(null)

        stockApi
            .scan(decodedBarcode)
            .then((res) => {
                if (!cancelled) setBag(res)
            })
            .catch((err) => {
                if (!cancelled) setLoadError(parseApiError(err).message)
            })
            .finally(() => {
                if (!cancelled) setLoadingBag(false)
            })

        return () => {
            cancelled = true
        }
    }, [decodedBarcode, state])

    const isExpired = Boolean(
        bag && (bag.isExpired || new Date(bag.expiredAt).getTime() <= nowMs),
    )
    const isEligibleToOpenSession = Boolean(
        bag &&
            !activeSession &&
            bag.currentStatus === 'RESERVED' &&
            !bag.isLocked &&
            !bag.isDiscarded &&
            !isExpired,
    )

    const handleCreateSession = async (barcodesToOpen: string[]) => {
        if (!canApprove || barcodesToOpen.length === 0 || createSessionMutation.isPending) return
        setConfirmError(null)
        try {
            const session = await createSessionMutation.mutateAsync({
                barcodes: barcodesToOpen,
                timeoutMinutes,
            })
            setCreatedSessionId(session.handoverSessionId)
            setSearchParams({ sessionId: String(session.handoverSessionId) }, { replace: true })
            message.success(`Đã mở phiên bàn giao ${session.sessionCode}. Chờ Lâm sàng quét xác nhận!`)
        } catch (err) {
            const msg = parseApiError(err).message
            setConfirmError(msg)
            message.error(msg)
        }
    }

    const handleCancelSession = async () => {
        if (!activeSession || cancelSessionMutation.isPending) return
        try {
            const res = await cancelSessionMutation.mutateAsync({
                sessionId: activeSession.handoverSessionId,
                data: cancelReason.trim() ? { cancelReason: cancelReason.trim() } : undefined,
            })
            setCancelModalOpen(false)
            setCancelReason('')
            message.success(`Đã hủy sớm phiên ${res.sessionCode}. Toàn bộ túi giữ nguyên RESERVED.`)
        } catch (err) {
            message.error(parseApiError(err).message)
        }
    }

    const handleBackToScan = () => {
        navigate('/coordination/handover')
    }

    const sessionBadge = activeSession ? getSessionStatusBadge(activeSession.status) : null
    const remainingSeconds =
        activeSession && activeSession.status === 'PENDING'
            ? computeRemainingSeconds(activeSession.expiresAt, nowMs)
            : 0

    return (
        <PageContainer className="handover-page">
            <PageHeader
                breadcrumb={[
                    { title: 'Điều phối Kho máu', path: '/coordination/queue' },
                    { title: 'Bàn giao cấp phát máu', path: '/coordination/handover' },
                    { title: 'Thông tin phiên & túi máu bàn giao' },
                ]}
                title="Bàn giao cấp phát máu"
                subtitle="Mở phiên bàn giao 2 pha và theo dõi trạng thái xác nhận từ khoa Lâm sàng tại quầy."
                extra={
                    <StatusPill tone={sessionBadge ? sessionBadge.tone : 'info'}>
                        {sessionBadge ? sessionBadge.label : 'Chuẩn bị mở phiên bàn giao'}
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

                {loadingBag ? (
                    <div style={{ padding: 48, textAlign: 'center' }}>
                        <Spin tip="Đang tải thông tin túi máu từ hệ thống..." />
                    </div>
                ) : loadError || !bag ? (
                    <Result
                        status="warning"
                        title="Không thể tải thông tin túi máu"
                        subTitle={loadError || 'Vui lòng quét lại mã barcode túi máu.'}
                        extra={
                            <Button type="primary" icon={<ArrowLeftOutlined />} onClick={handleBackToScan}>
                                Quay lại màn hình quét
                            </Button>
                        }
                    />
                ) : (
                    <>
                        <div className="handover-bag-banner">
                            <div className="handover-bag-banner__left">
                                <BloodGroupTag value={bag.bloodGroup} size="lg" />
                                <div>
                                    <div className="handover-bag-banner__label">Túi máu đang xem</div>
                                    <div className="handover-bag-banner__code">{bag.barcode}</div>
                                    <div className="handover-bag-banner__meta">
                                        {bag.componentType} - {bag.volumeMl} ml - Hạn {formatDateTime(bag.expiredAt)}
                                    </div>
                                </div>
                            </div>
                            <StatusPill
                                tone={
                                    activeSession?.status === 'CONFIRMED'
                                        ? 'success'
                                        : bag.currentStatus === 'RESERVED'
                                          ? 'primary'
                                          : 'warning'
                                }
                            >
                                {activeSession?.status === 'CONFIRMED'
                                    ? 'Đã xuất kho (ISSUED)'
                                    : bag.currentStatusLabel}
                            </StatusPill>
                        </div>

                        <div className="handover-detail-box">
                            <div className="handover-detail-box__header">
                                <div>
                                    <div className="handover-detail-box__eyebrow">Thông tin túi máu</div>
                                    <div className="handover-detail-box__code">{bag.barcode}</div>
                                </div>
                                <div style={{ width: 180 }}>
                                    <BarcodeSvg value={bag.barcode} height={42} />
                                </div>
                            </div>

                            <div className="handover-detail-grid">
                                <div className="handover-kv">
                                    <span>Mã barcode</span>
                                    <strong>{bag.barcode}</strong>
                                </div>
                                <div className="handover-kv">
                                    <span>Nhóm máu</span>
                                    <strong><BloodGroupTag value={bag.bloodGroup} size="sm" /></strong>
                                </div>
                                <div className="handover-kv">
                                    <span>Chế phẩm</span>
                                    <strong>{bag.componentType} ({bag.componentTypeCode})</strong>
                                </div>
                                <div className="handover-kv">
                                    <span>Thể tích</span>
                                    <strong>{bag.volumeMl} ml</strong>
                                </div>
                                <div className="handover-kv">
                                    <span>Ngày lấy máu</span>
                                    <strong>{formatDateTime(bag.collectedAt)}</strong>
                                </div>
                                <div className="handover-kv">
                                    <span>Hạn sử dụng</span>
                                    <strong>{formatDateTime(bag.expiredAt)}</strong>
                                </div>
                                <div className="handover-kv">
                                    <span>Vị trí kho hiện tại</span>
                                    <strong>
                                        {activeSession?.status === 'CONFIRMED'
                                            ? 'Đã xuất khỏi tủ kho'
                                            : bag.storageLocation?.locationName || '—'}
                                    </strong>
                                </div>
                                <div className="handover-kv">
                                    <span>Trạng thái túi</span>
                                    <strong>
                                        {activeSession?.status === 'CONFIRMED'
                                            ? 'ISSUED - Đã xuất kho bàn giao'
                                            : `${bag.currentStatus} - ${bag.currentStatusLabel}`}
                                    </strong>
                                </div>
                            </div>
                        </div>

                        {activeSession ? (
                            <>
                                {activeSession.status === 'PENDING' && (
                                    <Alert
                                        type="warning"
                                        showIcon
                                        icon={<ClockCircleOutlined />}
                                        message={`Phiên ${activeSession.sessionCode} đang chờ Lâm sàng xác nhận (${remainingSeconds > 0 ? `còn ${formatCountdown(remainingSeconds)}` : 'hết giờ - đang tự động hoàn tác'})`}
                                        description="Quy tắc tại quầy (SOP): Túi máu vẫn đang giữ RESERVED trong tủ bảo quản. Chỉ trao túi vào thùng bảo ôn sau khi màn hình báo CONFIRMED."
                                    />
                                )}

                                {activeSession.status === 'CONFIRMED' && (
                                    <Alert
                                        type="success"
                                        showIcon
                                        message={`Phiên ${activeSession.sessionCode} đã xác nhận thành công`}
                                        description={`Lâm sàng (${activeSession.confirmedByName || 'Nhân sự lâm sàng'}) đã xác nhận lúc ${formatDateTime(activeSession.confirmedAt)}. Toàn bộ túi đã chuyển ISSUED — có thể trao túi máu cho người lĩnh.`}
                                    />
                                )}

                                {(activeSession.status === 'CANCELLED' || activeSession.status === 'EXPIRED') && (
                                    <Alert
                                        type="error"
                                        showIcon
                                        message={
                                            activeSession.status === 'EXPIRED'
                                                ? `Phiên ${activeSession.sessionCode} đã quá thời gian chờ`
                                                : `Phiên ${activeSession.sessionCode} đã được hủy sớm`
                                        }
                                        description={
                                            activeSession.cancelReason ||
                                            'Toàn bộ túi máu trong phiên đã quay về trạng thái RESERVED và giữ nguyên vị trí tủ để có thể mở lại phiên mới.'
                                        }
                                    />
                                )}

                                <div className="handover-detail-box">
                                    <div className="handover-detail-box__header">
                                        <div>
                                            <div className="handover-detail-box__eyebrow">Chi tiết phiên & Phiếu yêu cầu</div>
                                            <div className="handover-detail-box__code">
                                                {activeSession.sessionCode} - {activeSession.requestCode}
                                            </div>
                                        </div>
                                        <StatusPill tone={getStatusTone(activeSession.requestStatus)}>
                                            {getStatusMeta(activeSession.requestStatus).label}
                                        </StatusPill>
                                    </div>

                                    <div className="handover-detail-grid">
                                        <div className="handover-kv">
                                            <span>Người bệnh</span>
                                            <strong>{activeSession.patientName} ({activeSession.patientCode})</strong>
                                        </div>
                                        <div className="handover-kv">
                                            <span>Khoa / Bác sĩ</span>
                                            <strong>
                                                {activeSession.departmentName || '—'} - {activeSession.doctorName || '—'}
                                            </strong>
                                        </div>
                                        <div className="handover-kv">
                                            <span>Nhóm máu bệnh nhân</span>
                                            <strong><BloodGroupTag value={activeSession.patientBloodType} size="sm" /></strong>
                                        </div>
                                        <div className="handover-kv">
                                            <span>Mức ưu tiên</span>
                                            <strong><UrgencyTag level={activeSession.urgencyLevel} /></strong>
                                        </div>
                                    </div>

                                    <div className="handover-detail-box__eyebrow" style={{ marginTop: 8 }}>
                                        Danh sách túi trong phiên ({activeSession.items.length} túi)
                                    </div>
                                    <div className="handover-session-items">
                                        {activeSession.items.map((item) => (
                                            <div key={item.allocationId} className="handover-session-chip">
                                                <span className="text-mono" style={{ fontWeight: 700, color: 'var(--c-text)' }}>
                                                    {item.barcode}
                                                </span>
                                                <span>
                                                    {item.componentCode} - {item.bloodType} - {item.volumeMl}ml - {item.bagStatus}
                                                </span>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            </>
                        ) : (
                            <>
                                {bag.currentStatus !== 'RESERVED' && (
                                    <Alert
                                        type="warning"
                                        showIcon
                                        message="Túi máu không còn ở trạng thái chờ bàn giao"
                                        description={`Trạng thái hiện tại từ hệ thống là "${bag.currentStatusLabel}" (${bag.currentStatus}). Không thể mở phiên bàn giao.`}
                                    />
                                )}

                                {bag.currentStatus === 'RESERVED' && isExpired && (
                                    <Alert
                                        type="error"
                                        showIcon
                                        message="Túi máu đã hết hạn sử dụng"
                                        description="Không thể mở phiên bàn giao túi máu đã quá hạn."
                                    />
                                )}

                                {isEligibleToOpenSession && (
                                    <Alert
                                        type="info"
                                        showIcon
                                        message="Sẵn sàng mở phiên bàn giao 2 pha (Pha 1 - BBNK)"
                                        description="Chọn thời gian chờ Lâm sàng xác nhận (3–5 phút). Để bàn giao nhiều túi cùng lúc trong 1 lô, bạn có thể quay lại trang chính và thêm nhiều túi vào khay."
                                    />
                                )}
                            </>
                        )}

                        {confirmError && (
                            <Alert
                                type="error"
                                showIcon
                                closable
                                onClose={() => setConfirmError(null)}
                                message="Thao tác phiên bàn giao thất bại"
                                description={confirmError}
                            />
                        )}

                        <div className="handover-footer-actions">
                            <Button
                                icon={<ArrowLeftOutlined />}
                                onClick={handleBackToScan}
                                disabled={createSessionMutation.isPending || cancelSessionMutation.isPending}
                            >
                                Quay lại danh sách bàn giao
                            </Button>

                            {!activeSession && isEligibleToOpenSession && (
                                <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
                                    <Select
                                        value={timeoutMinutes}
                                        options={TIMEOUT_OPTIONS}
                                        style={{ width: 165 }}
                                        onChange={setTimeoutMinutes}
                                    />
                                    <Button
                                        type="primary"
                                        size="large"
                                        icon={<CheckCircleOutlined />}
                                        loading={createSessionMutation.isPending}
                                        disabled={!canApprove || createSessionMutation.isPending}
                                        onClick={() => void handleCreateSession([bag.barcode])}
                                    >
                                        Mở phiên bàn giao (Pha 1)
                                    </Button>
                                </div>
                            )}

                            {activeSession?.status === 'PENDING' && (
                                <Button
                                    danger
                                    icon={<CloseCircleOutlined />}
                                    disabled={!canApprove || cancelSessionMutation.isPending}
                                    onClick={() => {
                                        setCancelModalOpen(true)
                                        setCancelReason('')
                                    }}
                                >
                                    Hủy phiên sớm
                                </Button>
                            )}

                            {(activeSession?.status === 'CANCELLED' || activeSession?.status === 'EXPIRED') && (
                                <Button
                                    type="primary"
                                    icon={<CheckCircleOutlined />}
                                    loading={createSessionMutation.isPending}
                                    disabled={!canApprove || createSessionMutation.isPending}
                                    onClick={() => void handleCreateSession(activeSession.items.map((i) => i.barcode))}
                                >
                                    Mở lại phiên cho lô này
                                </Button>
                            )}
                        </div>
                    </>
                )}
            </section>

            <Modal
                title={activeSession ? `Hủy sớm phiên ${activeSession.sessionCode}` : 'Hủy sớm phiên'}
                open={cancelModalOpen}
                onCancel={() => {
                    if (!cancelSessionMutation.isPending) setCancelModalOpen(false)
                }}
                onOk={() => void handleCancelSession()}
                okText="Xác nhận hủy phiên"
                cancelText="Đóng"
                okButtonProps={{ danger: true, loading: cancelSessionMutation.isPending }}
            >
                <Input.TextArea
                    rows={3}
                    maxLength={255}
                    showCount
                    placeholder="Nhập lý do hủy phiên tại quầy (không bắt buộc)..."
                    value={cancelReason}
                    onChange={(e) => setCancelReason(e.target.value)}
                />
            </Modal>
        </PageContainer>
    )
}
