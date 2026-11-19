import React, { useState, useEffect } from 'react'
import { Modal, Input, Button, Alert, Tag, Form } from 'antd'
import { ScanOutlined, CheckCircleOutlined } from '@ant-design/icons'
import { useConfirmReceipt } from '../hooks'
import type { PendingReceiptBagDto } from '../types'
import { formatDateTime } from '@/shared/formatters/date'
import './ScanHandoverModal.css'

interface ScanHandoverModalProps {
    open: boolean
    onClose: () => void
    targetBag?: PendingReceiptBagDto | null
}

export const ScanHandoverModal: React.FC<ScanHandoverModalProps> = ({
    open,
    onClose,
    targetBag,
}) => {
    const [scannedBarcode, setScannedBarcode] = useState('')
    const [expectedPatientCode, setExpectedPatientCode] = useState('')
    const confirmMutation = useConfirmReceipt()

    useEffect(() => {
        if (targetBag) {
            setScannedBarcode(targetBag.barcode)
            setExpectedPatientCode(targetBag.patientCode)
        } else {
            setScannedBarcode('')
            setExpectedPatientCode('')
        }
    }, [targetBag, open])

    const handleSubmit = async () => {
        if (!targetBag?.allocationId) return

        try {
            await confirmMutation.mutateAsync({
                allocationId: targetBag.allocationId,
                scannedBarcode: scannedBarcode.trim(),
                expectedPatientCode: !targetBag.isOwnRequest ? expectedPatientCode.trim() : undefined,
            })
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
            width={540}
            centered
            destroyOnClose
            className="scan-handover-modal"
        >
            <div className="scan-modal__header">
                <div className="scan-modal__icon">
                    <ScanOutlined />
                </div>
                <div>
                    <h3 className="scan-modal__title">Xác nhận nhận túi máu từ Ngân hàng máu</h3>
                    <p className="scan-modal__subtitle">
                        Đối soát thông tin túi máu và quét mã vạch để hoàn tất nhận bàn giao tại lâm sàng.
                    </p>
                </div>
            </div>

            {targetBag ? (
                <div className="scan-modal__bag-info">
                    <div className="scan-modal__bag-row">
                        <span>Mã túi máu</span>
                        <strong style={{ color: '#166534', fontFamily: 'monospace', fontSize: 15 }}>
                            {targetBag.barcode}
                        </strong>
                    </div>
                    <div className="scan-modal__bag-row">
                        <span>Người bệnh</span>
                        <strong>{targetBag.patientName} ({targetBag.patientCode})</strong>
                    </div>
                    <div className="scan-modal__bag-row">
                        <span>Loại chế phẩm</span>
                        <span>{targetBag.componentName} · {targetBag.volumeMl} ml</span>
                    </div>
                    <div className="scan-modal__bag-row">
                        <span>Nhóm máu túi</span>
                        <Tag color="red" style={{ fontWeight: 700 }}>{targetBag.bloodType}</Tag>
                    </div>
                    <div className="scan-modal__bag-row">
                        <span>Bác sĩ lập phiếu</span>
                        <span>
                            {targetBag.doctorName || 'Bác sĩ điều trị'}{' '}
                            {targetBag.isOwnRequest ? (
                                <Tag color="green">Phiếu của bạn</Tag>
                            ) : (
                                <Tag color="gold">Nhận hộ trong khoa</Tag>
                            )}
                        </span>
                    </div>
                    <div className="scan-modal__bag-row">
                        <span>Thời gian xuất kho</span>
                        <span>{targetBag.issuedAt ? formatDateTime(targetBag.issuedAt) : 'Chưa ghi nhận'}</span>
                    </div>
                </div>
            ) : (
                <Alert
                    type="info"
                    message="Vui lòng chọn túi máu cần nhận từ danh sách tab 'Nhận bàn giao' để đối soát."
                    showIcon
                    style={{ marginBottom: 16 }}
                />
            )}

            {!targetBag?.isOwnRequest && targetBag && (
                <Alert
                    type="warning"
                    message="Lưu ý nhận hộ trong khoa"
                    description="Bạn đang nhận túi máu của bác sĩ khác lập trong khoa. Theo quy định an toàn, bắt buộc đối soát mã người bệnh chính xác."
                    showIcon
                    style={{ marginBottom: 16 }}
                />
            )}

            <Form layout="vertical">
                <Form.Item label="Mã vạch quét được (Barcode)" required>
                    <Input
                        prefix={<ScanOutlined style={{ color: '#16a34a' }} />}
                        placeholder="Quét hoặc nhập mã túi máu..."
                        value={scannedBarcode}
                        onChange={(e) => setScannedBarcode(e.target.value)}
                        size="large"
                    />
                </Form.Item>

                {!targetBag?.isOwnRequest && targetBag && (
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
                        onClick={handleSubmit}
                        disabled={!targetBag || !scannedBarcode.trim() || (!targetBag.isOwnRequest && !expectedPatientCode.trim())}
                        className="scan-modal__submit-btn"
                    >
                        Xác nhận thực nhận
                    </Button>
                </div>
            </Form>
        </Modal>
    )
}
