import { useState } from 'react'
import { createPortal } from 'react-dom'
import { Button, InputNumber, Modal } from 'antd'
import { PrinterOutlined } from '@ant-design/icons'
import BagLabel, { type LabelData } from './BagLabel'

interface Props {
    open: boolean
    labels: LabelData[]
    onClose: () => void
}

const MAX_COPIES = 20

export default function LabelsPrintModal({ open, labels, onClose }: Props) {
    const [copies, setCopies] = useState(1)
    const count = Math.min(Math.max(copies || 1, 1), MAX_COPIES)

    return (
        <>
            <Modal
                open={open}
                onCancel={onClose}
                title={`Xem trước tem nhãn (${labels.length} túi)`}
                width={520}
                destroyOnHidden
                footer={
                    <div className="ss-modal-foot">
                        <label className="ss-copies">
                            Số bản mỗi túi
                            <InputNumber
                                min={1}
                                max={MAX_COPIES}
                                value={copies}
                                onChange={(v) => setCopies(Number(v) || 1)}
                            />
                        </label>
                        <Button onClick={onClose}>Đóng</Button>
                        <Button type="primary" icon={<PrinterOutlined />} onClick={() => window.print()}>
                            In ngay ({labels.length * count} tem)
                        </Button>
                    </div>
                }
            >
                <div className="inv-label-list">
                    {labels.map((l) => (
                        <BagLabel key={l.barcode} bag={l} />
                    ))}
                </div>
            </Modal>

            {open &&
                createPortal(
                    <div className="ss-print-root" aria-hidden>
                        {labels.flatMap((l) =>
                            Array.from({ length: count }, (_, i) => (
                                <BagLabel key={`${l.barcode}-${i}`} bag={l} />
                            )),
                        )}
                    </div>,
                    document.body,
                )}
        </>
    )
}