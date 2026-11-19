import React from 'react'
import { Modal, Button } from 'antd'
import { CheckOutlined } from '@ant-design/icons'
import { getUrgencyMeta } from '../format'
import type { UrgencyLevel } from '../types'
import './ConfirmSendRequestModal.css'

export interface ConfirmSendRequestData {
    requestCode?: string
    patientName: string
    patientCode: string
    bloodGroup: string
    componentName: string
    units: number
    urgencyLevel: UrgencyLevel
    departmentName?: string
    doctorName?: string
}

interface ConfirmSendRequestModalProps {
    open: boolean
    loading?: boolean
    data: ConfirmSendRequestData | null
    onClose: () => void
    onConfirm: () => void
}

export const ConfirmSendRequestModal: React.FC<ConfirmSendRequestModalProps> = ({
    open,
    loading = false,
    data,
    onClose,
    onConfirm,
}) => {
    if (!data) return null

    const urgencyMeta = getUrgencyMeta(data.urgencyLevel)

    return (
        <Modal
            open={open}
            onCancel={() => {
                if (!loading) onClose()
            }}
            closable={!loading}
            maskClosable={!loading}
            footer={null}
            width={580}
            className="confirm-request-modal"
            centered
            destroyOnClose
        >
            <div className="confirm-modal__alert-header">
                <div className="confirm-modal__icon-wrap">
                    <span className="confirm-modal__exclamation">!</span>
                </div>
                <div>
                    <h3 className="confirm-modal__title">Xác nhận gửi phiếu yêu cầu</h3>
                    <p className="confirm-modal__subtitle">
                        Sau khi gửi, phiếu sẽ được chuyển ngay đến bộ phận Ngân hàng máu để xử lý. Không thể chỉnh sửa sau khi gửi.
                    </p>
                </div>
            </div>

            <div className="confirm-modal__card">
                {data.requestCode && (
                    <div className="confirm-modal__row">
                        <span className="confirm-modal__label">Mã phiếu</span>
                        <span className="confirm-modal__value font-bold">{data.requestCode}</span>
                    </div>
                )}
                <div className="confirm-modal__row">
                    <span className="confirm-modal__label">Người bệnh</span>
                    <span className="confirm-modal__value font-bold">
                        {data.patientName} · {data.patientCode}
                    </span>
                </div>
                <div className="confirm-modal__row">
                    <span className="confirm-modal__label">Nhóm máu</span>
                    <span className="confirm-modal__value font-bold">{data.bloodGroup}</span>
                </div>
                <div className="confirm-modal__row">
                    <span className="confirm-modal__label">Sản phẩm & Số lượng</span>
                    <span className="confirm-modal__value font-bold">
                        {data.units} túi · {data.componentName}
                    </span>
                </div>
                <div className="confirm-modal__row">
                    <span className="confirm-modal__label">Mức độ ưu tiên</span>
                    <span className="confirm-modal__value font-bold" style={{ color: urgencyMeta.color }}>
                        {urgencyMeta.label}
                    </span>
                </div>
                <div className="confirm-modal__row">
                    <span className="confirm-modal__label">Khoa yêu cầu</span>
                    <span className="confirm-modal__value">{data.departmentName || 'Khoa Điều trị'}</span>
                </div>
                <div className="confirm-modal__row">
                    <span className="confirm-modal__label">Bác sĩ chỉ định</span>
                    <span className="confirm-modal__value font-medium">{data.doctorName || 'Bác sĩ điều trị'}</span>
                </div>
            </div>

            <div className="confirm-modal__footer">
                <Button onClick={onClose} disabled={loading} className="confirm-modal__btn-back">
                    Quay lại chỉnh sửa
                </Button>
                <Button
                    type="primary"
                    loading={loading}
                    icon={<CheckOutlined />}
                    onClick={onConfirm}
                    className="confirm-modal__btn-submit"
                >
                    Xác nhận gửi phiếu
                </Button>
            </div>
        </Modal>
    )
}
