import { useState } from 'react'
import { Alert, Button, Input, Modal, message } from 'antd'
import {
    CheckOutlined,
    ExclamationCircleOutlined,
    PlayCircleFilled,
    PrinterOutlined,
    ReloadOutlined,
} from '@ant-design/icons'
import { initials } from '@/features/donors/format'
import { fmtBlood, fmtDateTime } from '@/features/screening/format'
import { parseApiError } from '@/services/apiError'
import { useCompleteCollection, useStartCollection } from '../hooks'
import { useElapsed } from '../useElapsed'
import { SESSION_META, VOLUME_CHOICES, fmtElapsed, parseUtc, volumeMl } from '../format'
import BarcodeSvg from './BarcodeSvg'
import LabelPreviewModal from './LabelPreviewModal'
import type { SessionDetail } from '../types'

interface Props {
    detail: SessionDetail
    canEdit: boolean
    onClosed: () => void
}

export default function SessionWorkspace({ detail, canEdit, onClosed }: Props) {
    const startM = useStartCollection()
    const completeM = useCompleteCollection()

    const waiting = detail.status === 'WAITING_COLLECTION'
    const bag = detail.bloodBag ?? null
    const editable = canEdit && waiting
    const missingBlood = !detail.donor.bloodType || !detail.donor.rhFactor

    const [baseMs, setBaseMs] = useState<number | null>(() => (waiting && bag ? parseUtc(bag.collectedAt) : null))
    const [volume, setVolume] = useState(() => volumeMl(detail.targetVolumeMl))
    const [note, setNote] = useState(detail.collectionIncident ?? '')
    const [noteError, setNoteError] = useState(false)
    const [previewOpen, setPreviewOpen] = useState(false)

    const running = waiting && baseMs !== null
    const base = waiting ? baseMs : bag ? parseUtc(bag.collectedAt) : null
    const end = !waiting && detail.completedAt ? parseUtc(detail.completedAt) : null
    const elapsed = useElapsed(base, end, running)

    const shownVolume = waiting ? volume : (detail.actualVolumeMl ?? 0)
    const meta = SESSION_META[detail.status]

    const handleStart = () => {
        startM.mutate(detail.sessionId, {
            onSuccess: (res) => {
                setBaseMs(res.isNewBag ? Date.now() : parseUtc(res.collectedAt))
                message.success(res.message)
                setPreviewOpen(true)
            },
            onError: (e) => message.error(parseApiError(e).message),
        })
    }

    const close = (status: 'COMPLETED' | 'ABORTED') => {
        completeM.mutate(
            {
                sessionId: detail.sessionId,
                body:
                    status === 'COMPLETED'
                        ? { status, actualVolumeMl: volume, collectionIncident: note.trim() || undefined }
                        : { status, collectionIncident: note.trim() },
            },
            {
                onSuccess: (res) => {
                    message.success(res.message)
                    onClosed()
                },
                onError: (e) => message.error(parseApiError(e).message),
            },
        )
    }

    const handleAbort = () => {
        if (!note.trim()) {
            setNoteError(true)
            message.warning('Vui lòng mô tả sự cố / phản ứng bất lợi trước khi ghi nhận')
            return
        }
        Modal.confirm({
            title: 'Ghi nhận phản ứng bất lợi?',
            icon: <ExclamationCircleOutlined />,
            content: 'Phiên lấy máu sẽ bị hủy và túi máu đã tạo (nếu có) sẽ bị loại bỏ.',
            okText: 'Ghi nhận',
            cancelText: 'Quay lại',
            okButtonProps: { danger: true },
            onOk: () => close('ABORTED'),
        })
    }

    return (
        <>
            <div className="ss-card ss-donor">
                <span className="ss-avatar ss-avatar--lg">{initials(detail.donor.fullName)}</span>
                <div className="ss-donor-main">
                    <span className="ss-eyebrow">{waiting ? 'ĐẠT TIÊU CHUẨN' : meta.label.toUpperCase()}</span>
                    <h2 className="ss-donor-name">{detail.donor.fullName}</h2>
                    <span className="ss-muted">
                        {detail.donor.donorCode} · Nhóm máu{' '}
                        <b className="ss-blood">{fmtBlood(detail.donor.bloodType, detail.donor.rhFactor)}</b>
                    </span>
                </div>
            </div>

            <div className="ss-card ss-panel">
                <h3 className="ss-h">Thể tích thu thập</h3>
                <div className="ss-chips" role="radiogroup" aria-label="Thể tích thu thập">
                    {VOLUME_CHOICES.map((v) => (
                        <button
                            type="button"
                            key={v}
                            role="radio"
                            aria-checked={shownVolume === v}
                            disabled={!editable}
                            className={`ss-chip${shownVolume === v ? ' is-active' : ''}`}
                            onClick={() => setVolume(v)}
                        >
                            {v} ml
                        </button>
                    ))}
                    {!waiting && shownVolume > 0 && !VOLUME_CHOICES.includes(shownVolume) && (
                        <span className="ss-chip is-active">{shownVolume} ml</span>
                    )}
                </div>
                {waiting && (
                    <div className="ss-muted ss-hint">
                        Chỉ định của bác sĩ: {volumeMl(detail.targetVolumeMl)} ml. Chọn thể tích thực tế thu được trước khi hoàn tất.
                    </div>
                )}

                {editable && missingBlood && !bag && (
                    <Alert
                        type="error"
                        showIcon
                        title="Người hiến chưa có nhóm máu"
                        description="Không thể bắt đầu lấy máu và in tem. Cần cập nhật nhóm máu (ABO + Rh) ở bước khám sàng lọc."
                    />
                )}
                <h3 className="ss-h">Bộ đếm thời gian phiên</h3>
                <div className="ss-timer">
                    <div className="ss-timer-value" aria-live="off">{fmtElapsed(elapsed)}</div>
                    <div className="ss-muted">mm:ss — Thời gian lấy máu</div>
                    {editable && (
                        <div className="ss-timer-actions">
                            <Button
                                type="primary"
                                icon={<PlayCircleFilled />}
                                loading={startM.isPending}
                                disabled={baseMs !== null || !!bag || missingBlood}
                                onClick={handleStart}
                            >
                                {baseMs !== null || bag ? 'Đang lấy máu' : 'Bắt đầu'}
                            </Button>
                            <Button icon={<ReloadOutlined />} disabled={!running} onClick={() => setBaseMs(Date.now())}>
                                Đặt lại
                            </Button>
                        </div>
                    )}
                </div>

                {bag && (
                    <>
                        <h3 className="ss-h">Nhãn túi máu</h3>
                        <div className="ss-bag">
                            <div className="ss-bag-code">
                                <BarcodeSvg value={bag.barcode} height={48} />
                                <code className="ss-mono">{bag.barcode}</code>
                            </div>
                            <div className="ss-bag-info">
                                <span>
                                    Nhóm máu <b className="ss-blood">{bag.bloodGroup}</b>
                                </span>
                                <span>Thể tích túi: <b>{bag.volumeMl} ml</b></span>
                                <span>Thu thập: {fmtDateTime(bag.collectedAt)}</span>
                                <Button icon={<PrinterOutlined />} onClick={() => setPreviewOpen(true)}>
                                    Xem trước &amp; in tem
                                </Button>
                            </div>
                        </div>
                    </>
                )}

                <div className="ss-field">
                    <label htmlFor="ss-note">Ghi chú sự cố / quan sát</label>
                    <Input.TextArea
                        id="ss-note"
                        rows={3}
                        value={note}
                        maxLength={500}
                        showCount
                        disabled={!editable}
                        status={noteError ? 'error' : undefined}
                        placeholder="Mô tả sự cố, quan sát bất thường trong quá trình lấy máu..."
                        onChange={(e) => {
                            setNote(e.target.value)
                            setNoteError(false)
                        }}
                    />
                    {noteError && <span className="ss-err">Bắt buộc nhập khi ghi nhận phản ứng bất lợi</span>}
                </div>

                {waiting && (
                    <Alert
                        type="warning"
                        showIcon
                        title="Thời gian lấy máu tiêu chuẩn: 8–10 phút"
                        description="Dừng ngay nếu người hiến có dấu hiệu bất thường."
                    />
                )}

                {!waiting && (
                    <Alert
                        type={detail.status === 'COMPLETED' ? 'success' : 'error'}
                        showIcon
                        title={detail.status === 'COMPLETED' ? 'Phiên lấy máu đã hoàn tất' : 'Phiên lấy máu đã bị hủy do sự cố'}
                        description={`${detail.phlebotomistName ?? '—'} · ${fmtDateTime(detail.completedAt)}`}
                    />
                )}

                {editable && (
                    <div className="ss-actions">
                        <Button danger type="primary" loading={completeM.isPending} onClick={handleAbort}>
                            Ghi nhận phản ứng bất lợi
                        </Button>
                        <Button
                            type="primary"
                            icon={<CheckOutlined />}
                            loading={completeM.isPending}
                            disabled={!bag}
                            title={!bag ? 'Bấm "Bắt đầu" và in tem trước khi hoàn tất' : undefined}
                            onClick={() => close('COMPLETED')}
                        >
                            Hoàn tất phiên hiến
                        </Button>
                    </div>
                )}

                {!canEdit && waiting && <div className="ss-muted">Bạn chỉ có quyền xem phiên lấy máu.</div>}
            </div>

            {bag && <LabelPreviewModal open={previewOpen} bag={bag} onClose={() => setPreviewOpen(false)} />}
        </>
    )
}