import { useMemo, useState } from 'react'
import { Alert, Button, Card, Checkbox, Empty, Input, Select, Table, Tag, message } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { CheckCircleOutlined, InfoCircleOutlined, ReloadOutlined, RollbackOutlined } from '@ant-design/icons'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { formatDateTime } from '@/shared/formatters/date'
import { parseApiError } from '@/services/apiError'
import { useCreateReturn, useEligibleReturnBags, usePendingBloodReturns } from '../hooks'
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

        if (!allChecked) {
            message.warning('Bắt buộc xác nhận đầy đủ 4 điều kiện bảo quản trước khi gửi phiếu hoàn trả.')
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

    const selectOptions = useMemo(() => {
        const eligibleOpts = eligibleBags.map((b) => ({
            value: b.allocationId,
            label: `${b.barcode} · ${b.componentName} (${b.bloodType}, ${b.volumeMl}ml) — ${b.patientName} [${b.requestCode}]`,
            disabled: false,
        }))
        const ineligibleOpts = ineligibleBags.map((b) => ({
            value: -b.allocationId,
            label: `[Không đủ điều kiện] ${b.barcode} · ${b.componentName} — ${b.ineligibleReason}`,
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
                        <div style={{ color: '#64748b', fontSize: 12 }}>Phiếu yêu cầu: {r.requestCode}</div>
                    </div>
                ),
            },
            {
                title: 'CHẾ PHẨM',
                key: 'product',
                render: (_, r) => (
                    <div>
                        <div>
                            {r.componentName} ({r.volumeMl} ml)
                        </div>
                        <Tag color="red">{r.bloodType}</Tag>
                    </div>
                ),
            },
            {
                title: 'LÝ DO HOÀN TRẢ',
                dataIndex: 'returnReason',
                key: 'returnReason',
                render: (text) => <span style={{ color: '#475569' }}>{text}</span>,
            },
            {
                title: 'TRẠNG THÁI XỬ LÝ',
                dataIndex: 'finalAction',
                key: 'status',
                align: 'center',
                render: (action) => (
                    <Tag color="processing">
                        {action === 'PENDING_INSPECTION' ? 'Chờ Ngân hàng máu kiểm định' : action}
                    </Tag>
                ),
            },
            {
                title: 'THỜI GIAN LẬP',
                dataIndex: 'processedAt',
                key: 'processedAt',
                render: (val) => formatDateTime(val),
            },
        ],
        [],
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
                        {r.requestCode} · {r.patientName} ({r.patientCode})
                    </span>
                ),
            },
            {
                title: 'TRẠNG THÁI TÚI',
                key: 'bagStatus',
                render: (_, r) => <Tag>{r.bagStatus || r.allocationStatus}</Tag>,
            },
            {
                title: 'LÝ DO KHÔNG ĐỦ ĐIỀU KIỆN',
                dataIndex: 'ineligibleReason',
                key: 'reason',
                render: (text) => <span style={{ color: '#b91c1c', fontSize: 13 }}>{text}</span>,
            },
        ],
        [],
    )

    return (
        <div style={{ display: 'grid', gap: 20 }}>
            <Card className="clinical-main-card" bordered={false}>
                <div className="clin-ret-head" style={{ marginBottom: 16 }}>
                    <h2 className="clin-ret-title">Lập phiếu hoàn trả túi máu – W3.3</h2>
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
                </div>

                <Alert
                    type="info"
                    showIcon
                    icon={<InfoCircleOutlined />}
                    style={{ marginBottom: 18 }}
                    message="Phiếu hoàn trả sẽ được chuyển đến bộ phận Ngân hàng máu (W2.5)"
                    description="Sau khi lập phiếu hoàn trả, túi máu sẽ được phong tỏa (LOCKED) để vận chuyển về Ngân hàng máu kiểm định điều kiện nhập lại kho hoặc chuyển hủy."
                />

                {createdReturn && (
                    <Alert
                        type="success"
                        showIcon
                        closable
                        onClose={() => setCreatedReturn(null)}
                        style={{ marginBottom: 18 }}
                        icon={<CheckCircleOutlined />}
                        message={`Đã tạo phiếu hoàn trả #${createdReturn.returnId} thành công`}
                        description={
                            <div style={{ display: 'grid', gap: 4, marginTop: 4 }}>
                                <div>
                                    Mã phiếu: <strong>#{createdReturn.returnId}</strong> · Túi máu:{' '}
                                    <code className="handover-barcode">{createdReturn.barcode}</code> ({createdReturn.componentName} ·{' '}
                                    {createdReturn.bloodType})
                                </div>
                                <div>
                                    Phiếu yêu cầu: <strong>{createdReturn.requestCode}</strong> · Người bệnh:{' '}
                                    <strong>{createdReturn.patientName}</strong> ({createdReturn.patientCode})
                                </div>
                                <div>
                                    Trạng thái xử lý: <Tag color="processing">{createdReturn.finalAction}</Tag> · Trạng thái túi:{' '}
                                    <Tag color="warning">{createdReturn.bagStatus}</Tag> · Lập lúc:{' '}
                                    {formatDateTime(createdReturn.processedAt)}
                                </div>
                            </div>
                        }
                    />
                )}

                <div style={{ display: 'grid', gap: 16 }}>
                    <div>
                        <label
                            htmlFor="select-return-bag"
                            style={{ display: 'block', fontWeight: 600, fontSize: 13, marginBottom: 6 }}
                        >
                            Chọn túi máu hoàn trả <span style={{ color: '#dc2626' }}>*</span>{' '}
                            <Tag color="green" style={{ marginLeft: 6 }}>
                                {eligibleBags.length} túi đủ điều kiện
                            </Tag>
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
                            <div>
                                <div style={{ fontSize: 12, color: '#64748b' }}>Túi máu được chọn</div>
                                <strong style={{ fontFamily: 'monospace', fontSize: 15, color: '#15803d' }}>
                                    {selectedBag.barcode}
                                </strong>{' '}
                                · {selectedBag.componentName} ({selectedBag.volumeMl} ml) ·{' '}
                                <Tag color="red">{selectedBag.bloodType}</Tag>
                            </div>
                            <div style={{ fontSize: 13, color: '#334155' }}>
                                Phiếu <strong>{selectedBag.requestCode}</strong> · NB{' '}
                                <strong>{selectedBag.patientName}</strong> ({selectedBag.patientCode}) ·{' '}
                                <Tag color={selectedBag.isReceivedAtClinical ? 'green' : 'blue'}>
                                    {selectedBag.isReceivedAtClinical ? 'Đã nhận tại khoa' : 'Đã xuất kho (Chờ nhận)'}
                                </Tag>
                            </div>
                        </div>
                    )}

                    <div>
                        <label
                            htmlFor="select-return-reason"
                            style={{ display: 'block', fontWeight: 600, fontSize: 13, marginBottom: 6 }}
                        >
                            Lý do hoàn trả <span style={{ color: '#dc2626' }}>*</span>
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
                        <div style={{ fontWeight: 600, fontSize: 13, marginBottom: 8 }}>
                            Xác nhận điều kiện bảo quản <span style={{ color: '#dc2626' }}>*</span>
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
                            className="clin-ret-submit-btn"
                            icon={<RollbackOutlined />}
                            loading={createReturnMutation.isPending}
                            disabled={
                                !canEditReturns ||
                                !selectedBag ||
                                !resolvedReason ||
                                !allChecked ||
                                createReturnMutation.isPending
                            }
                            onClick={() => void handleSubmit()}
                        >
                            → Gửi phiếu hoàn trả về Ngân hàng máu
                        </Button>
                    </div>
                </div>
            </Card>

            {ineligibleBags.length > 0 && (
                <Card className="clinical-main-card" bordered={false}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 10 }}>
                        <h3 style={{ fontSize: 15, fontWeight: 700, margin: 0, color: '#1e293b' }}>
                            Túi máu không đủ điều kiện hoàn trả (bị khóa chọn)
                        </h3>
                        <Tag color="default">{ineligibleBags.length} túi</Tag>
                    </div>
                    <Table
                        columns={ineligibleColumns}
                        dataSource={ineligibleBags}
                        rowKey="allocationId"
                        pagination={false}
                        size="small"
                    />
                </Card>
            )}

            <Card className="clinical-main-card" bordered={false}>
                <div className="clin-ret-head" style={{ marginBottom: 12 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                        <h3 style={{ fontSize: 15, fontWeight: 700, margin: 0, color: '#1e293b' }}>
                            Phiếu hoàn trả đang chờ Ngân hàng máu kiểm định
                        </h3>
                        <Tag color="orange">{pendingReturnsPage?.totalCount || 0} phiếu đang chờ</Tag>
                    </div>
                </div>

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
            </Card>
        </div>
    )
}
