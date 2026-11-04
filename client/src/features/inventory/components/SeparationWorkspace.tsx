import { useMemo, useState } from 'react'
import { Alert, Button, Checkbox, InputNumber, Popconfirm, Select, Tag, message } from 'antd'
import { CheckCircleOutlined, PrinterOutlined } from '@ant-design/icons'
import { parseApiError } from '@/services/apiError'
import { fmtDateTime } from '@/features/screening/format'
import { useComponentTypes, useSeparate, useStorageLocations } from '../hooks'
import {
    CHILD_CODES,
    COMPONENT_LABEL,
    addDaysIso,
    childBarcode,
    locationOptions,
    storageHint,
} from '../format'
import type { LabelData } from './BagLabel'
import BagLabel from './BagLabel'
import LabelsPrintModal from './LabelsPrintModal'
import type { ChildCode, ProcessingQueueItem, SeparateResponse } from '../types'

interface Row {
    enabled: boolean
    volume: number | null
    locationId: number | null
}

const INITIAL: Record<ChildCode, Row> = {
    PRBC: { enabled: true, volume: null, locationId: null },
    FFP: { enabled: true, volume: null, locationId: null },
    PLT: { enabled: false, volume: null, locationId: null },
}

interface Props {
    parent: ProcessingQueueItem
    canEdit: boolean
    onDone: () => void
}

