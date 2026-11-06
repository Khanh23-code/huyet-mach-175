import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Alert, Button, Popconfirm, Segmented, Select, Tag, message } from 'antd'
import { CheckCircleOutlined, CloseCircleOutlined, WarningOutlined } from '@ant-design/icons'
import { parseApiError } from '@/services/apiError'
import { fmtDateTime } from '@/features/screening/format'
import { useRecordLabTest } from '../labTestsHooks'
import { ABO_OPTIONS, INDICATORS, RH_OPTIONS, formatGroup } from '../labTestsFormat'
import type { AboType, IndicatorKey, LabQueueItem, LabTestResult, RhValue, TestResultValue } from '../labTestsTypes'

interface Props {
    item: LabQueueItem
    canEdit: boolean
    canDispose: boolean
    onDone: () => void
}

type Draft = Record<IndicatorKey, TestResultValue | null>

const EMPTY: Draft = {
    hivResult: null,
    hbvResult: null,
    hcvResult: null,
    syphilisResult: null,
    irregularAntibody: null,
}

const RESULT_OPTIONS = [
    { label: 'Âm tính', value: 'NEGATIVE' },
    { label: 'Dương tính', value: 'POSITIVE' },
]

export default function LabTestWorkspace({ item, canEdit, canDispose, onDone }: Props) {
    const navigate = useNavigate()
    const record = useRecordLabTest()

    const [draft, setDraft] = useState<Draft>(EMPTY)
    const [abo, setAbo] = useState<AboType | null>(null)
    const [rh, setRh] = useState<RhValue | null>(null)
    const [result, setResult] = useState<LabTestResult | null>(null)

    const allFilled = INDICATORS.every((i) => draft[i.key] !== null)
    const positives = INDICATORS.filter((i) => draft[i.key] === 'POSITIVE')
    const verdict: 'PASSED' | 'FAILED' | null = !allFilled ? null : positives.length > 0 ? 'FAILED' : 'PASSED'
    const groupReady = abo !== null && rh !== null
    const confirmedGroup = groupReady ? formatGroup(abo, rh) : null
    const groupMismatch = confirmedGroup !== null && confirmedGroup !== item.expectedBloodGroup
    const canSubmit = canEdit && verdict !== null && groupReady

    const submit = async () => {
        if (!canSubmit || abo === null || rh === null) return
        try {
            const r = await record.mutateAsync({
                barcode: item.barcode,
                hivResult: draft.hivResult as TestResultValue,
                hbvResult: draft.hbvResult as TestResultValue,
                hcvResult: draft.hcvResult as TestResultValue,
                syphilisResult: draft.syphilisResult as TestResultValue,
                irregularAntibody: draft.irregularAntibody as TestResultValue,
                confirmedBloodType: abo,
                confirmedRhFactor: rh,
            })
            setResult(r)
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    if (result) {
        const failed = result.overallConclusion === 'FAILED'
        return (
            <section className="inv-card">
                <header className="inv-card__head">
                    <h2>
                        {failed ? (
                            <CloseCircleOutlined style={{ color: 'var(--c-danger)' }} />
                        ) : (
                            <CheckCircleOutlined style={{ color: 'var(--c-primary)' }} />
                        )}{' '}
                        {failed ? 'Túi máu không đạt chuẩn' : 'Túi máu đạt chuẩn'}
                    </h2>
                    <p>
                        Túi {result.barcode} · Nhóm máu khẳng định {result.confirmedBloodGroup} · KTV {result.technicianName} ·{' '}
                        {fmtDateTime(result.testedAt)}
                    </p>
                </header>
                <div className="inv-card__body inv-result">
                    <Alert
                        type={failed ? 'error' : 'success'}
                        showIcon
                        title={result.message}
                        description={failed ? result.discardReason : undefined}
                    />
                    <div className="inv-actions">
                        {failed && canDispose && (
                            <Button
                                type="primary"
                                danger
                                onClick={() =>
                                    navigate(`/inventory/disposals?barcode=${encodeURIComponent(result.barcode)}`)
                                }
                            >
                                Lập biên bản hủy túi này
                            </Button>
                        )}
                        <Button onClick={onDone}>Xét nghiệm túi khác</Button>
                    </div>
                    {failed && !canDispose && (
                        <Alert
                            type="warning"
                            showIcon
                            title="Túi đã bị khóa cách ly. Chuyển cho nhân viên có quyền lập biên bản tiêu hủy."
                        />
                    )}
                </div>
            </section>
        )
    }

    return (
        <section className="inv-card">
            <header className="inv-card__head">
                <h2>Nhập kết quả túi {item.barcode}</h2>
                <p>Chọn kết quả cho cả 5 chỉ số và xác nhận nhóm máu. Kết luận được tính tự động.</p>
            </header>
            <div className="inv-card__body">
                <dl className="inv-parent">
                    <div>
                        <dt>Nhóm máu dự kiến</dt>
                        <dd>
                            <Tag color="red">{item.expectedBloodGroup}</Tag>
                        </dd>
                    </div>
                    <div><dt>Chế phẩm</dt><dd>{item.componentTypeName}</dd></div>
                    <div><dt>Thể tích</dt><dd>{item.volumeMl} ml</dd></div>
                    <div><dt>Nguồn</dt><dd>{item.donorName || '—'}</dd></div>
                </dl>

                <div className="lab-section">
                    <div className="lab-section__head">
                        <h3>Sàng lọc bệnh truyền nhiễm</h3>
                        <Button
                            size="small"
                            disabled={!canEdit}
                            onClick={() =>
                                setDraft({
                                    hivResult: 'NEGATIVE',
                                    hbvResult: 'NEGATIVE',
                                    hcvResult: 'NEGATIVE',
                                    syphilisResult: 'NEGATIVE',
                                    irregularAntibody: 'NEGATIVE',
                                })
                            }
                        >
                            Đặt cả 5 chỉ số là âm tính
                        </Button>
                    </div>
                    {INDICATORS.map((ind) => (
                        <div
                            key={ind.key}
                            className={`lab-row${draft[ind.key] === 'POSITIVE' ? ' is-pos' : ''}`}
                        >
                            <div>
                                <b>{ind.label}</b>
                                <div className="lab-row__hint">{ind.hint}</div>
                            </div>
                            <Segmented
                                options={RESULT_OPTIONS}
                                value={draft[ind.key] ?? undefined}
                                disabled={!canEdit}
                                onChange={(v) => setDraft((p) => ({ ...p, [ind.key]: v as TestResultValue }))}
                            />
                        </div>
                    ))}
                </div>

                <div className="lab-section">
                    <div className="lab-section__head">
                        <h3>Nhóm máu khẳng định</h3>
                    </div>
                    <div className="lab-group">
                        <Select
                            placeholder="Nhóm ABO"
                            options={ABO_OPTIONS}
                            value={abo ?? undefined}
                            onChange={setAbo}
                            disabled={!canEdit}
                        />
                        <Select
                            placeholder="Yếu tố Rh"
                            options={RH_OPTIONS}
                            value={rh ?? undefined}
                            onChange={setRh}
                            disabled={!canEdit}
                        />
                    </div>
                    {groupMismatch && (
                        <Alert
                            style={{ marginTop: 12 }}
                            type="warning"
                            showIcon
                            icon={<WarningOutlined />}
                            title={`Nhóm máu khẳng định (${confirmedGroup}) khác nhóm dự kiến (${item.expectedBloodGroup}). Khi đạt chuẩn, hệ thống sẽ ghi đè nhóm máu của túi và hồ sơ người hiến.`}
                        />
                    )}
                </div>

                <div className={`lab-verdict${verdict ? ` is-${verdict.toLowerCase()}` : ''}`} role="status">
                    {verdict === null && 'Chưa đủ dữ liệu: chọn kết quả cho cả 5 chỉ số.'}
                    {verdict === 'PASSED' && 'Kết luận: ĐẠT CHUẨN. Túi sẽ chuyển sang TESTED_PASSED.'}
                    {verdict === 'FAILED' &&
                        `Kết luận: KHÔNG ĐẠT (dương tính ${positives.map((p) => p.label).join(', ')}). Túi sẽ bị khóa cách ly (LOCKED).`}
                </div>

                {!canEdit && (
                    <Alert style={{ marginTop: 12 }} type="warning" showIcon title="Bạn chỉ có quyền xem hàng chờ xét nghiệm." />
                )}

                <div className="inv-actions" style={{ marginTop: 16 }}>
                    <Popconfirm
                        title={verdict === 'FAILED' ? 'Ghi nhận túi KHÔNG ĐẠT?' : 'Xác nhận túi đạt chuẩn?'}
                        description={
                            verdict === 'FAILED'
                                ? 'Túi máu sẽ bị khóa cách ly. Không thể hoàn tác.'
                                : 'Nhóm máu khẳng định sẽ được ghi vào túi máu. Không thể hoàn tác.'
                        }
                        okText="Xác nhận"
                        cancelText="Hủy"
                        okButtonProps={{ danger: verdict === 'FAILED' }}
                        disabled={!canSubmit}
                        onConfirm={submit}
                    >
                        <Button
                            type="primary"
                            danger={verdict === 'FAILED'}
                            size="large"
                            disabled={!canSubmit}
                            loading={record.isPending}
                        >
                            {verdict === 'FAILED' ? 'Ghi nhận không đạt' : 'Xác nhận đạt chuẩn'}
                        </Button>
                    </Popconfirm>
                </div>
            </div>
        </section>
    )
}