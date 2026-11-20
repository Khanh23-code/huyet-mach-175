import { useEffect, useMemo, useState } from 'react'
import { Alert, App, Button, Checkbox, Input, Popconfirm, Result, Select, Spin, Tag } from 'antd'
import { ArrowLeftOutlined, CheckOutlined, CloseOutlined } from '@ant-design/icons'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { formatDateTime } from '@/shared/formatters/date'
import { useStorageLocations } from '../hooks'
import { returnsApi } from '../returnsApi'
import { useProcessBloodReturn } from '../returnsHooks'
import type {
    BloodReturnInspectionChecklistDto,
    ProcessBloodReturnResponse,
    ReturnInspectionDto,
} from '../returnsTypes'
import '@/features/coordination/pages/HandoverPages.css'
import './ReturnPages.css'

interface LocationState {
    inspection?: ReturnInspectionDto
}

const DEFAULT_CHECKLIST: BloodReturnInspectionChecklistDto = {
    isBagIntact: false,
    isLabelClear: false,
    isTempInRange: false,
    isOutsideTimeValid: false,
    isUnusedFullVolume: false,
}

const CHECKLIST_ITEMS: { key: keyof BloodReturnInspectionChecklistDto; label: string }[] = [
    { key: 'isBagIntact', label: 'Túi máu vẫn còn nguyên vẹn, không rò rỉ' },
    { key: 'isLabelClear', label: 'Nhãn nhận diện còn rõ ràng, không bị xóa' },
    { key: 'isTempInRange', label: 'Nhiệt độ bảo quản trong giới hạn (2–6 °C)' },
    { key: 'isOutsideTimeValid', label: 'Thời gian ngoài thiết bị bảo quản ≤ 30 phút' },
    { key: 'isUnusedFullVolume', label: 'Chưa được sử dụng một phần (toàn bộ thể tích)' },
]

