import { useCallback, useState } from 'react'
import { App, Empty, Tag } from 'antd'
import { MedicineBoxOutlined, ReloadOutlined } from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import { useCurrentUser } from '@/features/auth/useCurrentUser'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { formatDateTime } from '@/shared/formatters/date'
import BarcodeScanBox from '@/shared/components/BarcodeScanBox'
import { clinicalApi } from '../clinicalApi'
import { usePendingReceipts } from '../hooks'
import { getUrgencyMeta } from '../format'
import '@/features/coordination/pages/HandoverPages.css'
import './ClinicalHandoverPages.css'

export default function ClinicalHandoverScanPage() {
    const { message } = App.useApp()
    const navigate = useNavigate()
    const { displayName, roleLabel } = useCurrentUser()
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)

    const {
        data: pendingBags = [],
        isLoading: isPendingLoading,
        isFetching: isPendingFetching,
        refetch: refetchPending,
    } = usePendingReceipts(undefined, { enabled: canView })

    const [verifying, setVerifying] = useState(false)
    const [errorMsg, setErrorMsg] = useState<string | null>(null)

    const handleScan = useCallback(
        async (rawBarcode: string) => {
            const barcode = rawBarcode.trim()
            if (!barcode) return

            if (!canView) {
                const msg = 'Bạn không có quyền BLOOD_REQUEST_VIEW để xác minh túi máu bàn giao.'
                setErrorMsg(msg)
                message.error(msg)
                return
            }

            setVerifying(true)
            setErrorMsg(null)

            try {
                // Luôn kiểm tra trực tiếp với Backend danh sách túi máu thuộc đợt bàn giao hợp lệ của khoa
                const latestPending = await clinicalApi.getPendingReceipts()
                const matched = latestPending.find(
                    (b) => b.barcode.trim().toLowerCase() === barcode.toLowerCase(),
                )

                if (!matched) {
                    const msg = `Không tìm thấy túi máu mã "${barcode}" trong đợt bàn giao hợp lệ đang chờ nhận tại khoa của bạn (túi chưa xuất kho, thuộc khoa khác hoặc đã xác nhận nhận trước đó).`
                    setErrorMsg(msg)
                    message.error(msg)
                    return
                }

                navigate(`/clinical/handovers/${matched.allocationId}`, {
                    state: { bag: matched, scannedBarcode: matched.barcode },
                })
            } catch (err) {
                const msg = parseApiError(err).message
                setErrorMsg(msg)
                message.error(msg)
            } finally {
                setVerifying(false)
            }
        },
        [canView, message, navigate],
    )

    return (
        <div className="handover-page">
            <div className="handover-head">
                <div>
                    <div className="handover-breadcrumb">Yêu cầu máu Lâm sàng / W3.2 · Nhận bàn giao túi máu</div>
                    <h1 className="handover-title">Nhận bàn giao túi máu</h1>
                    <p className="handover-subtitle">
                        Quét barcode túi máu được Ngân hàng máu bàn giao để xác minh đợt xuất kho hợp lệ trước khi đối chiếu tiếp nhận.
                    </p>
                </div>
                <span className="handover-status-pill">
                    <i /> {pendingBags.length > 0 ? `${pendingBags.length} túi chờ quét nhận` : 'Chờ quét nhận'}
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
                    <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                        <button
                            type="button"
                            onClick={() => void refetchPending()}
                            style={{
                                border: '1px solid #cbd5e1',
                                background: '#fff',
                                borderRadius: 8,
                                padding: '5px 12px',
                                fontSize: 12,
                                fontWeight: 600,
                                cursor: 'pointer',
                                display: 'inline-flex',
                                alignItems: 'center',
                                gap: 6,
                            }}
                            disabled={isPendingLoading || isPendingFetching}
                        >
                            <ReloadOutlined spin={isPendingFetching} /> Làm mới danh sách chờ
                        </button>
                        <span className="handover-role-badge is-clin">CLIN</span>
                    </div>
                </div>

                {pendingBags.length > 0 ? (
                    <div className="clin-pending-list">
                        {pendingBags.map((item) => {
                            const urgency = getUrgencyMeta(item.urgencyLevel)
                            return (
                                <div key={item.allocationId} className="clin-pending-item">
                                    <div>
                                        <div className="clin-pending-item__eyebrow">
                                            Túi máu đang chờ nhận · {item.isOwnRequest ? 'Phiếu của bạn' : 'Nhận hộ trong khoa'}
                                        </div>
                                        <div className="clin-pending-item__title">
                                            {item.requestCode} — {item.patientName}
                                        </div>
                                        <div className="clin-pending-item__sub">
                                            {item.patientCode} · {item.departmentName || 'Khoa điều trị'} · Xuất kho lúc{' '}
                                            {item.issuedAt ? formatDateTime(item.issuedAt) : '—'}
                                        </div>
                                    </div>
                                    <div className="clin-pending-item__right">
                                        <Tag color={urgency.color}>{urgency.label}</Tag>
                                        <div className="handover-blood-badge" style={{ height: 42, minWidth: 44, fontSize: 15 }}>
                                            {item.bloodType}
                                        </div>
                                        <div style={{ textAlign: 'right' }}>
                                            <div style={{ fontWeight: 700, fontSize: 13, color: '#0f172a' }}>
                                                {item.componentName} ({item.volumeMl} ml)
                                            </div>
                                            <div style={{ fontSize: 12, color: '#475569', fontFamily: 'monospace' }}>
                                                Mã túi dự kiến: {item.barcode}
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            )
                        })}
                    </div>
                ) : (
                    !isPendingLoading && (
                        <Empty
                            image={Empty.PRESENTED_IMAGE_SIMPLE}
                            description="Hiện không có túi máu nào đang ở trạng thái chờ nhận tại khoa của bạn."
                        />
                    )
                )}

                <BarcodeScanBox
                    title="Quét barcode túi máu thực nhận"
                    description="Hệ thống sẽ xác minh túi máu thuộc đợt bàn giao hợp lệ của khoa trước khi chuyển sang màn hình đối chiếu tiếp nhận."
                    placeholder="Nhập mã barcode túi máu..."
                    submitLabel="Quét & kiểm tra"
                    loading={verifying}
                    errorMessage={errorMsg}
                    onClearError={() => setErrorMsg(null)}
                    onScan={handleScan}
                />
            </section>
        </div>
    )
}