export default function SeparationWorkspace({ parent, canEdit, onDone }: Props) {
    const [rows, setRows] = useState(INITIAL)
    const [result, setResult] = useState<SeparateResponse | null>(null)
    const [printOpen, setPrintOpen] = useState(false)

    const separate = useSeparate()
    const { data: locData } = useStorageLocations()
    const { data: types } = useComponentTypes()

    const locations = locData?.items ?? []
    const lifeDays = useMemo(
        () => Object.fromEntries((types ?? []).map((t) => [t.typeCode, t.lifeDays])) as Record<string, number>,
        [types],
    )

    const patch = (code: ChildCode, p: Partial<Row>) =>
        setRows((prev) => ({ ...prev, [code]: { ...prev[code], ...p } }))

    const enabled = CHILD_CODES.filter((c) => rows[c].enabled)
    const total = enabled.reduce((s, c) => s + (rows[c].volume ?? 0), 0)
    const overVolume = total > parent.volumeMl
    const incomplete = enabled.some((c) => !rows[c].volume || !rows[c].locationId)

    const expiryOf = (code: ChildCode) =>
        lifeDays[code] !== undefined ? addDaysIso(parent.collectedAt, lifeDays[code]) : null
    const expiredCodes = enabled.filter((c) => {
        const e = expiryOf(c)
        return e !== null && new Date(e).getTime() <= Date.now()
    })

    const canSubmit = canEdit && enabled.length > 0 && !incomplete && !overVolume && expiredCodes.length === 0

    const submit = async () => {
        try {
            const r = await separate.mutateAsync({
                parentBarcode: parent.barcode,
                components: enabled.map((c) => ({
                    componentType: c,
                    volumeMl: rows[c].volume as number,
                    storageLocationId: rows[c].locationId as number,
                })),
            })
            setResult(r)
            message.success('Điều chế thành công')
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    if (result) {
        const labels: LabelData[] = result.derivedComponents.map((d) => ({
            barcode: d.barcode,
            bloodGroup: d.bloodGroup,
            volumeMl: d.volumeMl,
            collectedAt: d.collectedAt,
            expiredAt: d.expiredAt,
            componentName: d.componentTypeName,
        }))

        return (
            <section className="inv-card">
                <header className="inv-card__head">
                    <h2><CheckCircleOutlined style={{ color: 'var(--c-primary)' }} /> Điều chế hoàn tất</h2>
                    <p>Túi gốc {result.parentBarcode} chuyển sang {result.parentStatus}. Kỹ thuật viên: {result.technicianName}.</p>
                </header>
                <div className="inv-card__body inv-result">
                    <Alert type="success" showIcon title={result.message} />
                    <div className="inv-label-list">
                        {labels.map((l) => (
                            <BagLabel key={l.barcode} bag={l} />
                        ))}
                    </div>
                    <div className="inv-actions">
                        <Button type="primary" icon={<PrinterOutlined />} onClick={() => setPrintOpen(true)}>
                            In tất cả tem ({labels.length})
                        </Button>
                        <Button onClick={onDone}>Điều chế túi khác</Button>
                    </div>
                    <LabelsPrintModal open={printOpen} labels={labels} onClose={() => setPrintOpen(false)} />
                </div>
            </section>
        )
    }

    return (
        <section className="inv-card">
            <header className="inv-card__head">
                <h2>Điều chế túi {parent.barcode}</h2>
                <p>Chọn các chế phẩm cần tạo. Mã túi con tự sinh theo quy ước mã túi mẹ + mã chế phẩm.</p>
            </header>
            <div className="inv-card__body">
                <dl className="inv-parent">
                    <div>
                        <dt>Nhóm máu</dt>
                        <dd>
                            {parent.bloodGroup} <Tag color="blue">Kế thừa từ túi gốc</Tag>
                        </dd>
                    </div>
                    <div><dt>Thể tích</dt><dd>{parent.volumeMl} ml</dd></div>
                    <div><dt>Thu thập</dt><dd>{fmtDateTime(parent.collectedAt)}</dd></div>
                    <div><dt>Nguồn</dt><dd>{parent.donorName || '—'}</dd></div>
                </dl>

                <div style={{ marginTop: 16 }}>
                    {CHILD_CODES.map((code) => {
                        const row = rows[code]
                        const expiry = expiryOf(code)
                        const expired = expiry !== null && new Date(expiry).getTime() <= Date.now()
                        const opts = locationOptions(locations, code)

                        return (
                            <div key={code} className={`inv-comp${row.enabled ? '' : ' is-off'}`}>
                                <Checkbox
                                    checked={row.enabled}
                                    disabled={!canEdit}
                                    onChange={(e) => patch(code, { enabled: e.target.checked })}
                                >
                                    <b>{code}</b> · {COMPONENT_LABEL[code]}
                                </Checkbox>
                                <InputNumber
                                    min={1}
                                    max={parent.volumeMl}
                                    placeholder="Thể tích (ml)"
                                    disabled={!row.enabled || !canEdit}
                                    value={row.volume}
                                    onChange={(v) => patch(code, { volume: v === null ? null : Number(v) })}
                                    style={{ width: '100%' }}
                                />
                                <Select
                                    placeholder={`Tủ phù hợp (${storageHint(code)})`}
                                    disabled={!row.enabled || !canEdit}
                                    value={row.locationId}
                                    options={opts}
                                    notFoundContent="Không có tủ phù hợp đang trống"
                                    onChange={(v) => patch(code, { locationId: v })}
                                />
                                {row.enabled && (
                                    <div className={`inv-comp__preview${expired ? ' is-bad' : ''}`}>
                                        Mã túi: <span className="inv-mono">{childBarcode(parent.barcode, code)}</span>
                                        {expiry && (
                                            <>
                                                {' · '}Hạn dùng: {fmtDateTime(expiry)}
                                                {expired && ' (đã quá hạn, không thể tạo)'}
                                            </>
                                        )}
                                    </div>
                                )}
                            </div>
                        )
                    })}
                </div>

                <div className="inv-total">
                    <span>Tổng thể tích chế phẩm</span>
                    <b style={{ color: overVolume ? 'var(--c-danger)' : undefined }}>
                        {total} / {parent.volumeMl} ml
                    </b>
                </div>

                {overVolume && (
                    <Alert style={{ marginTop: 12 }} type="error" showIcon title="Tổng thể tích các chế phẩm vượt thể tích túi gốc." />
                )}
                {!canEdit && (
                    <Alert style={{ marginTop: 12 }} type="warning" showIcon title="Bạn chỉ có quyền xem hàng chờ điều chế." />
                )}

                <div className="inv-actions" style={{ marginTop: 16 }}>
                    <Popconfirm
                        title="Xác nhận điều chế?"
                        description="Túi gốc sẽ chuyển sang PROCESSED và không thể hoàn tác."
                        okText="Xác nhận"
                        cancelText="Hủy"
                        disabled={!canSubmit}
                        onConfirm={submit}
                    >
                        <Button type="primary" size="large" disabled={!canSubmit} loading={separate.isPending}>
                            Xác nhận điều chế &amp; sinh mã
                        </Button>
                    </Popconfirm>
                </div>
            </div>
        </section>
    )
}