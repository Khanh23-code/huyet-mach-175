import { useState } from 'react'
import { Alert, Input, Modal, message } from 'antd'
import { ExclamationCircleOutlined } from '@ant-design/icons'
import { useRejectBloodRequest } from '../hooks'
import type { BloodRequestDetailDto } from '../types'

interface Props {
    visible: boolean
    request: BloodRequestDetailDto | null
    onClose: () => void
    onSuccess: () => void
}

export default function RejectRequestModal({ visible, request, onClose, onSuccess }: Props) {
    const [reason, setReason] = useState('')
    const [errorMsg, setErrorMsg] = useState<string | null>(null)
    const rejectMutation = useRejectBloodRequest()
    const submitting = rejectMutation.isPending

    const handleConfirm = async () => {
        if (!request) return
        const trimmed = reason.trim()
        if (!trimmed) {
            setErrorMsg('Vui lòng nhập lý do từ chối yêu cầu.')
            return
        }
        if (trimmed.length > 255) {
            setErrorMsg('Lý do từ chối không được vượt quá 255 ký tự.')
            return
        }

        setErrorMsg(null)
        try {
            const res = await rejectMutation.mutateAsync({
                requestId: request.requestId,
                data: { rejectionReason: trimmed },
            })
            message.success(
                `Đã từ chối phiếu ${res.requestCode}. ${
                    res.releasedBagCount > 0 ? `Đã giải phóng ${res.releasedBagCount} túi máu về kho.` : ''
                }`,
            )
            setReason('')
            onSuccess()
            onClose()
        } catch (err: unknown) {
            const msg =
                (err as { response?: { data?: { message?: string } } })?.response?.data?.message ||
                (err instanceof Error ? err.message : 'Không thể từ chối phiếu yêu cầu.')
            setErrorMsg(msg)
        }
    }

    const handleCancel = () => {
        if (!submitting) {
            setErrorMsg(null)
            onClose()
        }
    }

    if (!request) return null

    return (
        <Modal
            title={
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, color: 'var(--c-danger)' }}>
                    <ExclamationCircleOutlined />
                    <span>Từ chối phiếu yêu cầu máu</span>
                </div>
            }
            open={visible}
            onOk={handleConfirm}
            onCancel={handleCancel}
            confirmLoading={submitting}
            okText="Xác nhận từ chối"
            cancelText="Hủy bỏ"
            okButtonProps={{ danger: true }}
            destroyOnClose
        >
            <div style={{ display: 'flex', flexDirection: 'column', gap: 14, paddingTop: 8 }}>
                <div style={{ fontSize: 'var(--text-sm)', color: 'var(--c-text-secondary)', lineHeight: 1.6 }}>
                    Bạn đang thực hiện từ chối phiếu <strong>{request.requestCode}</strong> của bệnh nhân{' '}
                    <strong>{request.patientName}</strong> ({request.departmentName || 'Khoa lâm sàng'}).
                </div>

                <Alert
                    type="warning"
                    showIcon
                    message="Lưu ý quan trọng"
                    description="Thao tác này sẽ chuyển phiếu sang trạng thái REJECTED. Nếu phiếu đã giữ chỗ một phần túi máu trước đó, hệ thống sẽ tự động giải phóng toàn bộ túi máu về lại kho khả dụng."
                />

                {errorMsg && <Alert type="error" showIcon message={errorMsg} />}

                <div>
                    <label
                        style={{
                            display: 'block',
                            fontSize: 'var(--text-sm)',
                            fontWeight: 600,
                            marginBottom: 6,
                            color: 'var(--c-text)',
                        }}
                    >
                        Lý do từ chối <span style={{ color: 'var(--c-danger)' }}>*</span>
                    </label>
                    <Input.TextArea
                        rows={3}
                        maxLength={255}
                        showCount
                        placeholder="Nhập chi tiết lý do từ chối phiếu yêu cầu (ví dụ: Thiếu thông tin lâm sàng, không có túi máu tương thích, v.v.)..."
                        value={reason}
                        onChange={(e) => {
                            setReason(e.target.value)
                            if (errorMsg) setErrorMsg(null)
                        }}
                    />
                </div>
            </div>
        </Modal>
    )
}
