import { useCallback, useMemo, useState } from 'react'
import { Alert, Button, Checkbox, Empty, Input, Modal, Select, Table, Tag, message } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
    CheckCircleOutlined,
    CloseCircleOutlined,
    InfoCircleOutlined,
    ReloadOutlined,
    RollbackOutlined,
    WarningOutlined,
} from '@ant-design/icons'
import { BloodGroupTag, SectionCard, StatusPill } from '@/shared/components'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { formatDateTime } from '@/shared/formatters/date'
import { parseApiError } from '@/services/apiError'
import { useCancelReturn, useCreateReturn, useEligibleReturnBags, usePendingBloodReturns } from '../hooks'
import type { CreateBloodReturnResponse, IneligibleReturnBag, PendingBloodReturnDto } from '../types'
import './ClinicalReturnCreatePanel.css'

const PRESET_REASONS = [
    'Người bệnh hoãn phẫu thuật / thủ thuật',
    'Thay đổi phác đồ điều trị, chưa sử dụng',
    'Chỉ định dư so với nhu cầu thực tế',
    'Người bệnh chuyển khoa / chuyển viện trước khi truyền',
    'OTHER',
]

export function ClinicalReturnCreatePanel() {
    const { can } = usePermission()
    const canEditReturns = can(PERMISSIONS.BLOOD_RETURN_EDIT)
    const canViewReturns = can(PERMISSIONS.BLOOD_RETURN_VIEW)

    const {
        data: returnBagPools,
        isLoading: isEligibleLoading,
        isFetching: isEligibleFetching,
        refetch: refetchEligible,
    } = useEligibleReturnBags()

    const {
        data: pendingReturnsPage,
        isLoading: isPendingReturnsLoading,
        isFetching: isPendingReturnsFetching,
        refetch: refetchReturns,
    } = usePendingBloodReturns({ pageNumber: 1, pageSize: 20 }, { enabled: canViewReturns })

    const createReturnMutation = useCreateReturn()
    const cancelReturnMutation = useCancelReturn()

    const eligibleBags = useMemo(() => returnBagPools?.eligibleBags ?? [], [returnBagPools])
    const ineligibleBags = useMemo(() => returnBagPools?.ineligibleBags ?? [], [returnBagPools])

    const [selectedAllocationId, setSelectedAllocationId] = useState<number | undefined>()
    const [reasonOption, setReasonOption] = useState<string | undefined>()
    const [customReason, setCustomReason] = useState('')
    const [checklist, setChecklist] = useState({
        isUnusedFullVolume: false,
        isBagIntact: false,
        isLabelClear: false,
        isStoredUnder30Mins: false,
    })
    const [submitError, setSubmitError] = useState<string | null>(null)
    const [createdReturn, setCreatedReturn] = useState<CreateBloodReturnResponse | null>(null)
    const [cancellingReturn, setCancellingReturn] = useState<PendingBloodReturnDto | null>(null)
    const [cancelReason, setCancelReason] = useState('')

    const selectedBag = useMemo(
        () => eligibleBags.find((b) => b.allocationId === selectedAllocationId) ?? null,
        [eligibleBags, selectedAllocationId],
    )

    const resolvedReason = useMemo(() => {
        if (!reasonOption) return ''
        if (reasonOption === 'OTHER') return customReason.trim()
        return reasonOption.trim()
    }, [reasonOption, customReason])

    const allChecked =
        checklist.isUnusedFullVolume &&
        checklist.isBagIntact &&
        checklist.isLabelClear &&
        checklist.isStoredUnder30Mins

    const handleResetForm = () => {
        setSelectedAllocationId(undefined)
        setReasonOption(undefined)
        setCustomReason('')
        setChecklist({
            isUnusedFullVolume: false,
            isBagIntact: false,
            isLabelClear: false,
            isStoredUnder30Mins: false,
        })
        setSubmitError(null)
    }

    const handleSubmit = async () => {
        if (createReturnMutation.isPending) return
        setSubmitError(null)

        if (!canEditReturns) {
            setSubmitError('Tài khoản của bạn không có quyền BLOOD_RETURN_EDIT để lập phiếu hoàn trả.')
            return
        }

        if (!selectedBag) {
            message.warning('Vui lòng chọn túi máu đủ điều kiện hoàn trả.')
            return
        }

        if (!resolvedReason) {
            message.warning('Vui lòng chọn hoặc nhập lý do hoàn trả.')
            return
        }

        if (resolvedReason.length > 255) {
            message.warning('Lý do hoàn trả không được vượt quá 255 ký tự.')
            return
        }

        try {
            const res = await createReturnMutation.mutateAsync({
                allocationId: selectedBag.allocationId,
                returnReason: resolvedReason,
                checklist,
            })
            setCreatedReturn(res)
            handleResetForm()
            void refetchEligible()
            if (canViewReturns) void refetchReturns()
        } catch (err) {
            setSubmitError(parseApiError(err).message)
        }
    }

    const handleConfirmCancelReturn = useCallback(async () => {
        if (!cancellingReturn || cancelReturnMutation.isPending) return

        const targetId = cancellingReturn.returnId
        try {
            await cancelReturnMutation.mutateAsync({
                returnId: targetId,
                data: {
                    cancelReason: cancelReason.trim() || undefined,
                },
            })
            if (createdReturn?.returnId === targetId) {
                setCreatedReturn(null)
            }
            setCancellingReturn(null)
            setCancelReason('')
            void refetchEligible()
            if (canViewReturns) void refetchReturns()
        } catch {
            // Đã hiển thị toast lỗi ở hook useCancelReturn
        }
    }, [
        cancellingReturn,
        cancelReason,
        cancelReturnMutation,
        canViewReturns,
        createdReturn?.returnId,
        refetchEligible,
        refetchReturns,
    ])

    const selectOptions = useMemo(() => {
        const eligibleOpts = eligibleBags.map((b) => ({
            value: b.allocationId,
            label: `${b.barcode} - ${b.componentName} (${b.bloodType}, ${b.volumeMl}ml) — ${b.patientName} [${b.requestCode}]`,
            disabled: false,
        }))
        const ineligibleOpts = ineligibleBags.map((b) => ({
            value: -b.allocationId,
            label: `[Không đủ điều kiện] ${b.barcode} - ${b.componentName} — ${b.ineligibleReason}`,
            disabled: true,
        }))
        return [...eligibleOpts, ...ineligibleOpts]
    }, [eligibleBags, ineligibleBags])

    const returnColumns: ColumnsType<PendingBloodReturnDto> = useMemo(
        () => [
            {
                title: 'MÃ PHIẾU / TÚI MÁU',
                key: 'info',
                render: (_, r) => (
                    <div>
                        <strong>Mã phiếu #{r.returnId}</strong>
                        <div>
                            <code className="handover-barcode">{r.barcode}</code>
                        </div>
                    </div>
                ),
            },
            {
                title: 'NGƯỜI BỆNH',
                key: 'patient',
                render: (_, r) => (
                    <div>
                        <div>
                            <strong>{r.patientName}</strong> ({r.patientCode})
                        </div>
                        <div style={{ color: 'var(--c-text-muted)', fontSize: 'var(--text-xs)' }}>
                            Phiếu yêu cầu: {r.requestCode}
                        </div>
                    </div>
                ),
            },
            {
                title: 'CHẾ PHẨM',
                key: 'product',
                render: (_, r) => (
                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start', gap: 4 }}>
                        <div>
                            {r.componentName} ({r.volumeMl} ml)
                        </div>
                        <BloodGroupTag value={r.bloodType} size="sm" />
                    </div>
                ),
            },
            {
                title: 'LÝ DO HOÀN TRẢ',
                dataIndex: 'returnReason',
                key: 'returnReason',
                render: (text) => <span style={{ color: 'var(--c-text-secondary)' }}>{text}</span>,
            },
            {
                title: 'TRẠNG THÁI XỬ LÝ',
                dataIndex: 'finalAction',
                key: 'status',
                align: 'center',
                render: (action, r) => (
                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4 }}>
                        <StatusPill tone="info">
                            {action === 'PENDING_INSPECTION' ? 'Chờ Ngân hàng máu kiểm định' : action}
                        </StatusPill>
                        {(r.requiresMandatoryDiscard || (r.clinicalChecklistWarnings?.length ?? 0) > 0) && (
                            <Tag color="warning" style={{ margin: 0 }}>
                                Gắn cờ bắt buộc tiêu hủy
                            </Tag>
                        )}
                    </div>
                ),
            },
            {
                title: 'THỜI GIAN LẬP',
                dataIndex: 'processedAt',
                key: 'processedAt',
                render: (val) => formatDateTime(val),
            },
            ...(canEditReturns
                ? [
                      {
                          title: 'THAO TÁC',
                          key: 'actions',
                          align: 'right' as const,
                          render: (_: unknown, r: PendingBloodReturnDto) =>
                              r.finalAction === 'PENDING_INSPECTION' ? (
                                  <Button
                                      danger
                                      size="small"
                                      icon={<CloseCircleOutlined />}
                                      onClick={() => {
                                          setCancellingReturn(r)
                                          setCancelReason('')
                                      }}
                                  >
                                      Hủy phiếu
                                  </Button>
                              ) : (
                                  <span style={{ color: 'var(--c-text-muted)' }}>—</span>
                              ),
                      },
                  ]
                : []),
        ],
        [canEditReturns],
    )

    const ineligibleColumns: ColumnsType<IneligibleReturnBag> = useMemo(
        () => [
            {
                title: 'MÃ TÚI',
                dataIndex: 'barcode',
                key: 'barcode',
                render: (code) => <code className="handover-barcode">{code}</code>,
            },
            {
                title: 'PHIẾU / NGƯỜI BỆNH',
                key: 'patient',
                render: (_, r) => (
                    <span>
                        {r.requestCode} - {r.patientName} ({r.patientCode})
                    </span>
                ),
            },
            {
                title: 'TRẠNG THÁI TÚI',
                key: 'bagStatus',
                render: (_, r) => (
                    <StatusPill tone="neutral" dot={false}>
                        {r.bagStatus || r.allocationStatus}
                    </StatusPill>
                ),
            },
            {
                title: 'LÝ DO KHÔNG ĐỦ ĐIỀU KIỆN',
                dataIndex: 'ineligibleReason',
                key: 'reason',
                render: (text) => (
                    <span style={{ color: 'var(--c-danger)', fontSize: 'var(--text-sm)' }}>{text}</span>
                ),
            },
        ],
        [],
    )

    return (
        <div style={{ display: 'grid', gap: 'var(--page-gap)' }}>
            <SectionCard
                title="Lập phiếu hoàn trả túi máu"
                extra={
                    <Button
                        icon={<ReloadOutlined />}
                        loading={isEligibleLoading || isEligibleFetching}
                        onClick={() => {
                            void refetchEligible()
                            if (canViewReturns) void refetchReturns()
                        }}
                    >
                        Làm mới danh sách túi
                    </Button>
                }
            >
                <Alert
                    type="info"
                    showIcon
                    icon={<InfoCircleOutlined />}
                    style={{ marginBottom: 18 }}
                    message="Phiếu hoàn trả sẽ được chuyển đến bộ phận Ngân hàng máu"
                    description="Sau khi lập phiếu hoàn trả, túi máu sẽ được phong tỏa (LOCKED) để vận chuyển về Ngân hàng máu kiểm định điều kiện nhập lại kho hoặc chuyển hủy."
                />

                {createdReturn && (
                    <Alert
                        type={createdReturn.isEligibleForRestockEvaluation ? 'success' : 'warning'}
                        showIcon
                        closable
                        onClose={() => setCreatedReturn(null)}
                        style={{ marginBottom: 18 }}
                        icon={createdReturn.isEligibleForRestockEvaluation ? <CheckCircleOutlined /> : <WarningOutlined />}
                        message={
                            createdReturn.isEligibleForRestockEvaluation
                                ? `Đã tạo phiếu hoàn trả #${createdReturn.returnId} thành công`
                                : `Đã tạo phiếu hoàn trả #${createdReturn.returnId} kèm cảnh báo bắt buộc tiêu hủy`
                        }
                        description={
                            <div style={{ display: 'grid', gap: 4, marginTop: 4 }}>
                                <div>
                                    Mã phiếu: <strong>#{createdReturn.returnId}</strong> - Túi máu:{' '}
                                    <code className="handover-barcode">{createdReturn.barcode}</code> ({createdReturn.componentName} -{' '}
                                    {createdReturn.bloodType})
                                </div>
                                <div>
                                    Phiếu yêu cầu: <strong>{createdReturn.requestCode}</strong> - Người bệnh:{' '}
                                    <strong>{createdReturn.patientName}</strong> ({createdReturn.patientCode})
                                </div>
                                <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                                    <span>Trạng thái xử lý:</span>
                                    <StatusPill tone="info" dot={false}>{createdReturn.finalAction}</StatusPill>
                                    <span>- Trạng thái túi:</span>
                                    <StatusPill tone="warning" dot={false}>{createdReturn.bagStatus}</StatusPill>
                                    <span>- Lập lúc: {formatDateTime(createdReturn.processedAt)}</span>
                                </div>
                                {createdReturn.warningFlags && createdReturn.warningFlags.length > 0 && (
                                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginTop: 4 }}>
                                        {createdReturn.warningFlags.map((flag, idx) => (
                                            <Tag key={`${flag}-${idx}`} color="warning" style={{ margin: 0 }}>
                                                {flag}
                                            </Tag>
                                        ))}
                                    </div>
                                )}
                            </div>
                        }
                    />
                )}

                <div style={{ display: 'grid', gap: 16 }}>
                    <div>
                        <label
                            htmlFor="select-return-bag"
                            style={{ display: 'flex', alignItems: 'center', gap: 8, fontWeight: 600, fontSize: 'var(--text-sm)', marginBottom: 6 }}
                        >
                            <span>
                                Chọn túi máu hoàn trả <span style={{ color: 'var(--c-danger)' }}>*</span>
                            </span>
                            <StatusPill tone="success" dot={false}>
                                {eligibleBags.length} túi đủ điều kiện
                            </StatusPill>
                        </label>
                        <Select
                            id="select-return-bag"
                            size="large"
                            style={{ width: '100%' }}
                            showSearch
                            optionFilterProp="label"
                            placeholder={
                                isEligibleLoading
                                    ? 'Đang tải danh sách túi máu đủ điều kiện...'
                                    : eligibleBags.length > 0
                                      ? '-- Chọn túi máu đã xuất kho / đã nhận --'
                                      : 'Hiện không có túi máu nào đủ điều kiện hoàn trả'
                            }
                            loading={isEligibleLoading}
                            value={selectedAllocationId}
                            onChange={(val) => {
                                setSelectedAllocationId(val)
                                setSubmitError(null)
                            }}
                            options={selectOptions}
                            allowClear
                        />
                    </div>

                    {selectedBag && (
                        <div className="clin-ret-bag-summary">
                            <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                                <span style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)' }}>
                                    Túi máu được chọn:
                                </span>
                                <strong className="text-mono" style={{ fontSize: 'var(--text-md)', color: 'var(--c-primary)' }}>
                                    {selectedBag.barcode}
                                </strong>
                                <span>- {selectedBag.componentName} ({selectedBag.volumeMl} ml) -</span>
                                <BloodGroupTag value={selectedBag.bloodType} size="sm" />
                            </div>
                            <div style={{ fontSize: 'var(--text-sm)', color: 'var(--c-text-secondary)', display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                                <span>
                                    Phiếu <strong>{selectedBag.requestCode}</strong> - NB{' '}
                                    <strong>{selectedBag.patientName}</strong> ({selectedBag.patientCode}) -
                                </span>
                                <StatusPill tone={selectedBag.isReceivedAtClinical ? 'success' : 'info'} dot={false}>
                                    {selectedBag.isReceivedAtClinical ? 'Đã nhận tại khoa' : 'Đã xuất kho (Chờ nhận)'}
                                </StatusPill>
                            </div>
                        </div>
                    )}

                    <div>
                        <label
                            htmlFor="select-return-reason"
                            style={{ display: 'block', fontWeight: 600, fontSize: 'var(--text-sm)', marginBottom: 6 }}
                        >
                            Lý do hoàn trả <span style={{ color: 'var(--c-danger)' }}>*</span>
                        </label>
                        <Select
                            id="select-return-reason"
                            size="large"
                            style={{ width: '100%' }}
                            placeholder="Chọn lý do..."
                            value={reasonOption}
                            onChange={(val) => {
                                setReasonOption(val)
                                setSubmitError(null)
                            }}
                            options={PRESET_REASONS.map((r) => ({
                                value: r,
                                label: r === 'OTHER' ? 'Lý do khác (nhập cụ thể bên dưới)...' : r,
                            }))}
                        />
                        {reasonOption === 'OTHER' && (
                            <Input.TextArea
                                rows={3}
                                maxLength={255}
                                showCount
                                style={{ marginTop: 10 }}
                                placeholder="Nhập chi tiết lý do hoàn trả túi máu..."
                                value={customReason}
                                onChange={(e) => setCustomReason(e.target.value)}
                            />
                        )}
                    </div>

                    <div>
                        <div style={{ fontWeight: 600, fontSize: 'var(--text-sm)', marginBottom: 4 }}>
                            Bảng kiểm điều kiện bảo quản (4 tiêu chí đánh giá tái nhập kho)
                        </div>
                        <div style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)', marginBottom: 8 }}>
                            Tích đủ 4 tiêu chí để túi máu được xét tái nhập kho. Nếu bỏ chọn bất kỳ tiêu chí nào, phiếu hoàn trả vẫn gửi được nhưng túi máu sẽ tự động gắn cờ bắt buộc tiêu hủy.
                        </div>
                        <div className="clin-ret-checklist">
                            <div className={`clin-ret-check-item ${checklist.isUnusedFullVolume ? 'is-checked' : ''}`}>
                                <Checkbox
                                    checked={checklist.isUnusedFullVolume}
                                    onChange={(e) =>
                                        setChecklist((c) => ({ ...c, isUnusedFullVolume: e.target.checked }))
                                    }
                                >
                                    Túi máu chưa được sử dụng (toàn bộ thể tích)
                                </Checkbox>
                            </div>
                            <div className={`clin-ret-check-item ${checklist.isBagIntact ? 'is-checked' : ''}`}>
                                <Checkbox
                                    checked={checklist.isBagIntact}
                                    onChange={(e) => setChecklist((c) => ({ ...c, isBagIntact: e.target.checked }))}
                                >
                                    Túi còn nguyên vẹn, không rò rỉ, không vỡ
                                </Checkbox>
                            </div>
                            <div className={`clin-ret-check-item ${checklist.isLabelClear ? 'is-checked' : ''}`}>
                                <Checkbox
                                    checked={checklist.isLabelClear}
                                    onChange={(e) => setChecklist((c) => ({ ...c, isLabelClear: e.target.checked }))}
                                >
                                    Nhãn định danh còn rõ ràng, không bị xóa
                                </Checkbox>
                            </div>
                            <div className={`clin-ret-check-item ${checklist.isStoredUnder30Mins ? 'is-checked' : ''}`}>
                                <Checkbox
                                    checked={checklist.isStoredUnder30Mins}
                                    onChange={(e) =>
                                        setChecklist((c) => ({ ...c, isStoredUnder30Mins: e.target.checked }))
                                    }
                                >
                                    Đã bảo quản đúng nhiệt độ và chưa quá 30 phút ngoài thiết bị lạnh
                                </Checkbox>
                            </div>
                        </div>

                        {selectedBag && !allChecked && (
                            <Alert
                                type="warning"
                                showIcon
                                style={{ marginTop: 10 }}
                                message="Túi máu chưa đạt đủ 4 tiêu chí bảo quản"
                                description="Bạn vẫn có thể gửi phiếu hoàn trả, nhưng Ngân hàng máu sẽ nhận cảnh báo và đánh dấu bắt buộc tiêu hủy (không thể tái nhập kho)."
                            />
                        )}
                    </div>

                    {submitError && (
                        <Alert
                            type="error"
                            showIcon
                            closable
                            onClose={() => setSubmitError(null)}
                            message="Không thể lập phiếu hoàn trả"
                            description={submitError}
                        />
                    )}

                    <div className="clin-ret-actions">
                        <Button onClick={handleResetForm} disabled={createReturnMutation.isPending}>
                            Hủy
                        </Button>
                        <Button
                            type="primary"
                            icon={<RollbackOutlined />}
                            loading={createReturnMutation.isPending}
                            disabled={
                                !canEditReturns ||
                                !selectedBag ||
                                !resolvedReason ||
                                createReturnMutation.isPending
                            }
                            onClick={() => void handleSubmit()}
                        >
                            Gửi phiếu hoàn trả về Ngân hàng máu
                        </Button>
                    </div>
                </div>
            </SectionCard>

            {ineligibleBags.length > 0 && (
                <SectionCard
                    title="Túi máu không đủ điều kiện hoàn trả (bị khóa chọn)"
                    extra={
                        <StatusPill tone="neutral" dot={false}>
                            {ineligibleBags.length} túi
                        </StatusPill>
                    }
                    noPadding
                >
                    <Table
                        columns={ineligibleColumns}
                        dataSource={ineligibleBags}
                        rowKey="allocationId"
                        pagination={false}
                        size="small"
                    />
                </SectionCard>
            )}

            <SectionCard
                title="Phiếu hoàn trả đang chờ Ngân hàng máu kiểm định"
                extra={
                    <StatusPill tone="warning" dot={false}>
                        {pendingReturnsPage?.totalCount || 0} phiếu đang chờ
                    </StatusPill>
                }
                noPadding
            >
                <Table
                    columns={returnColumns}
                    dataSource={pendingReturnsPage?.items || []}
                    rowKey="returnId"
                    loading={isPendingReturnsLoading || isPendingReturnsFetching}
                    pagination={{
                        pageSize: 10,
                        total: pendingReturnsPage?.totalCount || 0,
                    }}
                    locale={{
                        emptyText: (
                            <Empty
                                description="Chưa có phiếu hoàn trả nào của khoa đang chờ kiểm định."
                                style={{ padding: 20 }}
                            />
                        ),
                    }}
                />
            </SectionCard>

            <Modal
                title={
                    cancellingReturn
                        ? `Hủy phiếu hoàn trả #${cancellingReturn.returnId} (${cancellingReturn.barcode})`
                        : 'Hủy phiếu hoàn trả'
                }
                open={Boolean(cancellingReturn)}
                onCancel={() => {
                    if (cancelReturnMutation.isPending) return
                    setCancellingReturn(null)
                    setCancelReason('')
                }}
                onOk={() => void handleConfirmCancelReturn()}
                okText="Xác nhận hủy phiếu"
                okButtonProps={{ danger: true, loading: cancelReturnMutation.isPending }}
                cancelText="Đóng"
                cancelButtonProps={{ disabled: cancelReturnMutation.isPending }}
                destroyOnHidden
            >
                {cancellingReturn && (
                    <div style={{ display: 'grid', gap: 12, paddingTop: 6 }}>
                        <Alert
                            type="warning"
                            showIcon
                            message="Túi máu sẽ được mở khóa và khôi phục về ISSUED"
                            description={`Phiếu #${cancellingReturn.returnId} của NB ${cancellingReturn.patientName} (${cancellingReturn.patientCode}) sẽ bị hủy trước khi Ngân hàng máu kiểm định.`}
                        />
                        <div>
                            <div style={{ fontWeight: 600, fontSize: 'var(--text-sm)', marginBottom: 6 }}>
                                Lý do hủy phiếu (không bắt buộc)
                            </div>
                            <Input.TextArea
                                rows={3}
                                maxLength={255}
                                showCount
                                placeholder="Ví dụ: Chọn nhầm túi máu hoàn trả / bác sĩ chỉ định giữ lại truyền tiếp..."
                                value={cancelReason}
                                onChange={(e) => setCancelReason(e.target.value)}
                                disabled={cancelReturnMutation.isPending}
                            />
                        </div>
                    </div>
                )}
            </Modal>
        </div>
    )
}
