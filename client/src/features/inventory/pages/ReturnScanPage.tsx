import { useCallback, useState } from 'react'
import { App, Button, Empty, Tag } from 'antd'
import { ReloadOutlined, ScanOutlined } from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { formatDateTime } from '@/shared/formatters/date'
import {
    BarcodeScanBox,
    BloodGroupTag,
    PageContainer,
    PageHeader,
    SectionCard,
    StatusPill,
} from '@/shared/components'
import { returnsApi } from '../returnsApi'
import { usePendingReturns } from '../returnsHooks'
import '@/shared/styles/HandoverLayout.css'
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
        <PageContainer>
            <PageHeader
                breadcrumb={[{ title: 'Túi máu & Kho' }, { title: 'Tiếp nhận hoàn trả' }]}
                title="Tiếp nhận hoàn trả túi máu"
                subtitle="Quét barcode túi máu được hoàn trả từ khoa lâm sàng để tra cứu phiếu và chuyển sang bước thẩm định bảo quản."
                extra={
                    <StatusPill tone={pendingPage?.totalCount ? 'warning' : 'primary'} pulse={Boolean(pendingPage?.totalCount)}>
                        {pendingPage?.totalCount ? `${pendingPage.totalCount} túi chờ kiểm định` : 'Sẵn sàng tiếp nhận trả'}
                    </StatusPill>
                }
            />

            <div className="return-scan-grid">
                <SectionCard
                    title="Quét mã túi máu hoàn trả"
                    subtitle="Quét barcode hoặc nhập mã thủ công để tra cứu túi máu và phiếu hoàn trả đang chờ kiểm định."
                >
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
                </SectionCard>

                <SectionCard
                    title="Phiếu hoàn trả chờ kiểm định"
                    subtitle="Danh sách túi máu đang ở trạng thái phong tỏa (LOCKED) từ khoa lâm sàng."
                    extra={
                        <Button
                            size="small"
                            icon={<ReloadOutlined spin={isPendingFetching} />}
                            onClick={() => void refetchPending()}
                            disabled={isPendingLoading || isPendingFetching}
                        >
                            Làm mới
                        </Button>
                    }
                >
                    {pendingItems.length > 0 ? (
                        <div className="return-pending-list">
                            {pendingItems.map((item) => (
                                <div key={item.returnId} className="return-pending-item">
                                    <div className="return-pending-item__top">
                                        <span className="return-pending-item__code">{item.barcode}</span>
                                        <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}>
                                            {item.requiresMandatoryDiscard && (
                                                <StatusPill tone="danger">Bắt buộc tiêu hủy</StatusPill>
                                            )}
                                            <StatusPill tone={item.isExpired ? 'danger' : 'warning'}>
                                                {item.isExpired ? 'Đã hết hạn' : `Phiếu trả #${item.returnId}`}
                                            </StatusPill>
                                        </div>
                                    </div>
                                    <div className="return-pending-item__title">
                                        <BloodGroupTag value={item.bloodType} size="sm" /> - {item.componentName} ({item.volumeMl} ml)
                                    </div>
                                    <div className="return-pending-item__meta">
                                        {item.departmentName || 'Khoa lâm sàng'} - Phiếu YC: {item.requestCode} -{' '}
                                        {item.allowRestock
                                            ? `Đã tái nhập ${item.previousRestockCount}/${item.maxRestockCount} lần`
                                            : 'Không cho phép tái nhập'}
                                    </div>
                                    <div className="return-pending-item__sub">
                                        Lý do: {item.returnReason}
                                    </div>
                                    {item.clinicalChecklistWarnings?.length > 0 && (
                                        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4, marginTop: 4 }}>
                                            {item.clinicalChecklistWarnings.map((w, idx) => (
                                                <Tag key={`${w}-${idx}`} color="warning" style={{ margin: 0 }}>
                                                    Cảnh báo LS: {w}
                                                </Tag>
                                            ))}
                                        </div>
                                    )}
                                    {item.mandatoryDiscardReasons?.length > 0 && (
                                        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4, marginTop: 4 }}>
                                            {item.mandatoryDiscardReasons.map((r, idx) => (
                                                <Tag key={`${r}-${idx}`} color="error" style={{ margin: 0 }}>
                                                    {r}
                                                </Tag>
                                            ))}
                                        </div>
                                    )}
                                    <div
                                        style={{
                                            display: 'flex',
                                            alignItems: 'center',
                                            justifyContent: 'space-between',
                                            gap: 8,
                                            marginTop: 6,
                                            flexWrap: 'wrap',
                                        }}
                                    >
                                        <div className="return-pending-item__time">
                                            Lập lúc {formatDateTime(item.processedAt)} - {item.returnedByName || 'Lâm sàng'}
                                        </div>
                                        {canApprove && (
                                            <Button
                                                size="small"
                                                type={item.requiresMandatoryDiscard ? 'default' : 'primary'}
                                                danger={item.requiresMandatoryDiscard}
                                                ghost={!item.requiresMandatoryDiscard}
                                                icon={<ScanOutlined />}
                                                loading={scanning}
                                                onClick={() => void handleScan(item.barcode)}
                                            >
                                                {item.requiresMandatoryDiscard ? 'Mở & chuyển hủy' : 'Kiểm định'}
                                            </Button>
                                        )}
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
                </SectionCard>
            </div>
        </PageContainer>
    )
}
