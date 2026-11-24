import React, { useMemo, useState } from 'react'
import { Modal, Input, Button, Alert, Form } from 'antd'
import { ScanOutlined, CheckCircleOutlined } from '@ant-design/icons'
import { BloodGroupTag, StatusPill } from '@/shared/components'
import { useConfirmHandoverSession } from '../hooks'
import type { HandoverSessionDto } from '../types'
import { formatDateTime } from '@/shared/formatters/date'
import './ScanHandoverModal.css'

interface ScanHandoverModalProps {
    open: boolean
    onClose: () => void
    targetSession?: HandoverSessionDto | null
}

export const ScanHandoverModal: React.FC<ScanHandoverModalProps> = ({
    open,
    onClose,
    targetSession,
}) => {
    const [scannedInput, setScannedInput] = useState('')
    const [expectedPatientCode, setExpectedPatientCode] = useState('')
    const confirmMutation = useConfirmHandoverSession()

    const expectedBarcodes = useMemo(
        () => (targetSession?.items ?? []).map((i) => i.barcode),
        [targetSession],
    )

    const handleSubmit = async () => {
        if (!targetSession) return

        const barcodesToSend = scannedInput.trim()
            ? scannedInput
                  .split(/[\s,;]+/)
                  .map((s) => s.trim())
                  .filter(Boolean)
            : expectedBarcodes

        try {
            await confirmMutation.mutateAsync({
                sessionId: targetSession.handoverSessionId,
                data: {
                    scannedBarcodes: barcodesToSend,
                    expectedPatientCode: !targetSession.isOwnRequest ? expectedPatientCode.trim() : undefined,
                },
            })
            setScannedInput('')
            setExpectedPatientCode('')
            onClose()
        } catch {
            // Handled in hook onError
        }
    }

    return (
        <Modal
            open={open}
            onCancel={onClose}
            footer={null}
            width={560}
            centered
            destroyOnClose
            className="scan-handover-modal"
        >
            <div className="scan-modal__header">
                <div className="scan-modal__icon">
                    <ScanOutlined />
                </div>
                <div>
                    <h3 className="scan-modal__title">Xác nhận nhận lô túi máu theo phiên</h3>
                    <p className="scan-modal__subtitle">
                        Đối soát thông tin các túi máu trong phiên bàn giao từ Ngân hàng máu để hoàn tất tiếp nhận.
                    </p>
                </div>
            </div>

            {targetSession ? (
                <div className="scan-modal__bag-info">
                    <div className="scan-modal__bag-row">
                        <span>Mã phiên bàn giao</span>
                        <strong style={{ color: 'var(--c-primary)', fontFamily: 'var(--font-mono)', fontSize: 15 }}>
                            {targetSession.sessionCode}
                        </strong>
                    </div>
                    <div className="scan-modal__bag-row">
                        <span>Người bệnh</span>
                        <strong>{targetSession.patientName} ({targetSession.patientCode})</strong>
                    </div>
                    <div className="scan-modal__bag-row">
                        <span>Số túi trong lô</span>
                        <span>{targetSession.items.length} túi ({expectedBarcodes.join(', ')})</span>
                    </div>
                    <div className="scan-modal__bag-row">
                        <span>Nhóm máu người bệnh</span>
                        <BloodGroupTag value={targetSession.patientBloodType} size="sm" />
                    </div>
                    <div className="scan-modal__bag-row">
                        <span>Bác sĩ lập phiếu</span>
                        <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
                            <span>{targetSession.doctorName || 'Bác sĩ điều trị'}</span>
                            <StatusPill tone={targetSession.isOwnRequest ? 'success' : 'warning'} dot={false}>
                                {targetSession.isOwnRequest ? 'Phiếu của bạn' : 'Nhận hộ trong khoa'}
                            </StatusPill>
                        </span>
                    </div>
                    <div className="scan-modal__bag-row">
                        <span>Hiệu lực đến</span>
                        <span>{formatDateTime(targetSession.expiresAt)}</span>
                    </div>
                </div>
            ) : (
                <Alert
                    type="info"
                    message="Vui lòng chọn phiên bàn giao cần nhận từ danh sách chờ xác nhận."
                    showIcon
                    style={{ marginBottom: 16 }}
                />
            )}

            {!targetSession?.isOwnRequest && targetSession && (
                <Alert
                    type="warning"
                    message="Lưu ý nhận hộ trong khoa"
                    description="Bạn đang nhận lô túi máu của bác sĩ khác lập trong khoa. Theo quy định an toàn, bắt buộc đối soát mã người bệnh chính xác."
                    showIcon
                    style={{ marginBottom: 16 }}
                />
            )}

            <Form layout="vertical">
                <Form.Item label="Danh sách barcode đã quét (mặc định khớp đủ lô nếu để trống)">
                    <Input
                        prefix={<ScanOutlined style={{ color: 'var(--c-success)' }} />}
                        placeholder={expectedBarcodes.join(', ') || 'Quét hoặc nhập các mã túi máu...'}
                        value={scannedInput}
                        onChange={(e) => setScannedInput(e.target.value)}
                        size="large"
                    />
                </Form.Item>

                {!targetSession?.isOwnRequest && targetSession && (
                    <Form.Item label="Mã người bệnh đối soát (Expected Patient Code)" required>
                        <Input
                            placeholder="Nhập mã hồ sơ người bệnh..."
                            value={expectedPatientCode}
                            onChange={(e) => setExpectedPatientCode(e.target.value)}
                            size="large"
                        />
                    </Form.Item>
                )}

                <div className="scan-modal__footer">
                    <Button onClick={onClose} disabled={confirmMutation.isPending}>
                        Hủy
                    </Button>
                    <Button
                        type="primary"
                        loading={confirmMutation.isPending}
                        icon={<CheckCircleOutlined />}
                        onClick={() => void handleSubmit()}
                        disabled={!targetSession || (!targetSession.isOwnRequest && !expectedPatientCode.trim())}
                        className="scan-modal__submit-btn"
                    >
                        Xác nhận nhận lô máu
                    </Button>
                </div>
            </Form>
        </Modal>
    )
}
