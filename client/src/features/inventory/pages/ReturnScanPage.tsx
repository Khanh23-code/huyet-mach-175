import { useCallback, useState } from 'react'
import { App, Button, Empty, Tag } from 'antd'
import { ReloadOutlined } from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { formatDateTime } from '@/shared/formatters/date'
import BarcodeScanBox from '@/shared/components/BarcodeScanBox'
import { returnsApi } from '../returnsApi'
import { usePendingReturns } from '../returnsHooks'
import '@/features/coordination/pages/HandoverPages.css'
import './ReturnPages.css'

export default function ReturnScanPage() {
    const { message } = App.useApp()
    const navigate = useNavigate()
    const { can } = usePermission()
    const canApprove = can(PERMISSIONS.BLOOD_RETURN_APPROVE)

    const {
        data: pendingPage,
        isLoading: isPendingLoading,
        isFetching: isPendingFetching,
        refetch: refetchPending,
    } = usePendingReturns({ pageNumber: 1, pageSize: 20 })

    const [scanning, setScanning] = useState(false)
    const [errorMsg, setErrorMsg] = useState<string | null>(null)

    const handleScan = useCallback(
        async (rawBarcode: string) => {
            const barcode = rawBarcode.trim()
            if (!barcode) return

            if (!canApprove) {
                const msg = 'Tài khoản không có quyền BLOOD_RETURN_APPROVE để kiểm định túi máu hoàn trả.'
                setErrorMsg(msg)
                message.error(msg)
                return
            }

            setScanning(true)
            setErrorMsg(null)

            try {
                const inspection = await returnsApi.scan(barcode)

                if (inspection.finalAction !== 'PENDING_INSPECTION') {
                    const msg = `Phiếu hoàn trả #${inspection.returnId} của túi ${inspection.barcode} đã được xử lý (${inspection.finalAction}).`
                    setErrorMsg(msg)
                    message.warning(msg)
                    return
                }

                if (inspection.bagStatus !== 'LOCKED') {
                    const msg = `Túi máu ${inspection.barcode} đang ở trạng thái ${inspection.bagStatus}, không ở trạng thái LOCKED chờ kiểm định.`
                    setErrorMsg(msg)
                    message.error(msg)
                    return
                }

                navigate(`/inventory/returns/${encodeURIComponent(inspection.barcode)}`, {
                    state: { inspection },
                })
            } catch (err) {
                const msg = parseApiError(err).message
                setErrorMsg(msg)
                message.error(msg)
            } finally {
                setScanning(false)
            }
        },
        [canApprove, message, navigate],
    )

    const pendingItems = pendingPage?.items || []

    return (
        <div className="return-page">
            <div className="handover-head">
                <div>
                    <div className="handover-breadcrumb">Túi máu & Kho / Tiếp nhận hoàn trả</div>
                    <h1 className="handover-title">Tiếp nhận hoàn trả túi máu</h1>
                    <p className="handover-subtitle">
                        Quét barcode túi máu được hoàn trả từ khoa lâm sàng để tra cứu phiếu và chuyển sang bước thẩm định bảo quản.
                    </p>
                </div>
                <span className="handover-status-pill">
                    <i /> {pendingPage?.totalCount ? `${pendingPage.totalCount} túi chờ kiểm định` : 'Sẵn sàng tiếp nhận trả'}
                </span>
            </div>

            <div className="return-scan-grid">
                <section className="return-card">
                    <div>
                        <h2 className="return-card__title">Quét mã túi máu hoàn trả</h2>
                        <p className="return-card__sub">
                            Quét barcode hoặc nhập mã thủ công để tra cứu túi máu và phiếu hoàn trả đang chờ kiểm định.
                        </p>
                    </div>

                    <BarcodeScanBox
                        title="Chờ quét mã barcode"
                        description="Đưa mã vạch túi máu hoàn trả vào máy đọc hoặc nhập mã thủ công bên dưới."
                        placeholder="Nhập mã túi máu thủ công..."
                        submitLabel="Tra cứu"
                        loading={scanning}
                        errorMessage={errorMsg}
                        onClearError={() => setErrorMsg(null)}
                        onScan={handleScan}
                    />
                </section>

                <aside className="return-card">
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 10 }}>
                        <div>
                            <h2 className="return-card__title">Phiếu hoàn trả chờ kiểm định</h2>
                            <p className="return-card__sub">
                                Danh sách túi máu đang ở trạng thái phong tỏa (LOCKED) từ khoa lâm sàng.
                            </p>
                        </div>
                        <Button
                            size="small"
                            icon={<ReloadOutlined spin={isPendingFetching} />}
                            onClick={() => void refetchPending()}
                            disabled={isPendingLoading || isPendingFetching}
                        >
                            Làm mới
                        </Button>
                    </div>

                    {pendingItems.length > 0 ? (
                        <div className="return-pending-list">
                            {pendingItems.map((item) => (
                                <div key={item.returnId} className="return-pending-item">
                                    <div className="return-pending-item__top">
                                        <span className="return-pending-item__code">{item.barcode}</span>
                                        <Tag color={item.isExpired ? 'red' : 'orange'}>
                                            {item.isExpired ? 'Đã hết hạn' : `Phiếu trả #${item.returnId}`}
                                        </Tag>
                                    </div>
                                    <div style={{ fontSize: 13, fontWeight: 600, color: '#0f172a' }}>
                                        {item.bloodType} · {item.componentName} ({item.volumeMl} ml)
                                    </div>
                                    <div style={{ fontSize: 12, color: '#475569' }}>
                                        {item.departmentName || 'Khoa lâm sàng'} · Phiếu YC: {item.requestCode}
                                    </div>
                                    <div style={{ fontSize: 12, color: '#64748b' }}>
                                        Lý do: {item.returnReason}
                                    </div>
                                    <div style={{ fontSize: 11, color: '#94a3b8' }}>
                                        Lập lúc {formatDateTime(item.processedAt)} · {item.returnedByName || 'Lâm sàng'}
                                    </div>
                                </div>
                            ))}
                        </div>
                    ) : (
                        !isPendingLoading && (
                            <Empty
                                image={Empty.PRESENTED_IMAGE_SIMPLE}
                                description="Hiện chưa có phiếu hoàn trả nào đang chờ kiểm định."
                            />
                        )
                    )}
                </aside>
            </div>
        </div>
    )
}
