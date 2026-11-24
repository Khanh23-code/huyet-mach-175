import { useEffect, useState } from 'react'
import { Alert, App, Button, Result, Spin } from 'antd'
import {
    ArrowLeftOutlined,
    CheckCircleOutlined,
    InboxOutlined,
} from '@ant-design/icons'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { useCurrentUser } from '@/features/auth/useCurrentUser'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { stockApi } from '@/features/inventory/stockApi'
import type { BagScan } from '@/features/inventory/stockTypes'
import { parseApiError } from '@/services/apiError'
import { formatDateTime } from '@/shared/formatters/date'
import { getStatusMeta, getStatusTone } from '@/features/clinical/format'
import {
    BarcodeSvg,
    BloodGroupTag,
    PageContainer,
    PageHeader,
    StatusPill,
    UrgencyTag,
} from '@/shared/components'
import { useDispatchBag } from '../hooks'
import type { DispatchBloodBagResponse } from '../types'
import '@/shared/styles/HandoverLayout.css'

interface LocationState {
    scannedBag?: BagScan
}

export default function HandoverDetailPage() {
    const { message } = App.useApp()
    const navigate = useNavigate()
    const { barcode: rawParam } = useParams<{ barcode: string }>()
    const location = useLocation()
    const state = (location.state as LocationState | null) ?? null

    const { displayName, roleLabel } = useCurrentUser()
    const { can } = usePermission()
    const canApprove = can(PERMISSIONS.BLOOD_REQUEST_APPROVE)
    const dispatchMutation = useDispatchBag()

    const decodedBarcode = rawParam ? decodeURIComponent(rawParam) : ''

    const [bag, setBag] = useState<BagScan | null>(state?.scannedBag ?? null)
    const [loadingBag, setLoadingBag] = useState<boolean>(!state?.scannedBag)
    const [loadError, setLoadError] = useState<string | null>(null)
    const [dispatched, setDispatched] = useState<DispatchBloodBagResponse | null>(null)
    const [confirmError, setConfirmError] = useState<string | null>(null)

    useEffect(() => {
        if (state?.scannedBag && state.scannedBag.barcode === decodedBarcode) {
            setBag(state.scannedBag)
            setLoadingBag(false)
            setLoadError(null)
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
        bag && (bag.isExpired || new Date(bag.expiredAt).getTime() <= Date.now()),
    )
    const isEligibleForDispatch = Boolean(
        bag &&
            !dispatched &&
            bag.currentStatus === 'RESERVED' &&
            !bag.isLocked &&
            !bag.isDiscarded &&
            !isExpired,
    )

    const handleConfirmHandover = async () => {
        if (!bag || !isEligibleForDispatch || dispatchMutation.isPending) return
        setConfirmError(null)
        try {
            const res = await dispatchMutation.mutateAsync({ barcode: bag.barcode })
            setDispatched(res)
            message.success(`Đã xác nhận xuất kho bàn giao túi máu ${res.barcode} cho phiếu ${res.requestCode}.`)
        } catch (err) {
            const msg = parseApiError(err).message
            setConfirmError(msg)
            message.error(msg)
        }
    }

    const handleBackToScan = () => {
        navigate('/coordination/handover')
    }

    return (
        <PageContainer className="handover-page">
            <PageHeader
                breadcrumb={[
                    { title: 'Điều phối Kho máu', path: '/coordination/queue' },
                    { title: 'Bàn giao cấp phát máu', path: '/coordination/handover' },
                    { title: 'Thông tin túi máu bàn giao' },
                ]}
                title="Bàn giao cấp phát máu"
                subtitle="Kiểm tra thông tin túi máu đã quét và xác nhận xuất kho bàn giao cho khoa lâm sàng."
                extra={
                    <StatusPill tone={dispatched ? 'success' : 'info'}>
                        {dispatched ? 'Đã xuất kho bàn giao' : 'Chờ xác nhận xuất kho'}
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
                                    <div className="handover-bag-banner__label">Túi máu đã quét</div>
                                    <div className="handover-bag-banner__code">{bag.barcode}</div>
                                    <div className="handover-bag-banner__meta">
                                        {bag.componentType} - {bag.volumeMl} ml - Hạn {formatDateTime(bag.expiredAt)}
                                    </div>
                                </div>
                            </div>
                            <StatusPill
                                tone={dispatched ? 'success' : bag.currentStatus === 'RESERVED' ? 'primary' : 'warning'}
                            >
                                {dispatched ? 'Đã xuất kho (ISSUED)' : bag.currentStatusLabel}
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
                                    <strong>{dispatched ? 'Đã xuất khỏi tủ kho' : bag.storageLocation?.locationName || '—'}</strong>
                                </div>
                                <div className="handover-kv">
                                    <span>Trạng thái túi</span>
                                    <strong>
                                        {dispatched
                                            ? 'ISSUED - Đã xuất kho bàn giao'
                                            : `${bag.currentStatus} - ${bag.currentStatusLabel}`}
                                    </strong>
                                </div>
                            </div>
                        </div>

                        {dispatched ? (
                            <>
                                <Alert
                                    type="success"
                                    showIcon
                                    message={`Bàn giao xuất kho thành công túi máu ${dispatched.barcode}`}
                                    description={`Túi máu đã chuyển sang trạng thái ${dispatched.bagStatus} lúc ${formatDateTime(dispatched.issuedAt)} bởi ${dispatched.issuedByName || displayName}.`}
                                />

                                <div className="handover-detail-box">
                                    <div className="handover-detail-box__header">
                                        <div>
                                            <div className="handover-detail-box__eyebrow">Phiếu yêu cầu tương ứng</div>
                                            <div className="handover-detail-box__code">{dispatched.requestCode}</div>
                                        </div>
                                        <StatusPill tone={getStatusTone(dispatched.requestStatus)}>
                                            {getStatusMeta(dispatched.requestStatus).label}
                                        </StatusPill>
                                    </div>

                                    <div className="handover-detail-grid">
                                        <div className="handover-kv">
                                            <span>Người bệnh</span>
                                            <strong>{dispatched.patientName}</strong>
                                        </div>
                                        <div className="handover-kv">
                                            <span>Mã người bệnh</span>
                                            <strong>{dispatched.patientCode}</strong>
                                        </div>
                                        <div className="handover-kv">
                                            <span>Khoa / Bác sĩ</span>
                                            <strong>
                                                {dispatched.departmentName || '—'} - {dispatched.doctorName || '—'}
                                            </strong>
                                        </div>
                                        <div className="handover-kv">
                                            <span>Nhóm máu bệnh nhân</span>
                                            <strong><BloodGroupTag value={dispatched.patientBloodType} size="sm" /></strong>
                                        </div>
                                        <div className="handover-kv">
                                            <span>Mức ưu tiên</span>
                                            <strong>
                                                <UrgencyTag level={dispatched.urgencyLevel} />
                                            </strong>
                                        </div>
                                        <div className="handover-kv">
                                            <span>Thời gian xuất kho</span>
                                            <strong>{formatDateTime(dispatched.issuedAt)}</strong>
                                        </div>
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
                                        description={`Trạng thái hiện tại từ hệ thống là "${bag.currentStatusLabel}" (${bag.currentStatus}). Không thể thực hiện xác nhận xuất kho bàn giao.`}
                                    />
                                )}

                                {bag.currentStatus === 'RESERVED' && isExpired && (
                                    <Alert
                                        type="error"
                                        showIcon
                                        message="Túi máu đã hết hạn sử dụng"
                                        description="Không thể xuất kho bàn giao túi máu đã quá hạn."
                                    />
                                )}

                                {isEligibleForDispatch && (
                                    <Alert
                                        type="info"
                                        showIcon
                                        message="Sẵn sàng xác nhận xuất kho bàn giao"
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
                                message="Xác nhận bàn giao thất bại"
                                description={confirmError}
                            />
                        )}

                        <div className="handover-footer-actions">
                            <Button
                                icon={<ArrowLeftOutlined />}
                                onClick={handleBackToScan}
                                disabled={dispatchMutation.isPending}
                            >
                                {dispatched ? 'Quét túi máu tiếp theo' : 'Quay lại quét túi máu'}
                            </Button>

                            {!dispatched && (
                                <Button
                                    type="primary"
                                    size="large"
                                    icon={<CheckCircleOutlined />}
                                    loading={dispatchMutation.isPending}
                                    disabled={!canApprove || !isEligibleForDispatch || dispatchMutation.isPending}
                                    onClick={() => void handleConfirmHandover()}
                                >
                                    Xác nhận bàn giao
                                </Button>
                            )}
                        </div>
                    </>
                )}
            </section>
        </PageContainer>
    )
}
