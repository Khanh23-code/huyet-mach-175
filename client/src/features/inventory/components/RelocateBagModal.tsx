import { useMemo, useState } from 'react'
import { Alert, App, Button, Input, Modal, Select } from 'antd'
import { parseApiError } from '@/services/apiError'
import { useStorageLocations } from '../hooks'
import { useRelocateBag } from '../stockHooks'
import { isLocationSuitable } from '../format'
import type { ComponentCode } from '../types'
import type { RelocatableBag } from '../stockTypes'
import { BloodGroupTag } from './StockBadges'

interface Props {
    bag: RelocatableBag | null
    onClose: () => void
}

export default function RelocateBagModal({ bag, onClose }: Props) {
    const { message } = App.useApp()
    const { data, isLoading } = useStorageLocations()
    const relocate = useRelocateBag()
    const [target, setTarget] = useState<number>()
    const [note, setNote] = useState('')

    const options = useMemo(
        () =>
            (data?.items ?? [])
                .filter((l) => l.isActive && !l.isFull && l.locationId !== bag?.storageLocationId)
                .map((l) => {
                    const ok = isLocationSuitable(bag?.componentTypeCode as ComponentCode, l.targetTemperature)
                    return {
                        value: l.locationId,
                        label: `${l.refrigeratorName} - ${l.shelfNumber} (${l.targetTemperature}°C)${ok === false ? ' · nhiệt độ không phù hợp' : ''}`,
                    }
                }),
        [data, bag],
    )

    const close = () => {
        setTarget(undefined)
        setNote('')
        onClose()
    }

    const submit = async () => {
        if (!bag || target === undefined) return
        try {
            const r = await relocate.mutateAsync({
                bagId: bag.bagId,
                body: { newStorageLocationId: target, note: note.trim() || undefined },
            })
            message.success(r.message || 'Đã đổi vị trí lưu trữ')
            close()
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    return (
        <Modal
            open={bag !== null}
            onCancel={close}
            title="Đổi vị trí lưu trữ túi máu"
            width={480}
            destroyOnHidden
            footer={
                <div className="inv-actions" style={{ justifyContent: 'flex-end' }}>
                    <Button onClick={close} disabled={relocate.isPending}>Hủy</Button>
                    <Button type="primary" disabled={target === undefined} loading={relocate.isPending} onClick={submit}>
                        Xác nhận chuyển
                    </Button>
                </div>
            }
        >
            {bag && (
                <div className="stk-form">
                    <dl className="inv-parent" style={{ gridTemplateColumns: 'repeat(3, 1fr)' }}>
                        <div><dt>Barcode</dt><dd className="inv-mono">{bag.barcode}</dd></div>
                        <div><dt>Nhóm máu</dt><dd><BloodGroupTag value={bag.bloodGroup} /></dd></div>
                        <div><dt>Vị trí hiện tại</dt><dd>{bag.storageLocationName || '—'}</dd></div>
                    </dl>
                    <div className="stk-field">
                        <label>Vị trí mới <span className="stk-req">*</span></label>
                        <Select
                            showSearch
                            loading={isLoading}
                            placeholder="Chọn tủ / ngăn còn chỗ"
                            options={options}
                            value={target}
                            onChange={setTarget}
                            optionFilterProp="label"
                            notFoundContent="Không còn vị trí khả dụng"
                        />
                    </div>
                    <div className="stk-field">
                        <label>Lý do / ghi chú</label>
                        <Input.TextArea rows={3} maxLength={255} showCount value={note} onChange={(e) => setNote(e.target.value)} />
                    </div>
                    <Alert type="info" showIcon title="Lịch sử di chuyển được ghi vào nhật ký hệ thống." />
                </div>
            )}
        </Modal>
    )
}