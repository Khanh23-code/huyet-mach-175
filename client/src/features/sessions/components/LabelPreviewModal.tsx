import { useState } from 'react'
import { createPortal } from 'react-dom'
import { Button, InputNumber, Modal } from 'antd'
import { PrinterOutlined } from '@ant-design/icons'
import BloodBagLabel from './BloodBagLabel'
import type { SessionBloodBag } from '../types'

interface Props {
    open: boolean
    bag: SessionBloodBag
    onClose: () => void
}

const MAX_COPIES = 20

export default function LabelPreviewModal({ open, bag, onClose }: Props) {
    const [copies, setCopies] = useState(1)
    const count = Math.min(Math.max(copies || 1, 1), MAX_COPIES)

    return (
        <>
            <Modal
                open={open}
                onCancel={onClose}
                title="Xem trước tem nhãn"
                width={520}
                destroyOnHidden
                footer={
                    <div className="ss-modal-foot">
                        <label className="ss-copies">
                            Số bản in
                            <InputNumber
                                min={1}
                                max={MAX_COPIES}
                                value={copies}
                                onChange={(v) => setCopies(Number(v) || 1)}
                            />
                        </label>
                        <Button onClick={onClose}>Đóng</Button>
                        <Button type="primary" icon={<PrinterOutlined />} onClick={() => window.print()}>
                            In ngay{count > 1 ? ` (${count})` : ''}
                        </Button>
                    </div>
                }
            >
                <BloodBagLabel bag={bag} />
            </Modal>

            {open &&
                createPortal(
                    <div className="ss-print-root" aria-hidden>
                        {Array.from({ length: count }, (_, i) => (
                            <BloodBagLabel key={i} bag={bag} />
                        ))}
                    </div>,
                    document.body,
                )}
        </>
    )
}