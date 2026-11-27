import { useCallback, useEffect, useMemo, useState } from 'react'
import { Alert, App, Button, Checkbox, Input, Result, Spin, Tooltip } from 'antd'
import {
    ArrowLeftOutlined,
    CheckCircleOutlined,
    ClockCircleOutlined,
    CopyOutlined,
    MedicineBoxOutlined,
    ScanOutlined,
} from '@ant-design/icons'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { useCurrentUser } from '@/features/auth/useCurrentUser'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { computeRemainingSeconds, formatCountdown, formatDateTime } from '@/shared/formatters/date'
import {
    BarcodeScanBox,
    BloodGroupTag,
    PageContainer,
    PageHeader,
    StatusPill,
    UrgencyTag,
} from '@/shared/components'
import { useConfirmHandoverSession, useHandoverSessionDetail } from '../hooks'
import { getStatusMeta, getStatusTone } from '../format'
import type { HandoverSessionDto } from '../types'
import '@/shared/styles/HandoverLayout.css'
import './ClinicalHandoverPages.css'

interface LocationState {
    session?: HandoverSessionDto
    initialScannedBarcode?: string
}

export default function ClinicalHandoverDetailPage() {
    const { message } = App.useApp()
    const navigate = useNavigate()
    const { sessionId: rawSessionId } = useParams<{ sessionId: string }>()
    const location = useLocation()
    const state = (location.state as LocationState | null) ?? null

    const { displayName, roleLabel } = useCurrentUser()
    const { can } = usePermission()
    const canEdit = can(PERMISSIONS.BLOOD_REQUEST_EDIT)
    const confirmMutation = useConfirmHandoverSession()

    const validSessionId = rawSessionId?.trim() || null

    const sessionQuery = useHandoverSessionDetail(validSessionId, {
        refetchInterval: (data) => (!data || data.status === 'PENDING' ? 5000 : false),
    })

    const session = sessionQuery.data ?? (state?.session?.handoverSessionId === validSessionId ? state.session : null)

    const [scannedBarcodes, setScannedBarcodes] = useState<string[]>(() =>
        state?.initialScannedBarcode ? [state.initialScannedBarcode] : [],
    )
    const [scanError, setScanError] = useState<string | null>(null)
    const [verifiedPhysicalMatch, setVerifiedPhysicalMatch] = useState<boolean>(false)
    const [expectedPatientCode, setExpectedPatientCode] = useState<string>('')
    const [confirmError, setConfirmError] = useState<string | null>(null)
    const [confirmedResult, setConfirmedResult] = useState<HandoverSessionDto | null>(null)
    const [nowMs, setNowMs] = useState(() => Date.now())

    const activeSession = confirmedResult ?? session

    useEffect(() => {
        if (activeSession?.status !== 'PENDING') return
        const timer = window.setInterval(() => {
            setNowMs(Date.now())
        }, 1000)
        return () => window.clearInterval(timer)
    }, [activeSession?.status])

    const scannedSet = useMemo(
        () => new Set(scannedBarcodes.map((b) => b.trim().toLowerCase())),
        [scannedBarcodes],
    )

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

    const handleScanItemBarcode = useCallback(
        (rawBarcode: string) => {
            const barcode = rawBarcode.trim()
            if (!barcode || !activeSession) return

            setScanError(null)
            const normalized = barcode.toLowerCase()
            const targetItem = activeSession.items.find(
                (i) => i.barcode.trim().toLowerCase() === normalized,
            )

            if (!targetItem) {
                const msg = `Túi máu "${barcode}" không nằm trong phiên bàn giao ${activeSession.sessionCode}. Vui lòng kiểm tra lại đúng lô túi máu!`
                setScanError(msg)
                message.error(msg)
                return
            }

            if (scannedSet.has(normalized)) {
                message.info(`Túi máu ${targetItem.barcode} đã được quét đối soát trước đó.`)
                return
            }

            setScannedBarcodes((prev) => {
                if (prev.some((b) => b.trim().toLowerCase() === normalized)) return prev
                return [...prev, targetItem.barcode]
            })
            message.success(`Đã quét khớp túi ${targetItem.barcode}`)
        },
        [activeSession, message, scannedSet],
    )

    const remainingSeconds =
        activeSession && activeSession.status === 'PENDING'
            ? computeRemainingSeconds(activeSession.expiresAt, nowMs)
            : 0
    const isPendingAndAlive = Boolean(activeSession?.status === 'PENDING' && remainingSeconds > 0)
    const allItemsScanned = Boolean(
        activeSession &&
            activeSession.items.length > 0 &&
            activeSession.items.every((item) => scannedSet.has(item.barcode.trim().toLowerCase())),
    )
    const requiresPatientVerify = Boolean(activeSession && !activeSession.isOwnRequest)
    const canConfirm = Boolean(
        activeSession &&
            isPendingAndAlive &&
            !confirmedResult &&
            canEdit &&
            allItemsScanned &&
            verifiedPhysicalMatch &&
            (!requiresPatientVerify || expectedPatientCode.trim().length > 0) &&
            !confirmMutation.isPending,
    )

    const handleConfirmSession = async () => {
        if (!activeSession || !canConfirm) return
        setConfirmError(null)

        try {
            const res = await confirmMutation.mutateAsync({
                sessionId: activeSession.handoverSessionId,
                data: {
                    scannedBarcodes,
                    expectedPatientCode: requiresPatientVerify ? expectedPatientCode.trim() : undefined,
                },
            })
            setConfirmedResult(res)
        } catch (err) {
            setConfirmError(parseApiError(err).message)
        }
    }

    const handleBackToScan = () => {
        navigate('/clinical/handovers')
    }

    const loadErrorMessage = !validSessionId
        ? 'Mã phiên bàn giao không hợp lệ.'
        : sessionQuery.isError
          ? parseApiError(sessionQuery.error).message
          : null

    return (
        <PageContainer className="handover-page">
            <PageHeader
                breadcrumb={[
                    { title: 'Yêu cầu máu Lâm sàng', path: '/clinical/requests' },
                    { title: 'Nhận bàn giao túi máu', path: '/clinical/handovers' },
                    { title: 'Đối soát nhận lô máu theo phiên' },
                ]}
                title="Đối soát nhận lô túi máu (Pha 2)"
                subtitle="Quét đủ toàn bộ barcode túi máu trong phiên bàn giao đang chờ tại quầy trước khi hết thời gian hiệu lực."
                extra={
                    activeSession ? (
                        <StatusPill
                            tone={
                                activeSession.status === 'CONFIRMED'
                                    ? 'success'
                                    : activeSession.status === 'PENDING'
                                      ? remainingSeconds <= 60
                                          ? 'danger'
                                          : 'warning'
                                      : 'danger'
                            }
                        >
                            {activeSession.status === 'CONFIRMED'
                                ? 'Đã xác nhận nhận máu'
                                : activeSession.status === 'PENDING'
                                  ? remainingSeconds > 0
                                      ? `Còn ${formatCountdown(remainingSeconds)}`
                                      : 'Đã hết thời gian chờ'
                                  : activeSession.status === 'CANCELLED'
                                    ? 'Đã bị BBNK hủy sớm'
                                    : 'Đã hết hạn'}
                        </StatusPill>
                    ) : (
                        <StatusPill tone="info">Đang tải phiên</StatusPill>
                    )
                }
            />

            <section className="handover-card">
                <div className="handover-role-bar">
                    <div className="handover-role-info">
                        <div className="handover-role-icon is-clin">
                            <MedicineBoxOutlined />
                        </div>
                        <div>
                            <div className="handover-role-title">Nhân viên / Điều dưỡng lâm sàng</div>
                            <div className="handover-role-sub">
                                {displayName} - {roleLabel || 'Khoa Lâm sàng'}
                            </div>
                        </div>
                    </div>
                    <span className="handover-role-badge is-clin">CLIN</span>
                </div>

                {sessionQuery.isLoading && !activeSession ? (
                    <div style={{ padding: 48, textAlign: 'center' }}>
                        <Spin tip="Đang tải thông tin phiên bàn giao..." />
                    </div>
                ) : loadErrorMessage || !activeSession ? (
                    <Result
                        status="warning"
                        title="Không thể tải thông tin phiên bàn giao"
                        subTitle={loadErrorMessage || 'Vui lòng quay lại danh sách phiên bàn giao để kiểm tra lại.'}
                        extra={
                            <Button type="primary" icon={<ArrowLeftOutlined />} onClick={handleBackToScan}>
                                Quay lại danh sách phiên
                            </Button>
                        }
                    />
                ) : (
                    <>
                        <div className="clin-pending-item">
                            <div>
                                <div className="clin-pending-item__eyebrow">
                                    Phiên {activeSession.sessionCode} -{' '}
                                    {activeSession.isOwnRequest ? 'Phiếu của bạn' : 'Nhận hộ trong khoa'}
                                </div>
                                <div className="clin-pending-item__title">
                                    {activeSession.requestCode} - {activeSession.patientName} ({activeSession.patientCode})
                                </div>
                                <div className="clin-pending-item__sub">
                                    {activeSession.departmentName || 'Khoa điều trị'} - BS {activeSession.doctorName || '—'} - KTV khởi tạo:{' '}
                                    {activeSession.initiatedByName || 'KTV BBNK'} ({formatDateTime(activeSession.createdAt)})
                                </div>
                            </div>
                            <div className="clin-pending-item__right">
                                <UrgencyTag level={activeSession.urgencyLevel} />
                                <BloodGroupTag value={activeSession.patientBloodType} size="lg" />
                            </div>
                        </div>

                        {activeSession.status === 'PENDING' && remainingSeconds <= 0 && (
                            <Alert
                                type="error"
                                showIcon
                                icon={<ClockCircleOutlined />}
                                message="Phiên bàn giao đã hết thời gian chờ"
                                description="Phiên đã quá hạn 3–5 phút và đang tự động hoàn tác về trạng thái giữ chỗ. Vui lòng nhờ Ngân hàng máu mở lại phiên mới."
                            />
                        )}

                        {activeSession.status === 'EXPIRED' && (
                            <Alert
                                type="error"
                                showIcon
                                message="Phiên bàn giao đã hết hạn"
                                description="Toàn bộ túi máu trong phiên đã quay về trạng thái giữ chỗ tại tủ kho. Vui lòng nhờ Ngân hàng máu mở lại phiên mới."
                            />
                        )}

                        {activeSession.status === 'CANCELLED' && (
                            <Alert
                                type="warning"
                                showIcon
                                message="Phiên bàn giao đã bị Ngân hàng máu hủy sớm"
                                description={
                                    activeSession.cancelReason ||
                                    'Toàn bộ túi máu trong phiên giữ nguyên trạng thái giữ chỗ. Vui lòng liên hệ quầy Ngân hàng máu.'
                                }
                            />
                        )}

                        <div className="clin-reconcile-box">
                            <div className="clin-reconcile-head">
                                <div className="clin-reconcile-icon">
                                    <CheckCircleOutlined />
                                </div>
                                <div>
                                    <div className="clin-reconcile-title">
                                        Đối soát danh sách túi máu trong phiên ({scannedBarcodes.length}/{activeSession.items.length} túi đã quét)
                                    </div>
                                    <div className="clin-reconcile-sub">
                                        Quét đủ 100% mã vạch trên nhãn túi máu thực tế khớp với bảng bên dưới trước khi bấm xác nhận.
                                    </div>
                                </div>
                            </div>

                            {isPendingAndAlive && (
                                <BarcodeScanBox
                                    title="Quét từng túi máu trong lô"
                                    description="Đưa lần lượt mã vạch từng túi máu vào máy quét hoặc nhập mã barcode bên dưới."
                                    placeholder="Quét hoặc nhập mã barcode túi máu..."
                                    submitLabel="Quét khớp mã"
                                    errorMessage={scanError}
                                    onClearError={() => setScanError(null)}
                                    onScan={handleScanItemBarcode}
                                />
                            )}

                            <table className="clin-reconcile-table">
                                <thead>
                                    <tr>
                                        <th>Mã túi máu</th>
                                        <th>Nhóm máu</th>
                                        <th>Chế phẩm &amp; thể tích</th>
                                        <th>Hạn sử dụng</th>
                                        <th>Trạng thái đối soát</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {activeSession.items.map((item) => {
                                        const isScanned =
                                            activeSession.status === 'CONFIRMED' ||
                                            scannedSet.has(item.barcode.trim().toLowerCase())
                                        return (
                                            <tr key={item.allocationId}>
                                                <td>
                                                    <div className="handover-code-cell">
                                                        <strong className="text-mono">{item.barcode}</strong>
                                                        <Tooltip title="Sao chép mã">
                                                            <Button
                                                                type="text"
                                                                size="small"
                                                                icon={<CopyOutlined />}
                                                                onClick={() => void handleCopyBarcode(item.barcode)}
                                                            />
                                                        </Tooltip>
                                                    </div>
                                                </td>
                                                <td>
                                                    <BloodGroupTag value={item.bloodType} size="sm" />
                                                </td>
                                                <td>
                                                    <strong>{item.componentName}</strong> ({item.componentCode} - {item.volumeMl} ml)
                                                </td>
                                                <td>{formatDateTime(item.expiredAt)}</td>
                                                <td>
                                                    {isScanned ? (
                                                        <StatusPill tone="success">✓ Đã quét khớp</StatusPill>
                                                    ) : (
                                                        <div style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
                                                            <StatusPill tone="warning" dot={false}>Chưa quét</StatusPill>
                                                            {isPendingAndAlive && (
                                                                <Button
                                                                    size="small"
                                                                    icon={<ScanOutlined />}
                                                                    onClick={() => handleScanItemBarcode(item.barcode)}
                                                                >
                                                                    Đánh dấu khớp
                                                                </Button>
                                                            )}
                                                        </div>
                                                    )}
                                                </td>
                                            </tr>
                                        )
                                    })}
                                </tbody>
                            </table>
                        </div>

                        {isPendingAndAlive && !confirmedResult && (
                            <div className="handover-detail-box">
                                {requiresPatientVerify && (
                                    <>
                                        <Alert
                                            type="warning"
                                            showIcon
                                            message="Xác nhận nhận hộ trong khoa"
                                            description="Bạn đang nhận lô túi máu cho phiếu của bác sĩ khác trong khoa. Theo quy định an toàn, bắt buộc nhập chính xác Mã người bệnh để đối soát."
                                        />
                                        <div>
                                            <label
                                                htmlFor="expected-patient-code"
                                                style={{ display: 'block', fontWeight: 600, fontSize: 'var(--text-sm)', marginBottom: 6 }}
                                            >
                                                Mã người bệnh đối soát <span style={{ color: 'var(--c-danger)' }}>*</span>
                                            </label>
                                            <Input
                                                id="expected-patient-code"
                                                size="large"
                                                placeholder={`Nhập mã người bệnh (ví dụ: ${activeSession.patientCode})`}
                                                value={expectedPatientCode}
                                                onChange={(e) => {
                                                    setExpectedPatientCode(e.target.value)
                                                    if (confirmError) setConfirmError(null)
                                                }}
                                                disabled={confirmMutation.isPending}
                                            />
                                        </div>
                                    </>
                                )}

                                <Checkbox
                                    checked={verifiedPhysicalMatch}
                                    onChange={(e) => setVerifiedPhysicalMatch(e.target.checked)}
                                    disabled={confirmMutation.isPending}
                                >
                                    Tôi đã kiểm tra trực quan toàn bộ {activeSession.items.length} túi máu thực tế (mã túi, nhóm máu, chế phẩm, thể tích, hạn dùng, nguyên vẹn nhãn) trùng khớp hoàn toàn với phiên {activeSession.sessionCode}.
                                </Checkbox>
                            </div>
                        )}

                        {confirmError && (
                            <Alert
                                type="error"
                                showIcon
                                closable
                                onClose={() => setConfirmError(null)}
                                message="Xác nhận tiếp nhận thất bại"
                                description={confirmError}
                            />
                        )}

                        {activeSession.status === 'CONFIRMED' && (
                            <Alert
                                type="success"
                                showIcon
                                message={`Tiếp nhận thành công lô ${activeSession.items.length} túi máu (${activeSession.sessionCode})`}
                                description={
                                    <div style={{ display: 'grid', gap: 4, marginTop: 4 }}>
                                        <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}>
                                            <span>
                                                Toàn bộ túi đã chuyển trạng thái <strong>Đã xuất kho</strong> - Trạng thái phiếu{' '}
                                                <strong>{activeSession.requestCode}</strong>:
                                            </span>
                                            <StatusPill tone={getStatusTone(activeSession.requestStatus)} dot={false}>
                                                {getStatusMeta(activeSession.requestStatus).label}
                                            </StatusPill>
                                        </div>
                                        <div>
                                            Người tiếp nhận: <strong>{activeSession.confirmedByName || displayName}</strong> - Thời gian:{' '}
                                            {formatDateTime(activeSession.confirmedAt)}
                                        </div>
                                    </div>
                                }
                            />
                        )}

                        <div className="handover-footer-actions">
                            <Button
                                icon={<ArrowLeftOutlined />}
                                onClick={handleBackToScan}
                                disabled={confirmMutation.isPending}
                            >
                                {activeSession.status === 'CONFIRMED' ? 'Quay lại nhận phiên tiếp theo' : 'Quay lại danh sách phiên'}
                            </Button>

                            {isPendingAndAlive && !confirmedResult && (
                                <Button
                                    type="primary"
                                    size="large"
                                    icon={<CheckCircleOutlined />}
                                    loading={confirmMutation.isPending}
                                    disabled={!canConfirm}
                                    onClick={() => {
                                        void handleConfirmSession()
                                    }}
                                >
                                    Xác nhận nhận lô máu ({scannedBarcodes.length}/{activeSession.items.length})
                                </Button>
                            )}
                        </div>
                    </>
                )}
            </section>
        </PageContainer>
    )
}
