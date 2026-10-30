import { useState, type ReactNode } from 'react'
import { Alert, App, Button, Input, Radio, Select } from 'antd'
import { CheckOutlined, HeartOutlined } from '@ant-design/icons'
import { parseApiError } from '@/services/apiError'
import { useSaveClinical } from '../hooks'
import {
    validateClinical,
    valuesFromClinical,
    type ClinicalFormValues,
    type FormErrors,
} from '../clinicalValidation'
import { SCREENING_META, VOLUME_OPTIONS, fmtDateTime, type Answers } from '../format'
import type { Conclusion, SurveyDetail, UpdateClinicalRequest } from '../types'

interface Props {
    detail: SurveyDetail
    /** Có quyền sửa và hồ sơ chưa có kết luận */
    editable: boolean
    /** Gửi kèm đáp án bảng hỏi (khách vãng lai chưa có bảng hỏi) */
    sendAnswers: boolean
    answers: Answers
    onSaved: () => void
}

function Field({
                   label,
                   required,
                   error,
                   children,
               }: {
    label: string
    required?: boolean
    error?: string
    children: ReactNode
}) {
    return (
        <div className="sc-field">
            <label>
                {label}
                {required && <span className="sc-req"> *</span>}
            </label>
            {children}
            {error && <div className="sc-err">{error}</div>}
        </div>
    )
}

