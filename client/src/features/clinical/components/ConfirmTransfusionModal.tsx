import { useState } from 'react'
import { Alert, Button, Input, Modal } from 'antd'
import { CheckCircleOutlined, HeartOutlined, WarningOutlined } from '@ant-design/icons'
import { parseApiError } from '@/services/apiError'
import { formatDateTime } from '@/shared/formatters/date'
import { BloodGroupTag, StatusPill } from '@/shared/components'
import { useConfirmTransfusion } from '../hooks'
import type { ConfirmTransfusionResponse, TransfusionScanDto } from '../types'

interface ConfirmTransfusionModalProps {
    open: boolean
    onClose: () => void
    bag: TransfusionScanDto | null
    onConfirmed?: (res: ConfirmTransfusionResponse) => void
}

interface ConfirmTransfusionFormProps {
    bag: TransfusionScanDto
    onClose: () => void
    onConfirmed?: (res: ConfirmTransfusionResponse) => void
}

function ConfirmTransfusionForm({ bag, onClose, onConfirmed }: ConfirmTransfusionFormProps) {
    const confirmMutation = useConfirmTransfusion()
    const [expectedPatientCode, setExpectedPatientCode] = useState('')
    const [errorMsg, setErrorMsg] = useState<string | null>(null)

    const isIssued = bag.bagStatus === 'ISSUED'

    const handleSubmit = async () => {
        if (confirmMutation.isPending) return
        setErrorMsg(null)

        const trimmedCode = expectedPatientCode.trim()
        if (!trimmedCode) {
            setErrorMsg('Vui lòng nhập hoặc quét mã người bệnh trên vòng tay để đối chiếu.')
            return
        }

        if (!isIssued) {
            setErrorMsg(
                `Túi máu ${bag.barcode} hiện không ở trạng thái đã xuất kho, không thể xác nhận truyền máu.`,
            )
            return
        }

        try {
            const res = await confirmMutation.mutateAsync({
                barcode: bag.barcode,
                allocationId: bag.allocationId,
                expectedPatientCode: trimmedCode,
            })
            onConfirmed?.(res)
            onClose()
        } catch (err) {
            setErrorMsg(parseApiError(err).message)
        }
    }

    return (
        <div style={{ display: 'grid', gap: 16 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                <div
                    style={{
                        width: 42,
                        height: 42,
                        borderRadius: 'var(--radius-md)',
                        background: 'var(--c-danger-soft, #fff1f0)',
                        color: 'var(--c-danger, #cf1322)',
                        display: 'grid',
                        placeItems: 'center',
                        fontSize: 20,
                        flexShrink: 0,
                    }}
                >
                    <HeartOutlined />
                </div>
                <div>
                    <h3 style={{ margin: 0, fontSize: 'var(--text-lg)', fontWeight: 700, color: 'var(--c-text)' }}>
                        Xác nhận truyền máu tại giường
                    </h3>
                    <div style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-secondary)', marginTop: 2 }}>
                        Đối chiếu túi máu và xác nhận mã người bệnh trước khi chuyển trạng thái sang Đã truyền cho người bệnh.
                    </div>
                </div>
            </div>

            {!isIssued && (
                <Alert
                    type="error"
                    showIcon
                    icon={<WarningOutlined />}
                    message="Trạng thái túi máu không hợp lệ"
                    description="Chỉ túi máu đã xuất kho về khoa mới có thể xác nhận truyền tại giường."
                />
            )}

            {bag.isExpired && (
                <Alert
                    type="error"
                    showIcon
                    message="Túi máu đã quá hạn sử dụng"
                    description={`Hạn sử dụng ghi nhận: ${formatDateTime(bag.expiredAt)}. Vui lòng kiểm tra lại trước khi truyền.`}
                />
            )}

            <div
                style={{
                    display: 'grid',
                    gap: 12,
                    gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
                }}
            >
                <div
                    style={{
                        padding: 14,
                        borderRadius: 'var(--radius-md)',
                        border: '1px solid var(--c-border)',
                        background: 'var(--c-surface-muted, #fafafa)',
                        display: 'grid',
                        gap: 8,
                    }}
                >
                    <div style={{ fontSize: 11, fontWeight: 700, textTransform: 'uppercase', color: 'var(--c-text-muted)' }}>
                        Thông tin túi máu
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 8 }}>
                        <code className="handover-barcode" style={{ fontSize: 'var(--text-sm)' }}>
                            {bag.barcode}
                        </code>
                        <BloodGroupTag value={bag.bloodType} size="sm" />
                    </div>
                    <div style={{ fontSize: 'var(--text-sm)', fontWeight: 600 }}>
                        {bag.componentName} ({bag.componentCode}) - {bag.volumeMl} ml
                    </div>
                    <div style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-secondary)', display: 'grid', gap: 4 }}>
                        <div>
                            Trạng thái:{' '}
                            <StatusPill tone={isIssued ? 'info' : 'danger'} dot={false}>
                                {isIssued ? 'Đã xuất kho' : 'Không hợp lệ'}
                            </StatusPill>
                        </div>
                        <div>Hạn dùng: {formatDateTime(bag.expiredAt)}</div>
                        <div>Xuất kho lúc: {bag.issuedAt ? formatDateTime(bag.issuedAt) : '—'}</div>
                        <div>Người nhận tại quầy: {bag.receivedByName || 'Chưa ghi nhận'}</div>
                    </div>
                </div>

                <div
                    style={{
                        padding: 14,
                        borderRadius: 'var(--radius-md)',
                        border: '1px solid var(--c-primary-border)',
                        background: 'var(--c-primary-soft)',
                        display: 'grid',
                        gap: 8,
                    }}
                >
                    <div style={{ fontSize: 11, fontWeight: 700, textTransform: 'uppercase', color: 'var(--c-primary)' }}>
                        Người bệnh được chỉ định
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 8 }}>
                        <span style={{ fontSize: 'var(--text-base)', fontWeight: 700 }}>{bag.patientName}</span>
                        <BloodGroupTag value={bag.patientBloodType} size="sm" />
                    </div>
                    <div style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-secondary)', display: 'grid', gap: 4 }}>
                        <div>
                            Mã người bệnh: <strong className="text-mono">{bag.patientCode}</strong>
                        </div>
                        <div>
                            Phiếu yêu cầu: <strong>{bag.requestCode}</strong> ({bag.isOwnRequest ? 'Phiếu của bạn' : 'Cùng khoa'})
                        </div>
                        <div>Khoa điều trị: {bag.departmentName || '—'}</div>
                        <div>Bác sĩ chỉ định: {bag.doctorName || '—'}</div>
                    </div>
                </div>
            </div>

            <div style={{ display: 'grid', gap: 6 }}>
                <label
                    htmlFor="confirm-transfusion-patient-code"
                    style={{ fontWeight: 600, fontSize: 'var(--text-sm)', color: 'var(--c-text)' }}
                >
                    Xác minh mã người bệnh tại giường <span style={{ color: 'var(--c-danger)' }}>*</span>
                </label>
                <Input
                    id="confirm-transfusion-patient-code"
                    size="large"
                    autoFocus
                    value={expectedPatientCode}
                    onChange={(e) => {
                        setExpectedPatientCode(e.target.value)
                        if (errorMsg) setErrorMsg(null)
                    }}
                    onPressEnter={() => void handleSubmit()}
                    placeholder="Nhập hoặc quét mã vòng tay người bệnh (VD: BN-250602-0041)"
                    disabled={confirmMutation.isPending}
                />
                <div style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)' }}>
                    Hệ thống yêu cầu đối chiếu trực tiếp mã người bệnh trên vòng tay để phòng ngừa nhầm lẫn trước khi truyền.
                </div>
            </div>

            {errorMsg && (
                <Alert
                    type="error"
                    showIcon
                    closable
                    onClose={() => setErrorMsg(null)}
                    message="Xác nhận truyền máu thất bại"
                    description={errorMsg}
                />
            )}

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, paddingTop: 4 }}>
                <Button onClick={onClose} disabled={confirmMutation.isPending}>
                    Hủy
                </Button>
                <Button
                    type="primary"
                    icon={<CheckCircleOutlined />}
                    loading={confirmMutation.isPending}
                    disabled={!isIssued || !expectedPatientCode.trim() || confirmMutation.isPending}
                    onClick={() => void handleSubmit()}
                >
                    Xác nhận truyền máu
                </Button>
            </div>
        </div>
    )
}

export function ConfirmTransfusionModal({
    open,
    onClose,
    bag,
    onConfirmed,
}: ConfirmTransfusionModalProps) {
    return (
        <Modal
            open={open && Boolean(bag)}
            onCancel={onClose}
            footer={null}
            width={640}
            centered
            destroyOnHidden
        >
            {bag && (
                <ConfirmTransfusionForm
                    key={bag.allocationId}
                    bag={bag}
                    onClose={onClose}
                    onConfirmed={onConfirmed}
                />
            )}
        </Modal>
    )
}
