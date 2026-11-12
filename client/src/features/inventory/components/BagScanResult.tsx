import { useMemo, useState } from 'react'
import { Alert, App, Button, Input, Select } from 'antd'
import { CheckCircleFilled, ClockCircleOutlined } from '@ant-design/icons'
import Can from '@/features/auth/components/Can'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { useStorageLocations } from '../hooks'
import { useTransitionBag } from '../stockHooks'
import { addOffline } from '../offlineQueue'
import {
    AUTO_STATUSES, COMING_SOON_STATUSES, MANUAL_STATUSES, allowedManualTargets, fmtDateTimeSec, isNetworkError, statusMeta,
} from '../stockFormat'
import type { BagScan, BagStatus } from '../stockTypes'
import { BagStatusTag, BloodGroupTag } from './StockBadges'

interface Props {
    bag: BagScan
    /** Sau khi chuyển trạng thái thành công: quét lại để làm mới. */
    onChanged: (barcode: string) => void
    /** Lệnh bị mất mạng và đã vào hàng đợi ngoại tuyến. */
    onQueued: () => void
}

const SOON = 'Sắp ra mắt'

/** Dùng `key={barcode + currentStatus}` ở chỗ gọi để reset form khi đổi túi/trạng thái. */
export default function BagScanResult({ bag, onChanged, onQueued }: Props) {
    const { message } = App.useApp()
    const transition = useTransitionBag()
    const { data: locs } = useStorageLocations()

    const targets = useMemo(() => allowedManualTargets(bag.currentStatus, bag.isExpired), [bag])
    const next = bag.nextStatus as BagStatus | null | undefined
    const [target, setTarget] = useState<BagStatus | undefined>(next && targets.includes(next) ? next : targets[0])
    const [locationId, setLocationId] = useState<number>()
    const [notes, setNotes] = useState('')

    const options = [
        ...MANUAL_STATUSES.map((s) => ({ value: s, label: statusMeta(s).label, disabled: !targets.includes(s) })),
        ...(['RESERVED', 'ISSUED'] as BagStatus[]).map((s) => ({
            value: s, label: `${statusMeta(s).label} (${SOON})`, disabled: true,
        })),
    ]
    const locOptions = (locs?.items ?? [])
        .filter((l) => l.isActive && !l.isFull)
        .map((l) => ({ value: l.locationId, label: `${l.refrigeratorName} - ${l.shelfNumber} (${l.targetTemperature}°C)` }))
    const needsLocation = target === 'READY_TESTING' || target === 'AVAILABLE'
    const terminal = bag.isDiscarded || bag.currentStatus === 'TRANSFUSED'

    const submit = async () => {
        if (!target) return
        const body = {
            barcode: bag.barcode,
            targetStatus: target,
            storageLocationId: needsLocation ? locationId : undefined,
            notes: notes.trim() || undefined,
        }
        try {
            const r = await transition.mutateAsync(body)
            message.success(r.message || `Đã chuyển sang "${r.currentStatusLabel}"`)
            onChanged(bag.barcode)
        } catch (e) {
            if (isNetworkError(e)) {
                addOffline(body)
                message.warning('Mất kết nối: đã lưu lệnh vào hàng đợi ngoại tuyến, sẽ gửi lại khi có mạng.')
                onQueued()
            } else {
                message.error(parseApiError(e).message)
            }
        }
    }

    const nextHint = (() => {
        if (!next || terminal) return null
        if (AUTO_STATUSES.includes(next))
            return `Bước kế tiếp "${bag.nextStatusLabel ?? statusMeta(next).label}" được cập nhật tự động ${next === 'PROCESSED' ? 'khi điều chế chế phẩm' : 'khi có kết quả xét nghiệm'}, không cập nhật thủ công tại đây.`
        if (COMING_SOON_STATUSES.includes(next))
            return `Bước kế tiếp "${bag.nextStatusLabel ?? statusMeta(next).label}" thuộc luồng cấp phát, ${SOON.toLowerCase()}.`
        return null
    })()

    return (
        <section className="inv-card inv-card__body scan-result">
            <div className="scan-result__head">
                <div>
                    <div className="inv-mono scan-result__code">{bag.barcode}</div>
                    <div className="stk-muted">{bag.componentType} · {bag.volumeMl} ml</div>
                </div>
                <div className="inv-actions">
                    <BloodGroupTag value={bag.bloodGroup} />
                    <BagStatusTag status={bag.currentStatus} label={bag.currentStatusLabel} />
                </div>
            </div>

            <dl className="inv-parent" style={{ gridTemplateColumns: 'repeat(3, 1fr)' }}>
                <div><dt>Vị trí lưu trữ</dt><dd>{bag.storageLocation?.locationName ?? '—'}</dd></div>
                <div><dt>Thu thập</dt><dd>{fmtDateTimeSec(bag.collectedAt)}</dd></div>
                <div><dt>Hết hạn</dt><dd style={bag.isExpired ? { color: 'var(--c-danger)' } : undefined}>{fmtDateTimeSec(bag.expiredAt)}</dd></div>
            </dl>

            {bag.isDiscarded && <Alert type="error" showIcon title="Túi máu đã bị tiêu hủy, không thể cập nhật trạng thái." />}
            {bag.isLocked && !bag.isDiscarded && <Alert type="warning" showIcon title="Túi máu đang bị tạm khóa." />}
            {bag.isExpired && !bag.isDiscarded && <Alert type="error" showIcon title="Túi máu đã hết hạn sử dụng: không thể nhập kho hoặc cấp phát, chỉ được tạm khóa để xử lý hủy." />}

            <ol className="scan-steps" aria-label="Vòng đời túi máu">
                {bag.timeline.map((s) => {
                    const state = s.completed ? 'done' : s.step === bag.currentStep ? 'current' : 'todo'
                    const soon = s.step >= 5 && !s.completed
                    return (
                        <li key={s.step} className={`scan-step is-${state}${soon ? ' is-soon' : ''}`}>
                            <span className="scan-step__dot">{s.completed ? <CheckCircleFilled /> : s.step === bag.currentStep ? <ClockCircleOutlined /> : s.step}</span>
                            <b>{s.name}</b>
                            <small>{s.completed ? fmtDateTimeSec(s.timestamp) : soon ? SOON : 'Chưa thực hiện'}</small>
                        </li>
                    )
                })}
            </ol>

            {nextHint && <Alert type="info" showIcon title={nextHint} />}

            <Can
                do={PERMISSIONS.INVENTORY_EDIT}
                fallback={<Alert type="warning" showIcon title="Bạn chỉ có quyền xem, không thể cập nhật trạng thái." />}
            >
                {!terminal && (
                    targets.length === 0 ? (
                        <Alert type="info" showIcon title="Không có trạng thái nào có thể cập nhật thủ công cho túi này." />
                    ) : (
                        <div className="scan-form">
                            <div className="stk-field">
                                <label>Chuyển sang trạng thái</label>
                                <Select value={target} onChange={setTarget} options={options} style={{ width: '100%' }} />
                            </div>
                            {needsLocation && (
                                <div className="stk-field">
                                    <label>Vị trí lưu trữ (tùy chọn)</label>
                                    <Select
                                        allowClear showSearch optionFilterProp="label" placeholder="Giữ nguyên vị trí hiện tại"
                                        options={locOptions} value={locationId} onChange={setLocationId} style={{ width: '100%' }}
                                    />
                                </div>
                            )}
                            <div className="stk-field scan-form__notes">
                                <label>Ghi chú</label>
                                <Input maxLength={255} value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="Không bắt buộc" />
                            </div>
                            <Button type="primary" size="large" loading={transition.isPending} disabled={!target} onClick={submit}>
                                Cập nhật trạng thái
                            </Button>
                        </div>
                    )
                )}
            </Can>
        </section>
    )
}