import { useEffect, useState } from 'react'
import { Alert, App, Button, Checkbox, Input, Result, Spin, Tag } from 'antd'
import { ArrowLeftOutlined, CheckCircleOutlined, MedicineBoxOutlined } from '@ant-design/icons'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { useCurrentUser } from '@/features/auth/useCurrentUser'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { formatDateTime } from '@/shared/formatters/date'
import { clinicalApi } from '../clinicalApi'
import { useConfirmReceipt } from '../hooks'
import { getStatusMeta, getUrgencyMeta } from '../format'
import type { ConfirmReceiptResponse, PendingReceiptBagDto } from '../types'
import '@/features/coordination/pages/HandoverPages.css'
import './ClinicalHandoverPages.css'

interface LocationState {
    bag?: PendingReceiptBagDto
    scannedBarcode?: string
}

export default function ClinicalHandoverDetailPage() {
    const { message } = App.useApp()
    const navigate = useNavigate()
    const { allocationId: rawAllocId } = useParams<{ allocationId: string }>()
    const location = useLocation()
    const state = (location.state as LocationState | null) ?? null

    const { displayName, roleLabel } = useCurrentUser()
    const { can } = usePermission()
    const canEdit = can(PERMISSIONS.BLOOD_REQUEST_EDIT)
    const confirmMutation = useConfirmReceipt()

    const allocationId = Number(rawAllocId)
    const [bag, setBag] = useState<PendingReceiptBagDto | null>(state?.bag ?? null)
    const [loading, setLoading] = useState<boolean>(!state?.bag)
    const [loadError, setLoadError] = useState<string | null>(null)

    const [verifiedPhysicalMatch, setVerifiedPhysicalMatch] = useState<boolean>(false)
    const [expectedPatientCode, setExpectedPatientCode] = useState<string>('')
    const [confirmError, setConfirmError] = useState<string | null>(null)
    const [receiptResult, setReceiptResult] = useState<ConfirmReceiptResponse | null>(null)

    useEffect(() => {
        if (state?.bag && state.bag.allocationId === allocationId) {
            setBag(state.bag)
            setLoading(false)
            setLoadError(null)
            return
        }

        if (!Number.isInteger(allocationId) || allocationId <= 0) {
            setLoading(false)
            setLoadError('Mã phân bổ bàn giao không hợp lệ.')
            return
        }

        let cancelled = false
        setLoading(true)
        setLoadError(null)

        clinicalApi
            .getPendingReceipts()
            .then((list) => {
                if (cancelled) return
                const found = list.find((item) => item.allocationId === allocationId) || null
                if (!found) {
                    setLoadError('Túi máu này không còn trong danh sách chờ nhận bàn giao hoặc đã được tiếp nhận trước đó.')
                }
                setBag(found)
            })
            .catch((err) => {
                if (!cancelled) setLoadError(parseApiError(err).message)
            })
            .finally(() => {
                if (!cancelled) setLoading(false)
            })

        return () => {
            cancelled = true
        }
    }, [allocationId, state])

    const scannedBarcode = state?.scannedBarcode || bag?.barcode || ''
    const requiresPatientVerify = Boolean(bag && !bag.isOwnRequest)
    const canConfirm = Boolean(
        bag &&
            !receiptResult &&
            canEdit &&
            verifiedPhysicalMatch &&
            (!requiresPatientVerify || expectedPatientCode.trim().length > 0) &&
            !confirmMutation.isPending,
    )

    const handleConfirmReceive = async () => {
        if (!bag || !canConfirm) return
        setConfirmError(null)

        try {
            const res = await confirmMutation.mutateAsync({
                allocationId: bag.allocationId,
                scannedBarcode: scannedBarcode.trim(),
                expectedPatientCode: requiresPatientVerify ? expectedPatientCode.trim() : undefined,
            })
            setReceiptResult(res)
        } catch (err) {
            setConfirmError(parseApiError(err).message)
        }
    }

    const handleBackToScan = () => {
        navigate('/clinical/handovers')
    }

    return (
        <div className="handover-page">
            <div className="handover-head">
                <div>
                    <div className="handover-breadcrumb">Yêu cầu máu Lâm sàng / W3.2 · Thông tin túi máu tiếp nhận</div>
                    <h1 className="handover-title">Thông tin túi máu tiếp nhận</h1>
                    <p className="handover-subtitle">
                        Đối chiếu thông tin túi máu thực tế với dữ liệu bàn giao từ Ngân hàng máu trước khi xác nhận tiếp nhận.
                    </p>
                </div>
                <span className={`handover-status-pill ${receiptResult ? 'is-success' : ''}`}>
                    <i /> {receiptResult ? 'Đã xác nhận tiếp nhận' : 'Chờ đối chiếu tiếp nhận'}
                </span>
            </div>

            <section className="handover-card">
                <div className="handover-role-bar">
                    <div className="handover-role-info">
                        <div className="handover-role-icon is-clin">
                            <MedicineBoxOutlined />
                        </div>
                        <div>
                            <div className="handover-role-title">Nhân viên / Điều dưỡng lâm sàng</div>
                            <div className="handover-role-sub">
                                {displayName} · {roleLabel || 'Khoa Lâm sàng'}
                            </div>
                        </div>
                    </div>
                    <span className="handover-role-badge is-clin">CLIN</span>
                </div>

                {loading ? (
                    <div style={{ padding: 48, textAlign: 'center' }}>
                        <Spin tip="Đang tải thông tin túi máu bàn giao..." />
                    </div>
                ) : loadError || !bag ? (
                    <Result
                        status="warning"
                        title="Không thể hiển thị thông tin tiếp nhận"
                        subTitle={loadError || 'Vui lòng quay lại màn hình quét barcode để kiểm tra lại.'}
                        extra={
                            <Button type="primary" icon={<ArrowLeftOutlined />} onClick={handleBackToScan}>
                                Quay lại màn hình quét
                            </Button>
                        }
                    />
                ) : (
                    <>
                        <div className="clin-pending-item">
                            <div>
                                <div className="clin-pending-item__eyebrow">
                                    Phiếu yêu cầu bàn giao · {bag.isOwnRequest ? 'Phiếu của bạn' : 'Nhận hộ trong khoa'}
                                </div>
                                <div className="clin-pending-item__title">
                                    {bag.requestCode} — {bag.patientName}
                                </div>
                                <div className="clin-pending-item__sub">
                                    Mã NB: {bag.patientCode} · Bác sĩ lập phiếu: {bag.doctorName || '—'} · Người xuất kho:{' '}
                                    {bag.issuedByName || '—'} ({bag.issuedAt ? formatDateTime(bag.issuedAt) : '—'})
                                </div>
                            </div>
                            <div className="clin-pending-item__right">
                                <Tag color={getUrgencyMeta(bag.urgencyLevel).color}>
                                    {getUrgencyMeta(bag.urgencyLevel).label}
                                </Tag>
                                <div className="handover-blood-badge">{bag.bloodType}</div>
                            </div>
                        </div>

                        <div className="clin-reconcile-box">
                            <div className="clin-reconcile-head">
                                <div className="clin-reconcile-icon">
                                    <CheckCircleOutlined />
                                </div>
                                <div>
                                    <div className="clin-reconcile-title">Đối chiếu thông tin túi máu thực tế & dữ liệu bàn giao</div>
                                    <div className="clin-reconcile-sub">
                                        Kiểm tra kỹ nhãn túi máu thực tế khớp với thông tin phiếu bàn giao trước khi xác nhận tiếp nhận.
                                    </div>
                                </div>
                            </div>

                            <table className="clin-reconcile-table">
                                <thead>
                                    <tr>
                                        <th>Tiêu chí</th>
                                        <th>Dữ liệu bàn giao</th>
                                        <th>Barcode đã quét</th>
                                        <th>Trạng thái</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td>Mã túi máu</td>
                                        <td><strong style={{ fontFamily: 'monospace' }}>{bag.barcode}</strong></td>
                                        <td><strong className="clin-reconcile-ok" style={{ fontFamily: 'monospace' }}>{scannedBarcode}</strong></td>
                                        <td className="clin-reconcile-ok">✓ Khớp mã</td>
                                    </tr>
                                    <tr>
                                        <td>Nhóm máu</td>
                                        <td><strong>{bag.bloodType}</strong></td>
                                        <td><strong className="clin-reconcile-ok">{bag.bloodType}</strong></td>
                                        <td className="clin-reconcile-ok">✓</td>
                                    </tr>
                                    <tr>
                                        <td>Chế phẩm & thể tích</td>
                                        <td><strong>{bag.componentName} ({bag.volumeMl} ml)</strong></td>
                                        <td><strong className="clin-reconcile-ok">{bag.componentCode} · {bag.volumeMl} ml</strong></td>
                                        <td className="clin-reconcile-ok">✓</td>
                                    </tr>
                                    <tr>
                                        <td>Hạn sử dụng</td>
                                        <td>{formatDateTime(bag.expiresAt)}</td>
                                        <td>{formatDateTime(bag.expiresAt)}</td>
                                        <td className="clin-reconcile-ok">✓</td>
                                    </tr>
                                    <tr>
                                        <td>Người bệnh nhận máu</td>
                                        <td><strong>{bag.patientName}</strong> ({bag.patientCode})</td>
                                        <td>Phiếu {bag.requestCode}</td>
                                        <td className="clin-reconcile-ok">✓</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>

                        {!receiptResult && (
                            <div className="handover-detail-box">
                                {requiresPatientVerify && (
                                    <>
                                        <Alert
                                            type="warning"
                                            showIcon
                                            message="Xác nhận nhận hộ trong khoa"
                                            description="Bạn đang nhận túi máu cho phiếu của bác sĩ khác trong khoa. Theo quy định an toàn, vui lòng nhập chính xác Mã người bệnh để đối soát."
                                        />
                                        <div>
                                            <label
                                                htmlFor="expected-patient-code"
                                                style={{ display: 'block', fontWeight: 600, fontSize: 13, marginBottom: 6 }}
                                            >
                                                Mã người bệnh đối soát <span style={{ color: '#dc2626' }}>*</span>
                                            </label>
                                            <Input
                                                id="expected-patient-code"
                                                size="large"
                                                placeholder={`Nhập mã người bệnh (ví dụ: ${bag.patientCode})`}
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
                                    Tôi đã kiểm tra trực quan túi máu thực tế (mã túi, nhóm máu, chế phẩm, thể tích, hạn dùng, nguyên vẹn nhãn) trùng khớp hoàn toàn với dữ liệu bàn giao ở trên.
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

                        {receiptResult && (
                            <Alert
                                type="success"
                                showIcon
                                message={`Tiếp nhận thành công túi máu ${receiptResult.barcode}`}
                                description={
                                    <div style={{ display: 'grid', gap: 4, marginTop: 4 }}>
                                        <div>
                                            Trạng thái phân bổ: <strong>{receiptResult.allocationStatus}</strong> · Trạng thái phiếu{' '}
                                            <strong>{receiptResult.requestCode}</strong>:{' '}
                                            <Tag color={getStatusMeta(receiptResult.requestStatus).color}>
                                                {getStatusMeta(receiptResult.requestStatus).label} ({receiptResult.requestStatus})
                                            </Tag>
                                        </div>
                                        <div>
                                            Người tiếp nhận: <strong>{receiptResult.receivedByName || displayName}</strong> · Thời gian:{' '}
                                            {formatDateTime(receiptResult.confirmedAt)}
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
                                {receiptResult ? 'Quét nhận túi máu khác' : 'Quay lại quét barcode'}
                            </Button>

                            {!receiptResult && (
                                <Button
                                    type="primary"
                                    size="large"
                                    icon={<CheckCircleOutlined />}
                                    className="handover-btn-primary"
                                    loading={confirmMutation.isPending}
                                    disabled={!canConfirm}
                                    onClick={() => {
                                        if (!verifiedPhysicalMatch) {
                                            message.warning('Vui lòng tick xác nhận đã đối chiếu thông tin thực tế.')
                                            return
                                        }
                                        void handleConfirmReceive()
                                    }}
                                >
                                    Xác nhận tiếp nhận
                                </Button>
                            )}
                        </div>
                    </>
                )}
            </section>
        </div>
    )
}