export default function ClinicalForm({ detail, editable, sendAnswers, answers, onSaved }: Props) {
    const { message, modal } = App.useApp()
    const save = useSaveClinical()
    const [v, setV] = useState<ClinicalFormValues>(() => valuesFromClinical(detail.clinical))
    const [errors, setErrors] = useState<FormErrors>({})
    const [pending, setPending] = useState<Conclusion | null>(null)

    const set = <K extends keyof ClinicalFormValues>(key: K, value: ClinicalFormValues[K]) => {
        setV((p) => ({ ...p, [key]: value }))
        setErrors((p) => ({ ...p, [key]: undefined }))
    }

    const num = (key: 'weightKg' | 'systolic' | 'diastolic' | 'pulse' | 'hemoglobin' | 'bodyTemp', unit: string) => (
        <Input
            size="large"
            inputMode="decimal"
            suffix={unit}
            value={v[key]}
            disabled={!editable}
            status={errors[key] ? 'error' : undefined}
            onChange={(e) => set(key, e.target.value.replace(/[^\d.,]/g, ''))}
        />
    )

    const doSave = async (status: Conclusion, body: UpdateClinicalRequest) => {
        setPending(status)
        try {
            const r = await save.mutateAsync({ appointmentId: detail.appointmentId, body })
            message.success(r.message)
            onSaved()
        } catch (e) {
            message.error(parseApiError(e).message)
        } finally {
            setPending(null)
        }
    }

    const submit = (status: Conclusion) => {
        const res = validateClinical(v, status, { sendAnswers, answers })
        if (!res.ok) {
            setErrors(res.errors)
            message.error('Vui lòng kiểm tra lại các trường được đánh dấu')
            return
        }
        setErrors({})
        modal.confirm({
            title: `Kết luận "${SCREENING_META[status].label}"?`,
            content: `Lưu kết quả sàng lọc cho ${detail.donor.fullName}. ${
                status === 'QUALIFIED' ? 'Người hiến sẽ được chuyển sang phòng lấy máu.' : 'Hồ sơ sẽ đóng, không tạo phiên lấy máu.'
            }`,
            okText: 'Xác nhận',
            cancelText: 'Kiểm tra lại',
            onOk: () => doSave(status, res.payload),
        })
    }

    const c = detail.clinical
    const meta = SCREENING_META[c.screeningStatus]

    return (
        <>
            <div className="sc-card sc-panel">
                <div className="sc-panel-head">
                    <span className="sc-icon"><HeartOutlined /></span>
                    <div className="sc-panel-title">
                        <b>Chỉ số sinh tồn</b>
                        <small>{editable ? 'Nhập kết quả đo trực tiếp' : 'Kết quả đã ghi nhận'}</small>
                    </div>
                    {editable && <span className="sc-req">* Bắt buộc khi kết luận Đạt tiêu chuẩn</span>}
                </div>

                <div className="sc-form-grid">
                    <Field label="Huyết áp tâm thu" required error={errors.systolic}>{num('systolic', 'mmHg')}</Field>
                    <Field label="Huyết áp tâm trương" required error={errors.diastolic}>{num('diastolic', 'mmHg')}</Field>
                    <Field label="Mạch" required error={errors.pulse}>{num('pulse', 'lần/phút')}</Field>
                    <Field label="Thân nhiệt" required error={errors.bodyTemp}>{num('bodyTemp', '°C')}</Field>
                    <Field label="Cân nặng" required error={errors.weightKg}>{num('weightKg', 'kg')}</Field>
                    <Field label="Hemoglobin" required error={errors.hemoglobin}>{num('hemoglobin', 'g/L')}</Field>

                    <Field label="Test nhanh HBsAg" required error={errors.hbsag}>
                        <Radio.Group
                            optionType="button"
                            buttonStyle="solid"
                            disabled={!editable}
                            value={v.hbsag || undefined}
                            onChange={(e) => set('hbsag', e.target.value)}
                            options={[
                                { label: 'Âm tính', value: 'NEG' },
                                { label: 'Dương tính', value: 'POS' },
                            ]}
                        />
                    </Field>
                    <Field label="Thể tích máu chỉ định" required error={errors.targetVolume}>
                        <Select
                            size="large"
                            placeholder="Chọn thể tích"
                            disabled={!editable}
                            options={VOLUME_OPTIONS}
                            value={v.targetVolume || undefined}
                            status={errors.targetVolume ? 'error' : undefined}
                            onChange={(val) => set('targetVolume', val)}
                        />
                    </Field>
                </div>

                <Field label="Ghi chú khám lâm sàng" error={errors.notes}>
                    <Input.TextArea
                        rows={3}
                        maxLength={500}
                        disabled={!editable}
                        placeholder="Nhập ghi chú nếu có..."
                        value={v.notes}
                        status={errors.notes ? 'error' : undefined}
                        onChange={(e) => set('notes', e.target.value)}
                    />
                </Field>

                <Field label="Lý do tạm hoãn / không đạt" error={errors.deferralReason}>
                    <Input.TextArea
                        rows={2}
                        maxLength={500}
                        disabled={!editable}
                        placeholder="Bắt buộc khi kết luận Tạm hoãn hoặc Không đạt"
                        value={v.deferralReason}
                        status={errors.deferralReason ? 'error' : undefined}
                        onChange={(e) => set('deferralReason', e.target.value)}
                    />
                </Field>
            </div>

            <div className="sc-card sc-conclusion">
                <div>
                    <b>Kết luận sàng lọc</b>
                    <div className="sc-muted">
                        {editable ? 'Chọn một quyết định để hoàn tất hồ sơ.' : 'Hồ sơ đã có kết luận.'}
                    </div>
                </div>

                {editable ? (
                    <div className="sc-actions">
                        <Button
                            type="primary"
                            size="large"
                            icon={<CheckOutlined />}
                            loading={pending === 'QUALIFIED'}
                            disabled={save.isPending}
                            onClick={() => submit('QUALIFIED')}
                        >
                            Đạt tiêu chuẩn
                        </Button>
                        <Button
                            type="primary"
                            size="large"
                            className="sc-btn-warning"
                            loading={pending === 'DEFERRED'}
                            disabled={save.isPending}
                            onClick={() => submit('DEFERRED')}
                        >
                            Tạm hoãn
                        </Button>
                        <Button
                            type="primary"
                            danger
                            size="large"
                            loading={pending === 'UNQUALIFIED'}
                            disabled={save.isPending}
                            onClick={() => submit('UNQUALIFIED')}
                        >
                            Không đạt
                        </Button>
                    </div>
                ) : (
                    c.screeningStatus !== 'PENDING' && (
                        <Alert
                            style={{ flex: 1 }}
                            type={c.screeningStatus === 'QUALIFIED' ? 'success' : c.screeningStatus === 'DEFERRED' ? 'warning' : 'error'}
                            showIcon
                            title={`${meta.label}${c.doctorName ? ` · ${c.doctorName}` : ''} · ${fmtDateTime(c.screenedAt)}`}
                            description={
                                c.screeningStatus === 'QUALIFIED'
                                    ? 'Đã chuyển sang phòng lấy máu.'
                                    : c.deferralReason ?? undefined
                            }
                        />
                    )
                )}
            </div>
        </>
    )
}