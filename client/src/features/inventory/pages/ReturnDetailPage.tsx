import { useEffect, useMemo, useState } from 'react'
import { Alert, App, Button, Checkbox, Input, Popconfirm, Result, Select, Spin, Tag } from 'antd'
import {
    ArrowLeftOutlined,
    CheckOutlined,
    CloseOutlined,
    DeleteOutlined,
    WarningOutlined,
} from '@ant-design/icons'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { formatDateTime } from '@/shared/formatters/date'
import {
    BloodGroupTag,
    PageContainer,
    PageHeader,
    SectionCard,
    StatusPill,
} from '@/shared/components'
import { BagStatusTag } from '../components/StockBadges'
import { useStorageLocations } from '../hooks'
import { storageZoneMeta } from '../stockFormat'
import { returnsApi } from '../returnsApi'
import { useProcessBloodReturn } from '../returnsHooks'
import type {
    BloodReturnInspectionChecklistDto,
    ProcessBloodReturnResponse,
    ReturnInspectionDto,
} from '../returnsTypes'
import '@/shared/styles/HandoverLayout.css'
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

function getChecklistItems(
    inspection: ReturnInspectionDto,
): { key: keyof BloodReturnInspectionChecklistDto; label: string }[] {
    const tempRangeText =
        inspection.storageTemperatureLabel || `${inspection.minTemp}°C đến ${inspection.maxTemp}°C`
    const outsideLimitText =
        inspection.maxOutsideMinutesForRestock != null
            ? `Thời gian ngoài thiết bị bảo quản ≤ ${inspection.maxOutsideMinutesForRestock} phút`
            : 'Thời gian ngoài thiết bị bảo quản hợp lệ (không giới hạn phút theo cấu hình)'
    const elapsedText =
        inspection.minutesSinceIssued != null
            ? ` (đã xuất kho khoảng ${Math.round(inspection.minutesSinceIssued)} phút)`
            : ''

    return [
        { key: 'isBagIntact', label: 'Túi máu vẫn còn nguyên vẹn, không rò rỉ' },
        { key: 'isLabelClear', label: 'Nhãn nhận diện còn rõ ràng, không bị xóa' },
        { key: 'isTempInRange', label: `Nhiệt độ bảo quản trong giới hạn (${tempRangeText})` },
        { key: 'isOutsideTimeValid', label: `${outsideLimitText}${elapsedText}` },
        { key: 'isUnusedFullVolume', label: 'Chưa được sử dụng một phần (toàn bộ thể tích)' },
    ]
}

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
    const matchedStateInspection =
        state?.inspection &&
        state.inspection.barcode.toLowerCase() === decodedBarcode.toLowerCase()
            ? state.inspection
            : null

    const [fetchedInspection, setFetchedInspection] = useState<ReturnInspectionDto | null>(null)
    const [loading, setLoading] = useState<boolean>(
        () => !matchedStateInspection && Boolean(decodedBarcode),
    )
    const [loadError, setLoadError] = useState<string | null>(() =>
        decodedBarcode ? null : 'Không có mã barcode túi máu hoàn trả để tra cứu.',
    )

    const [nowMs] = useState(() => Date.now())
    const [checklist, setChecklist] = useState<BloodReturnInspectionChecklistDto>(DEFAULT_CHECKLIST)
    const [selectedRestockLocationId, setSelectedRestockLocationId] = useState<string | undefined>(
        undefined,
    )
    const [selectedDiscardLocationId, setSelectedDiscardLocationId] = useState<string | undefined>(
        undefined,
    )
    const [discardReason, setDiscardReason] = useState<string>('')
    const [submitError, setSubmitError] = useState<string | null>(null)
    const [result, setResult] = useState<ProcessBloodReturnResponse | null>(null)

    const inspection = matchedStateInspection ?? fetchedInspection

    const { data: locationsPage, isLoading: isLocationsLoading } = useStorageLocations()

    const activeLocations = useMemo(
        () => (locationsPage?.items || []).filter((loc) => loc.isActive && !loc.isFull),
        [locationsPage],
    )

    const suitableReleasedLocations = useMemo(() => {
        const released = activeLocations.filter((loc) => loc.zone === 'RELEASED')
        if (!inspection) return released
        return released.filter(
            (loc) =>
                loc.targetTemperature >= inspection.minTemp &&
                loc.targetTemperature <= inspection.maxTemp,
        )
    }, [activeLocations, inspection])

    const disposalHoldLocations = useMemo(
        () => activeLocations.filter((loc) => loc.zone === 'DISPOSAL_HOLD'),
        [activeLocations],
    )

    const defaultRestockLocationId = useMemo(() => {
        if (!inspection) return undefined
        if (
            inspection.previousLocationId &&
            suitableReleasedLocations.some((l) => l.locationId === inspection.previousLocationId)
        ) {
            return inspection.previousLocationId
        }
        return suitableReleasedLocations[0]?.locationId
    }, [inspection, suitableReleasedLocations])

    const effectiveRestockLocationId = selectedRestockLocationId ?? defaultRestockLocationId

    useEffect(() => {
        if (matchedStateInspection || !decodedBarcode) return

        let cancelled = false
        returnsApi
            .scan(decodedBarcode)
            .then((res) => {
                if (!cancelled) {
                    setFetchedInspection(res)
                    setLoadError(null)
                }
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
    }, [decodedBarcode, matchedStateInspection])

    const checklistItems = useMemo(
        () => (inspection ? getChecklistItems(inspection) : []),
        [inspection],
    )

    const allChecked =
        checklistItems.length > 0 && checklistItems.every((item) => checklist[item.key])

    const isExpired = Boolean(
        inspection && (inspection.isExpired || new Date(inspection.expiredAt).getTime() <= nowMs),
    )

    const restockBlockedByPolicy = Boolean(
        inspection &&
            (isExpired ||
                !inspection.allowRestock ||
                inspection.previousRestockCount >= inspection.maxRestockCount ||
                inspection.requiresMandatoryDiscard),
    )

    const canRestock = Boolean(
        inspection &&
            !result &&
            canApprove &&
            !restockBlockedByPolicy &&
            allChecked &&
            effectiveRestockLocationId &&
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
                    newStorageLocationId:
                        action === 'RESTOCK'
                            ? effectiveRestockLocationId
                            : selectedDiscardLocationId,
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

    const handleGoToDisposal = (barcode: string) => {
        navigate(`/inventory/disposals?barcode=${encodeURIComponent(barcode)}&reason=DAMAGED_BAG`)
    }

    return (
        <PageContainer>
            <PageHeader
                breadcrumb={[
                    { title: 'Túi máu & Kho' },
                    { title: 'Tiếp nhận hoàn trả', path: '/inventory/returns' },
                    { title: 'Thông tin túi máu trả' },
                ]}
                backTo="/inventory/returns"
                title="Tiếp nhận hoàn trả túi máu"
                subtitle="Kiểm định điều kiện bảo quản và quyết định tái nhập kho sạch hoặc chuyển khu chờ tiêu hủy."
                extra={
                    <StatusPill
                        tone={
                            result
                                ? result.finalAction === 'RESTOCK'
                                    ? 'success'
                                    : 'warning'
                                : restockBlockedByPolicy
                                  ? 'danger'
                                  : 'warning'
                        }
                        pulse={!result}
                    >
                        {result
                            ? result.finalAction === 'RESTOCK'
                                ? 'Đã tái nhập kho'
                                : 'Chờ lập biên bản hủy'
                            : restockBlockedByPolicy
                              ? 'Bắt buộc chuyển hủy'
                              : 'Chờ thẩm định BBNK'}
                    </StatusPill>
                }
            />

            {loading ? (
                <SectionCard bodyClassName="return-loading-body">
                    <Spin tip="Đang tải thông tin túi máu và phiếu hoàn trả..." />
                </SectionCard>
            ) : loadError || !inspection ? (
                <SectionCard>
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
                </SectionCard>
            ) : (
                <div className="return-detail-grid">
                    {/* Cột trái: Thông tin túi máu, cảnh báo, lý do lâm sàng & bảng kiểm */}
                    <SectionCard
                        title="Thông tin túi máu hoàn trả"
                        subtitle="Dữ liệu túi máu, cấu hình tái nhập của chế phẩm và cảnh báo từ khoa lâm sàng."
                        bodyClassName="return-stack"
                    >
                        <div className={`return-bag-pill ${restockBlockedByPolicy ? 'is-expired' : ''}`}>
                            <div className="return-bag-pill__left">
                                <BloodGroupTag value={inspection.bloodType} size="lg" />
                                <div>
                                    <div className="handover-bag-banner__code">{inspection.barcode}</div>
                                    <div className="handover-bag-banner__meta">
                                        {inspection.componentName} ({inspection.componentCode}) - {inspection.volumeMl} ml -{' '}
                                        {inspection.previousLocationName || 'Chưa ghi nhận tủ cũ'} - Hạn{' '}
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

                        {restockBlockedByPolicy && (
                            <Alert
                                type="error"
                                showIcon
                                icon={<WarningOutlined />}
                                message="Không đủ điều kiện tái nhập kho - Bắt buộc chuyển hủy"
                                description={
                                    <div style={{ display: 'grid', gap: 4, marginTop: 4 }}>
                                        {inspection.mandatoryDiscardReasons?.length > 0 ? (
                                            inspection.mandatoryDiscardReasons.map((reason, idx) => (
                                                <div key={`${reason}-${idx}`}>- {reason}</div>
                                            ))
                                        ) : (
                                            <div>
                                                Túi máu đã hết hạn, chế phẩm không cho phép tái nhập hoặc đã vượt số lần tái nhập tối đa.
                                            </div>
                                        )}
                                    </div>
                                }
                            />
                        )}

                        {inspection.clinicalChecklistWarnings?.length > 0 && (
                            <Alert
                                type="warning"
                                showIcon
                                message="Khoa Lâm sàng báo cáo có tiêu chí chưa đạt khi trả túi"
                                description={
                                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginTop: 6 }}>
                                        {inspection.clinicalChecklistWarnings.map((w, idx) => (
                                            <Tag key={`${w}-${idx}`} color="warning" style={{ margin: 0 }}>
                                                {w}
                                            </Tag>
                                        ))}
                                    </div>
                                }
                            />
                        )}

                        <div className="handover-detail-grid">
                            <div className="handover-kv">
                                <span>Mã phiếu hoàn trả</span>
                                <strong>{inspection.returnCode}</strong>
                            </div>
                            <div className="handover-kv">
                                <span>Trạng thái túi hiện tại</span>
                                <strong><BagStatusTag status={inspection.bagStatus} /></strong>
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
                            <div className="handover-kv">
                                <span>Chính sách tái nhập kho</span>
                                <strong>
                                    {inspection.allowRestock
                                        ? `Đã tái nhập ${inspection.previousRestockCount}/${inspection.maxRestockCount} lần`
                                        : 'Không cho phép tái nhập'}
                                </strong>
                            </div>
                            <div className="handover-kv">
                                <span>Dải nhiệt độ chế phẩm</span>
                                <strong>
                                    {inspection.storageTemperatureLabel} ({inspection.minTemp}°C đến {inspection.maxTemp}°C)
                                </strong>
                            </div>
                            <div className="handover-kv">
                                <span>Thời gian từ lúc xuất kho</span>
                                <strong>
                                    {inspection.minutesSinceIssued != null
                                        ? `${Math.round(inspection.minutesSinceIssued)} phút`
                                        : '—'}
                                    {inspection.maxOutsideMinutesForRestock != null
                                        ? ` (giới hạn ${inspection.maxOutsideMinutesForRestock} phút)`
                                        : ' (không giới hạn phút)'}
                                </strong>
                            </div>
                        </div>

                        <div>
                            <div className="return-field-label">
                                Lý do hoàn trả (từ đơn vị lâm sàng) <span className="return-field-req">*</span>
                            </div>
                            <div className="return-reason-box">{inspection.returnReason}</div>
                        </div>

                        <div>
                            <div className="return-field-label">
                                Kiểm tra điều kiện bảo quản tại Ngân hàng máu <span className="return-field-req">*</span>
                            </div>
                            <div className="return-checklist">
                                {checklistItems.map((item) => {
                                    const checked = checklist[item.key]
                                    return (
                                        <div
                                            key={item.key}
                                            className={`return-check-item ${checked ? 'is-checked' : ''}`}
                                        >
                                            <Checkbox
                                                style={{ width: '100%' }}
                                                checked={checked}
                                                disabled={
                                                    Boolean(result) ||
                                                    restockBlockedByPolicy ||
                                                    processMutation.isPending
                                                }
                                                onChange={(e) => handleToggleCheck(item.key, e.target.checked)}
                                            >
                                                <span className="return-check-item__label">{item.label}</span>
                                            </Checkbox>
                                        </div>
                                    )
                                })}
                            </div>
                        </div>

                        {!result && (
                            <div className="return-stack">
                                {!restockBlockedByPolicy && (
                                    <div>
                                        <label htmlFor="restock-location-select" className="return-field-label">
                                            Ngăn tủ Kho sạch khi tái nhập{' '}
                                            {inspection.previousLocationName
                                                ? `(Vị trí cũ: ${inspection.previousLocationName})`
                                                : ''}
                                        </label>
                                        <Select
                                            id="restock-location-select"
                                            size="large"
                                            style={{ width: '100%' }}
                                            loading={isLocationsLoading}
                                            disabled={processMutation.isPending}
                                            placeholder={`Chọn ngăn tủ Kho sạch (${inspection.minTemp}°C đến ${inspection.maxTemp}°C)...`}
                                            value={effectiveRestockLocationId}
                                            onChange={(val) => {
                                                setSelectedRestockLocationId(val)
                                                if (submitError) setSubmitError(null)
                                            }}
                                            options={suitableReleasedLocations.map((loc) => ({
                                                value: loc.locationId,
                                                label: `${loc.refrigeratorName} - ${loc.shelfNumber} (${loc.currentBagsCount} túi - ${loc.targetTemperature}°C - ${storageZoneMeta(loc.zone).shortLabel})`,
                                            }))}
                                        />
                                        {!isLocationsLoading && suitableReleasedLocations.length === 0 && (
                                            <Alert
                                                type="warning"
                                                showIcon
                                                style={{ marginTop: 8 }}
                                                message={`Chưa có ngăn thuộc Kho sạch phù hợp dải nhiệt độ ${inspection.minTemp}°C đến ${inspection.maxTemp}°C.`}
                                            />
                                        )}
                                    </div>
                                )}

                                <div>
                                    <label htmlFor="discard-location-select" className="return-field-label">
                                        Ngăn Khu chờ hủy nếu chuyển tiêu hủy (tùy chọn)
                                    </label>
                                    <Select
                                        id="discard-location-select"
                                        allowClear
                                        style={{ width: '100%' }}
                                        loading={isLocationsLoading}
                                        disabled={processMutation.isPending}
                                        placeholder="Tự động chọn ngăn Khu chờ hủy mặc định..."
                                        value={selectedDiscardLocationId}
                                        onChange={(val) => setSelectedDiscardLocationId(val)}
                                        options={disposalHoldLocations.map((loc) => ({
                                            value: loc.locationId,
                                            label: `${loc.refrigeratorName} - ${loc.shelfNumber} (${loc.currentBagsCount} túi - ${loc.targetTemperature}°C)`,
                                        }))}
                                    />
                                </div>

                                <div>
                                    <label htmlFor="discard-reason-input" className="return-field-label">
                                        Lý do từ chối / tiêu hủy (nếu chọn chuyển khu chờ hủy)
                                    </label>
                                    <Input
                                        id="discard-reason-input"
                                        maxLength={255}
                                        placeholder="Ví dụ: Vượt thời gian ngoài tủ cho phép, rách nhãn hoặc chế phẩm không tái nhập..."
                                        value={discardReason}
                                        disabled={processMutation.isPending}
                                        onChange={(e) => setDiscardReason(e.target.value)}
                                    />
                                </div>
                            </div>
                        )}
                    </SectionCard>

                    {/* Cột phải: Quyết định tiếp nhận & kết quả */}
                    <SectionCard
                        title="Quyết định tiếp nhận"
                        subtitle="Dựa trên cấu hình chế phẩm và kết quả kiểm tra bảo quản để chọn hướng xử lý phù hợp."
                        bodyClassName="return-stack"
                    >
                        {!result && (
                            <>
                                {restockBlockedByPolicy ? (
                                    <Alert
                                        type="error"
                                        showIcon
                                        title="Chỉ cho phép từ chối và chuyển khu chờ hủy"
                                        description="Túi máu không đáp ứng điều kiện chính sách (hết hạn, chế phẩm cấm tái nhập hoặc hết lượt tái nhập). Vui lòng bấm 'Từ chối - Chuyển khu chờ hủy'."
                                    />
                                ) : !allChecked ? (
                                    <Alert
                                        type="warning"
                                        showIcon
                                        title="Vui lòng xác nhận tất cả 5 điều kiện trước khi nhập lại kho."
                                    />
                                ) : !effectiveRestockLocationId ? (
                                    <Alert
                                        type="warning"
                                        showIcon
                                        title="Vui lòng chọn ngăn tủ Kho sạch phù hợp để tái nhập kho."
                                    />
                                ) : (
                                    <Alert
                                        type="success"
                                        showIcon
                                        title="Đạt đủ điều kiện tái nhập kho"
                                        description="Có thể chấp nhận tái nhập kho sạch để sẵn sàng cấp phát lại."
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
                                title="Xử lý hoàn trả thất bại"
                                description={submitError}
                            />
                        )}

                        {result ? (
                            <div className="return-stack">
                                <Alert
                                    type={result.finalAction === 'RESTOCK' ? 'success' : 'warning'}
                                    showIcon
                                    title={
                                        result.finalAction === 'RESTOCK'
                                            ? 'Đã tái nhập kho thành công'
                                            : 'Đã từ chối tái nhập - Chuyển sang khu chờ tiêu hủy'
                                    }
                                    description={result.message}
                                />

                                <div className="handover-detail-box">
                                    <div className="handover-kv">
                                        <span>Quyết định xử lý</span>
                                        <strong>
                                            <StatusPill tone={result.finalAction === 'RESTOCK' ? 'success' : 'danger'}>
                                                {result.finalAction === 'RESTOCK' ? 'Tái nhập kho' : 'Chuyển khu chờ hủy'}
                                            </StatusPill>
                                        </strong>
                                    </div>
                                    <div className="handover-kv">
                                        <span>Trạng thái túi mới</span>
                                        <strong><BagStatusTag status={result.bagStatus} /></strong>
                                    </div>
                                    <div className="handover-kv">
                                        <span>Trạng thái phân bổ</span>
                                        <strong>{result.allocationStatus === 'RETURNED' ? 'Đã hoàn trả' : result.allocationStatus}</strong>
                                    </div>
                                    {result.newLocationName && (
                                        <div className="handover-kv">
                                            <span>Vị trí kho mới</span>
                                            <strong>{result.newLocationName}</strong>
                                        </div>
                                    )}
                                    {result.discardReason && (
                                        <div className="handover-kv">
                                            <span>Lý do chuyển hủy</span>
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

                                {result.finalAction === 'DISCARD' && (
                                    <Button
                                        danger
                                        type="primary"
                                        size="large"
                                        icon={<DeleteOutlined />}
                                        onClick={() => handleGoToDisposal(result.barcode)}
                                    >
                                        Mở màn hình Lập biên bản tiêu hủy
                                    </Button>
                                )}

                                <Button
                                    type={result.finalAction === 'RESTOCK' ? 'primary' : 'default'}
                                    size="large"
                                    icon={<ArrowLeftOutlined />}
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
                                        size="middle"
                                        icon={<CheckOutlined />}
                                        loading={processMutation.isPending}
                                        disabled={!canRestock}
                                        onClick={() => void handleProcess('RESTOCK')}
                                    >
                                        Chấp nhận nhập lại kho
                                    </Button>

                                    <Popconfirm
                                        title="Xác nhận từ chối tái nhập và chuyển khu chờ hủy?"
                                        description={`Túi máu ${inspection.barcode} sẽ được giữ khóa và chuyển vào khu chờ tiêu hủy để lập biên bản tiêu hủy.`}
                                        okText="Xác nhận chuyển hủy"
                                        cancelText="Hủy bỏ"
                                        okButtonProps={{ danger: true, loading: processMutation.isPending }}
                                        disabled={!canDiscard}
                                        onConfirm={() => void handleProcess('DISCARD')}
                                    >
                                        <Button
                                            type="primary"
                                            danger
                                            size="middle"
                                            icon={<CloseOutlined />}
                                            loading={processMutation.isPending}
                                            disabled={!canDiscard}
                                        >
                                            Từ chối - Chuyển khu chờ hủy
                                        </Button>
                                    </Popconfirm>
                                </div>

                                <p className="return-hint">
                                    * Nút &quot;Chấp nhận nhập lại kho&quot; chỉ khả dụng khi chế phẩm được phép tái nhập, chưa vượt số lần quy định, đạt đủ 5 điều kiện, túi chưa hết hạn và chọn được ngăn thuộc Kho sạch.
                                </p>

                                <div className="return-policy-box">
                                    <div className="return-policy-box__title">Quy trình xử lý sau kiểm định</div>
                                    <p>
                                        Túi đạt chuẩn được đưa về Kho sạch để sẵn sàng cấp phát lại. Túi bị từ chối được giữ khóa tại khu chờ tiêu hủy để lập biên bản đề xuất hủy và phê duyệt tiêu hủy vật lý.
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
                    </SectionCard>
                </div>
            )}
        </PageContainer>
    )
}