export default function ReturnDetailPage() {
    const { message } = App.useApp()
    const navigate = useNavigate()
    const { barcode: rawParam } = useParams<{ barcode: string }>()
    const location = useLocation()
    const state = (location.state as LocationState | null) ?? null

    const { can } = usePermission()
    const canApprove = can(PERMISSIONS.BLOOD_RETURN_APPROVE)
    const processMutation = useProcessBloodReturn()

    const decodedBarcode = rawParam ? decodeURIComponent(rawParam) : ''

    const [inspection, setInspection] = useState<ReturnInspectionDto | null>(state?.inspection ?? null)
    const [loading, setLoading] = useState<boolean>(!state?.inspection)
    const [loadError, setLoadError] = useState<string | null>(null)

    const [checklist, setChecklist] = useState<BloodReturnInspectionChecklistDto>(DEFAULT_CHECKLIST)
    const [selectedLocationId, setSelectedLocationId] = useState<number | undefined>(undefined)
    const [discardReason, setDiscardReason] = useState<string>('')
    const [submitError, setSubmitError] = useState<string | null>(null)
    const [result, setResult] = useState<ProcessBloodReturnResponse | null>(null)

    const { data: locationsPage, isLoading: isLocationsLoading } = useStorageLocations()
    const activeAvailableLocations = useMemo(
        () => (locationsPage?.items || []).filter((loc) => loc.isActive && !loc.isFull),
        [locationsPage],
    )

    useEffect(() => {
        if (
            state?.inspection &&
            state.inspection.barcode.toLowerCase() === decodedBarcode.toLowerCase()
        ) {
            setInspection(state.inspection)
            setLoading(false)
            setLoadError(null)
            return
        }

        if (!decodedBarcode) {
            setLoading(false)
            setLoadError('Không có mã barcode túi máu hoàn trả để tra cứu.')
            return
        }

        let cancelled = false
        setLoading(true)
        setLoadError(null)

        returnsApi
            .scan(decodedBarcode)
            .then((res) => {
                if (!cancelled) setInspection(res)
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
    }, [decodedBarcode, state])

    // Gợi ý sẵn vị trí lưu cũ nếu tủ vẫn đang hoạt động và chưa đầy
    useEffect(() => {
        if (!inspection || selectedLocationId !== undefined) return
        if (
            inspection.previousLocationId &&
            activeAvailableLocations.some((l) => l.locationId === inspection.previousLocationId)
        ) {
            setSelectedLocationId(inspection.previousLocationId)
        } else if (activeAvailableLocations.length > 0 && !inspection.previousLocationId) {
            setSelectedLocationId(activeAvailableLocations[0].locationId)
        }
    }, [inspection, activeAvailableLocations, selectedLocationId])

    const allChecked = CHECKLIST_ITEMS.every((item) => checklist[item.key])
    const isExpired = Boolean(
        inspection && (inspection.isExpired || new Date(inspection.expiredAt).getTime() <= Date.now()),
    )
    const canRestock = Boolean(
        inspection &&
            !result &&
            canApprove &&
            !isExpired &&
            allChecked &&
            selectedLocationId &&
            !processMutation.isPending,
    )
    const canDiscard = Boolean(
        inspection && !result && canApprove && !processMutation.isPending,
    )

    const handleToggleCheck = (key: keyof BloodReturnInspectionChecklistDto, checked: boolean) => {
        setChecklist((prev) => ({ ...prev, [key]: checked }))
        if (submitError) setSubmitError(null)
    }

    const handleProcess = async (action: 'RESTOCK' | 'DISCARD') => {
        if (!inspection || result || processMutation.isPending) return
        if (action === 'RESTOCK' && !canRestock) return

        setSubmitError(null)
        try {
            const res = await processMutation.mutateAsync({
                returnId: inspection.returnId,
                body: {
                    action,
                    checklist,
                    newStorageLocationId: action === 'RESTOCK' ? selectedLocationId : undefined,
                    discardReason:
                        action === 'DISCARD' && discardReason.trim()
                            ? discardReason.trim()
                            : undefined,
                },
            })
            setResult(res)
            message.success(res.message)
        } catch (err) {
            const msg = parseApiError(err).message
            setSubmitError(msg)
            message.error(msg)
        }
    }

    const handleBackToScan = () => {
        navigate('/inventory/returns')
    }

    return (
        <div className="return-page">
            <div className="handover-head">
                <div>
                    <div className="handover-breadcrumb">Túi máu & Kho / Tiếp nhận hoàn trả / Thông tin túi máu trả</div>
                    <h1 className="handover-title">Tiếp nhận hoàn trả túi máu</h1>
                    <p className="handover-subtitle">
                        Kiểm tra điều kiện bảo quản và quyết định nhập lại kho hoặc chuyển sang hủy.
                    </p>
                </div>
                <span className={`handover-status-pill ${result ? 'is-success' : ''}`}>
                    <i />{' '}
                    {result
                        ? result.finalAction === 'RESTOCK'
                            ? 'Đã tái nhập kho (AVAILABLE)'
                            : 'Đã chuyển hủy (DISCARDED)'
                        : 'Chờ thẩm định BBNK'}
                </span>
            </div>

            {loading ? (
                <section className="return-card" style={{ padding: 48, textAlign: 'center' }}>
                    <Spin tip="Đang tải thông tin túi máu và phiếu hoàn trả..." />
                </section>
            ) : loadError || !inspection ? (
                <section className="return-card">
                    <Result
                        status="warning"
                        title="Không thể tra cứu túi máu hoàn trả"
                        subTitle={loadError || 'Vui lòng quay lại màn hình quét barcode để kiểm tra lại.'}
                        extra={
                            <Button type="primary" icon={<ArrowLeftOutlined />} onClick={handleBackToScan}>
                                Quay lại màn hình quét
                            </Button>
                        }
                    />
                </section>
            ) : (
                <div className="return-detail-grid">
                    {/* Cột trái: Thông tin túi máu, lý do hoàn trả từ lâm sàng & bảng kiểm 5 điều kiện */}
                    <section className="return-card">
                        <div>
                            <h2 className="return-card__title">Thông tin túi máu hoàn trả</h2>
                            <p className="return-card__sub">
                                Dữ liệu túi máu và phiếu hoàn trả được tra cứu trực tiếp từ hệ thống.
                            </p>
                        </div>

                        <div className={`return-bag-pill ${isExpired ? 'is-expired' : ''}`}>
                            <div className="return-bag-pill__left">
                                <div className="handover-blood-badge">{inspection.bloodType}</div>
                                <div>
                                    <div className="handover-bag-banner__code">{inspection.barcode}</div>
                                    <div className="handover-bag-banner__meta">
                                        {inspection.componentName} · {inspection.volumeMl} ml ·{' '}
                                        {inspection.previousLocationName || 'Chưa ghi nhận tủ cũ'} · Hạn{' '}
                                        {formatDateTime(inspection.expiredAt)}
                                    </div>
                                </div>
                            </div>
                            <Button
                                size="small"
                                onClick={handleBackToScan}
                                disabled={processMutation.isPending}
                            >
                                Đổi
                            </Button>
                        </div>

                        <div className="handover-detail-grid">
                            <div className="handover-kv">
                                <span>Mã phiếu hoàn trả</span>
                                <strong>#{inspection.returnId} ({inspection.finalAction})</strong>
                            </div>
                            <div className="handover-kv">
                                <span>Trạng thái túi hiện tại</span>
                                <strong><Tag color="warning">{inspection.bagStatus}</Tag></strong>
                            </div>
                            <div className="handover-kv">
                                <span>Phiếu yêu cầu liên quan</span>
                                <strong>{inspection.requestCode}</strong>
                            </div>
                            <div className="handover-kv">
                                <span>Khoa lâm sàng</span>
                                <strong>{inspection.departmentName || '—'}</strong>
                            </div>
                            <div className="handover-kv">
                                <span>Người bệnh</span>
                                <strong>{inspection.patientName} ({inspection.patientCode})</strong>
                            </div>
                            <div className="handover-kv">
                                <span>Bác sĩ chỉ định</span>
                                <strong>{inspection.doctorName || '—'}</strong>
                            </div>
                            <div className="handover-kv">
                                <span>Người lập hoàn trả</span>
                                <strong>{inspection.returnedByName || '—'}</strong>
                            </div>
                            <div className="handover-kv">
                                <span>Thời gian hoàn trả</span>
                                <strong>{formatDateTime(inspection.returnedAt)}</strong>
                            </div>
                        </div>

                        <div>
                            <div style={{ fontWeight: 600, fontSize: 13, marginBottom: 6, color: '#0f172a' }}>
                                Lý do hoàn trả (từ đơn vị lâm sàng) <span style={{ color: '#dc2626' }}>*</span>
                            </div>
                            <div className="return-reason-box">{inspection.returnReason}</div>
                        </div>

                        <div>
                            <div style={{ fontWeight: 600, fontSize: 13, marginBottom: 8, color: '#0f172a' }}>
                                Kiểm tra điều kiện bảo quản <span style={{ color: '#dc2626' }}>*</span>
                            </div>
                            <div className="return-checklist">
                                {CHECKLIST_ITEMS.map((item) => {
                                    const checked = checklist[item.key]
                                    return (
                                        <div
                                            key={item.key}
                                            className={`return-check-item ${checked ? 'is-checked' : ''}`}
                                        >
                                            <Checkbox
                                                checked={checked}
                                                disabled={Boolean(result) || processMutation.isPending}
                                                onChange={(e) => handleToggleCheck(item.key, e.target.checked)}
                                            >
                                                {item.label}
                                            </Checkbox>
                                        </div>
                                    )
                                })}
                            </div>
                        </div>

                        {!result && (
                            <div style={{ display: 'grid', gap: 14 }}>
                                <div>
                                    <label
                                        htmlFor="restock-location-select"
                                        style={{ display: 'block', fontWeight: 600, fontSize: 13, marginBottom: 6 }}
                                    >
                                        Ngăn tủ lưu trữ khi nhập lại kho{' '}
                                        {inspection.previousLocationName ? `(Vị trí cũ: ${inspection.previousLocationName})` : ''}
                                    </label>
                                    <Select
                                        id="restock-location-select"
                                        size="large"
                                        style={{ width: '100%' }}
                                        loading={isLocationsLoading}
                                        disabled={isExpired || processMutation.isPending}
                                        placeholder="Chọn ngăn tủ còn chỗ để tái nhập kho..."
                                        value={selectedLocationId}
                                        onChange={(val) => {
                                            setSelectedLocationId(val)
                                            if (submitError) setSubmitError(null)
                                        }}
                                        options={activeAvailableLocations.map((loc) => ({
                                            value: loc.locationId,
                                            label: `${loc.refrigeratorName} - ${loc.shelfNumber} (${loc.currentBagsCount} túi · ${loc.targetTemperature}°C)`,
                                        }))}
                                    />
                                </div>

                                <div>
                                    <label
                                        htmlFor="discard-reason-input"
                                        style={{ display: 'block', fontWeight: 600, fontSize: 13, marginBottom: 6 }}
                                    >
                                        Lý do từ chối / tiêu hủy (nếu chọn chuyển sang hủy)
                                    </label>
                                    <Input
                                        id="discard-reason-input"
                                        maxLength={255}
                                        placeholder="Ví dụ: Túi máu quá thời gian 30 phút ngoài tủ lạnh hoặc rách nhãn..."
                                        value={discardReason}
                                        disabled={processMutation.isPending}
                                        onChange={(e) => setDiscardReason(e.target.value)}
                                    />
                                </div>
                            </div>
                        )}
                    </section>

                    {/* Cột phải: Quyết định tiếp nhận & kết quả */}
                    <aside className="return-card">
                        <div>
                            <h2 className="return-card__title">Quyết định tiếp nhận</h2>
                            <p className="return-card__sub">
                                Dựa trên kết quả kiểm tra bảo quản, chọn hành động phù hợp với túi máu hoàn trả.
                            </p>
                        </div>

                        {!result && (
                            <>
                                {isExpired ? (
                                    <Alert
                                        type="error"
                                        showIcon
                                        message="Túi máu đã hết hạn sử dụng"
                                        description="Không thể tái nhập kho túi máu đã hết hạn. Bắt buộc chọn 'Từ chối — Chuyển sang hủy'."
                                    />
                                ) : !allChecked ? (
                                    <Alert
                                        type="warning"
                                        showIcon
                                        message="Vui lòng xác nhận tất cả 5 điều kiện trước khi nhập lại kho."
                                    />
                                ) : !selectedLocationId ? (
                                    <Alert
                                        type="warning"
                                        showIcon
                                        message="Vui lòng chọn ngăn tủ lưu trữ để tái nhập kho."
                                    />
                                ) : (
                                    <Alert
                                        type="success"
                                        showIcon
                                        message="Đạt đủ 5 điều kiện bảo quản"
                                        description="Có thể chấp nhận tái nhập kho về trạng thái AVAILABLE."
                                    />
                                )}
                            </>
                        )}

                        {submitError && (
                            <Alert
                                type="error"
                                showIcon
                                closable
                                onClose={() => setSubmitError(null)}
                                message="Xử lý hoàn trả thất bại"
                                description={submitError}
                            />
                        )}

                        {result ? (
                            <div style={{ display: 'grid', gap: 12 }}>
                                <Alert
                                    type={result.finalAction === 'RESTOCK' ? 'success' : 'warning'}
                                    showIcon
                                    message={
                                        result.finalAction === 'RESTOCK'
                                            ? 'Đã tái nhập kho thành công'
                                            : 'Đã chuyển túi máu sang tiêu hủy'
                                    }
                                    description={result.message}
                                />

                                <div className="handover-detail-box" style={{ padding: 14 }}>
                                    <div className="handover-kv">
                                        <span>Quyết định xử lý</span>
                                        <strong>
                                            <Tag color={result.finalAction === 'RESTOCK' ? 'green' : 'red'}>
                                                {result.finalAction}
                                            </Tag>
                                        </strong>
                                    </div>
                                    <div className="handover-kv">
                                        <span>Trạng thái túi mới</span>
                                        <strong>{result.bagStatus}</strong>
                                    </div>
                                    <div className="handover-kv">
                                        <span>Trạng thái phân bổ</span>
                                        <strong>{result.allocationStatus}</strong>
                                    </div>
                                    {result.newLocationName && (
                                        <div className="handover-kv">
                                            <span>Vị trí kho mới</span>
                                            <strong>{result.newLocationName}</strong>
                                        </div>
                                    )}
                                    {result.discardReason && (
                                        <div className="handover-kv">
                                            <span>Lý do hủy</span>
                                            <strong>{result.discardReason}</strong>
                                        </div>
                                    )}
                                    <div className="handover-kv">
                                        <span>Người thẩm định</span>
                                        <strong>{result.inspectedByName || '—'}</strong>
                                    </div>
                                    <div className="handover-kv" style={{ borderBottom: 'none' }}>
                                        <span>Thời gian xử lý</span>
                                        <strong>{formatDateTime(result.processedAt)}</strong>
                                    </div>
                                </div>

                                <Button
                                    type="primary"
                                    size="large"
                                    icon={<ArrowLeftOutlined />}
                                    className="handover-btn-primary"
                                    onClick={handleBackToScan}
                                >
                                    Quét túi máu trả tiếp theo
                                </Button>
                            </div>
                        ) : (
                            <>
                                <div className="return-decision-actions">
                                    <Button
                                        type="primary"
                                        icon={<CheckOutlined />}
                                        className="return-btn-restock"
                                        loading={processMutation.isPending}
                                        disabled={!canRestock}
                                        onClick={() => void handleProcess('RESTOCK')}
                                    >
                                        Chấp nhận nhập lại kho
                                    </Button>

                                    <Popconfirm
                                        title="Xác nhận chuyển túi máu sang tiêu hủy?"
                                        description={`Túi máu ${inspection.barcode} sẽ chuyển sang trạng thái DISCARDED và không thể hoàn tác.`}
                                        okText="Xác nhận hủy"
                                        cancelText="Hủy bỏ"
                                        okButtonProps={{ danger: true, loading: processMutation.isPending }}
                                        disabled={!canDiscard}
                                        onConfirm={() => void handleProcess('DISCARD')}
                                    >
                                        <Button
                                            type="primary"
                                            danger
                                            icon={<CloseOutlined />}
                                            className="return-btn-discard"
                                            loading={processMutation.isPending}
                                            disabled={!canDiscard}
                                        >
                                            Từ chối — Chuyển sang hủy
                                        </Button>
                                    </Popconfirm>
                                </div>

                                <p style={{ margin: 0, fontSize: 12, color: '#64748b' }}>
                                    * Nút &quot;Chấp nhận nhập lại kho&quot; yêu cầu tất cả 5 điều kiện được xác nhận, túi chưa hết hạn và đã chọn ngăn tủ lưu trữ.
                                </p>

                                <div className="return-policy-box">
                                    <div className="return-policy-box__title">Tiêu chí nhập lại kho</div>
                                    <p>
                                        Túi máu chỉ được nhập lại kho nếu toàn bộ điều kiện bảo quản được đáp ứng. Bất kỳ vi phạm nào đều dẫn đến yêu cầu hủy bắt buộc theo quy định của Bộ Y tế.
                                    </p>
                                </div>

                                <Button
                                    icon={<ArrowLeftOutlined />}
                                    onClick={handleBackToScan}
                                    disabled={processMutation.isPending}
                                >
                                    Quay lại quét túi hoàn trả
                                </Button>
                            </>
                        )}
                    </aside>
                </div>
            )}
        </div>
    )
}
