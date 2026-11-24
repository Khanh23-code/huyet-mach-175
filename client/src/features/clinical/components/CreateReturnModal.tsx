import React, { useState } from 'react'
import { Modal, Form, Input, Checkbox, Button, Alert } from 'antd'
import { RollbackOutlined } from '@ant-design/icons'
import { BloodGroupTag } from '@/shared/components'
import { useCreateReturn } from '../hooks'
import type { PendingReceiptBagDto } from '../types'
import './CreateReturnModal.css'

interface CreateReturnModalProps {
    open: boolean
    onClose: () => void
    bag: PendingReceiptBagDto | null
}

export const CreateReturnModal: React.FC<CreateReturnModalProps> = ({ open, onClose, bag }) => {
    const [returnReason, setReturnReason] = useState('')
    const [checklist, setChecklist] = useState({
        isUnusedFullVolume: true,
        isBagIntact: true,
        isLabelClear: true,
        isStoredUnder30Mins: true,
    })

    const returnMutation = useCreateReturn()

    const allChecked =
        checklist.isUnusedFullVolume &&
        checklist.isBagIntact &&
        checklist.isLabelClear &&
        checklist.isStoredUnder30Mins

    const handleSubmit = async () => {
        if (!bag) return

        try {
            await returnMutation.mutateAsync({
                allocationId: bag.allocationId,
                returnReason: returnReason.trim(),
                checklist,
            })
            onClose()
            setReturnReason('')
        } catch {
            // Handled in hook
        }
    }

    if (!bag) return null

    return (
        <Modal
            open={open}
            onCancel={onClose}
            footer={null}
            width={560}
            centered
            destroyOnClose
            className="create-return-modal"
        >
            <div className="return-modal__header">
                <div className="return-modal__icon">
                    <RollbackOutlined />
                </div>
                <div>
                    <h3 className="return-modal__title">Lập phiếu hoàn trả túi máu chưa sử dụng</h3>
                    <p className="return-modal__subtitle">
                        Phiếu sẽ khóa túi máu (LOCKED) và gửi yêu cầu thẩm định hoàn trả về Ngân hàng máu.
                    </p>
                </div>
            </div>

            <div className="return-modal__info-box">
                <div className="return-modal__info-row">
                    <span>Mã túi máu</span>
                    <strong style={{ color: 'var(--c-primary)', fontFamily: 'var(--font-mono)' }}>{bag.barcode}</strong>
                </div>
                <div className="return-modal__info-row">
                    <span>Người bệnh</span>
                    <span>{bag.patientName} ({bag.patientCode})</span>
                </div>
                <div className="return-modal__info-row">
                    <span>Chế phẩm</span>
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
                        <span>{bag.componentName} - {bag.volumeMl} ml</span>
                        <BloodGroupTag value={bag.bloodType} size="sm" />
                    </span>
                </div>
            </div>

            <Alert
                type="warning"
                message="Quy định an toàn sinh học hoàn trả túi máu"
                description="Túi máu hoàn trả bắt buộc phải thỏa mãn đầy đủ 4 tiêu chí an toàn dưới đây trước khi vận chuyển về Ngân hàng máu."
                showIcon
                style={{ marginBottom: 16 }}
            />

            <Form layout="vertical">
                <div className="return-modal__checklist">
                    <div className="return-modal__checklist-title">Bảng kiểm an toàn (4 điều kiện bắt buộc)</div>
                    <Checkbox
                        checked={checklist.isUnusedFullVolume}
                        onChange={(e) => setChecklist({ ...checklist, isUnusedFullVolume: e.target.checked })}
                    >
                        Túi máu nguyên vẹn, còn đủ thể tích quy chuẩn, chưa bị trích dẫn hoặc đâm kim truyền.
                    </Checkbox>
                    <Checkbox
                        checked={checklist.isBagIntact}
                        onChange={(e) => setChecklist({ ...checklist, isBagIntact: e.target.checked })}
                    >
                        Vỏ túi không rách vỡ, rò rỉ, dây nối túi máu còn hàn kín an toàn.
                    </Checkbox>
                    <Checkbox
                        checked={checklist.isLabelClear}
                        onChange={(e) => setChecklist({ ...checklist, isLabelClear: e.target.checked })}
                    >
                        Nhãn túi máu rõ ràng, mã vạch còn nguyên vẹn, không bị nhòe mờ hoặc rách góc.
                    </Checkbox>
                    <Checkbox
                        checked={checklist.isStoredUnder30Mins}
                        onChange={(e) => setChecklist({ ...checklist, isStoredUnder30Mins: e.target.checked })}
                    >
                        Thời gian rời tủ bảo quản chuyên dụng dưới 30 phút, bảo quản đúng quy cách vận chuyển.
                    </Checkbox>
                </div>

                <Form.Item label="Lý do hoàn trả máu" required style={{ marginTop: 16 }}>
                    <Input.TextArea
                        rows={3}
                        maxLength={255}
                        showCount
                        placeholder="Ví dụ: Người bệnh hoãn phẫu thuật / thay đổi phác đồ điều trị..."
                        value={returnReason}
                        onChange={(e) => setReturnReason(e.target.value)}
                    />
                </Form.Item>

                <div className="return-modal__footer">
                    <Button onClick={onClose} disabled={returnMutation.isPending}>
                        Hủy
                    </Button>
                    <Button
                        type="primary"
                        danger
                        loading={returnMutation.isPending}
                        onClick={handleSubmit}
                        disabled={!allChecked || !returnReason.trim()}
                        icon={<RollbackOutlined />}
                    >
                        Xác nhận lập phiếu hoàn trả
                    </Button>
                </div>
            </Form>
        </Modal>
    )
}
