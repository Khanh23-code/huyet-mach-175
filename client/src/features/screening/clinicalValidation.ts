import type { Answers } from './format'
import type { Conclusion, SurveyClinicalDetail, TargetVolume, UpdateClinicalRequest } from './types'

/** Giá trị form để dạng chuỗi, chuyển sang số khi gửi. */
export interface ClinicalFormValues {
    weightKg: string
    systolic: string
    diastolic: string
    pulse: string
    hemoglobin: string
    bodyTemp: string
    hbsag: '' | 'NEG' | 'POS'
    targetVolume: '' | TargetVolume
    notes: string
    deferralReason: string
}

export type FormErrors = Partial<Record<keyof ClinicalFormValues | '_form', string>>

type NumField = 'weightKg' | 'systolic' | 'diastolic' | 'pulse' | 'hemoglobin' | 'bodyTemp'

// Khoảng hợp lệ lấy từ UpdateClinicalScreeningRequest ở BE
const NUMERIC: { key: NumField; label: string; min: number; max: number; unit: string }[] = [
    { key: 'weightKg', label: 'Cân nặng', min: 30, max: 250, unit: 'kg' },
    { key: 'systolic', label: 'Huyết áp tâm thu', min: 50, max: 250, unit: 'mmHg' },
    { key: 'diastolic', label: 'Huyết áp tâm trương', min: 30, max: 150, unit: 'mmHg' },
    { key: 'pulse', label: 'Mạch', min: 30, max: 200, unit: 'lần/phút' },
    { key: 'hemoglobin', label: 'Hemoglobin', min: 50, max: 250, unit: 'g/L' },
    { key: 'bodyTemp', label: 'Thân nhiệt', min: 34, max: 42, unit: '°C' },
]

export const emptyValues = (): ClinicalFormValues => ({
    weightKg: '',
    systolic: '',
    diastolic: '',
    pulse: '',
    hemoglobin: '',
    bodyTemp: '',
    hbsag: '',
    targetVolume: '',
    notes: '',
    deferralReason: '',
})

export const valuesFromClinical = (c: SurveyClinicalDetail): ClinicalFormValues => ({
    weightKg: c.weightKg != null ? String(c.weightKg) : '',
    systolic: c.systolicBloodPressure != null ? String(c.systolicBloodPressure) : '',
    diastolic: c.diastolicBloodPressure != null ? String(c.diastolicBloodPressure) : '',
    pulse: c.pulse != null ? String(c.pulse) : '',
    hemoglobin: c.hemoglobinLevel != null ? String(c.hemoglobinLevel) : '',
    bodyTemp: c.bodyTemp != null ? String(c.bodyTemp) : '',
    hbsag: c.rapidHbsAgNegative == null ? '' : c.rapidHbsAgNegative ? 'NEG' : 'POS',
    targetVolume: c.targetVolumeMl ?? '',
    notes: c.clinicalNotes ?? '',
    deferralReason: c.deferralReason ?? '',
})

const toNumber = (s: string) => Number(s.trim().replace(',', '.'))

export type ValidationResult =
    | { ok: true; payload: UpdateClinicalRequest }
    | { ok: false; errors: FormErrors }

export function validateClinical(
    v: ClinicalFormValues,
    status: Conclusion,
    ctx: { sendAnswers: boolean; answers: Answers },
): ValidationResult {
    const errors: FormErrors = {}
    const nums: Partial<Record<NumField, number>> = {}
    const qualified = status === 'QUALIFIED'

    for (const f of NUMERIC) {
        const raw = v[f.key].trim()
        if (raw === '') {
            if (qualified) errors[f.key] = `Vui lòng nhập ${f.label.toLowerCase()}`
            continue
        }
        const n = toNumber(raw)
        if (Number.isNaN(n)) errors[f.key] = `${f.label} không hợp lệ`
        else if (n < f.min || n > f.max) errors[f.key] = `${f.label} phải từ ${f.min} đến ${f.max} ${f.unit}`
        else nums[f.key] = n
    }

    if (qualified) {
        if (v.hbsag === '') errors.hbsag = 'Vui lòng chọn kết quả test nhanh HBsAg'
        else if (v.hbsag === 'POS') errors.hbsag = 'HBsAg dương tính, không thể kết luận Đạt tiêu chuẩn'
        if (v.targetVolume === '') errors.targetVolume = 'Vui lòng chọn thể tích máu chỉ định'
    } else if (v.deferralReason.trim() === '') {
        errors.deferralReason = 'Vui lòng nhập lý do tạm hoãn / không đạt'
    }

    if (v.notes.length > 500) errors.notes = 'Ghi chú tối đa 500 ký tự'
    if (v.deferralReason.length > 500) errors.deferralReason = 'Lý do tối đa 500 ký tự'

    if (Object.keys(errors).length > 0) return { ok: false, errors }

    const payload: UpdateClinicalRequest = {
        screeningStatus: status,
        weightKg: nums.weightKg,
        systolicBloodPressure: nums.systolic,
        diastolicBloodPressure: nums.diastolic,
        pulse: nums.pulse,
        hemoglobinLevel: nums.hemoglobin,
        bodyTemp: nums.bodyTemp,
        rapidHbsAgNegative: v.hbsag === '' ? undefined : v.hbsag === 'NEG',
        clinicalNotes: v.notes.trim() || undefined,
    }
    if (qualified) payload.targetVolumeMl = v.targetVolume as TargetVolume
    else payload.deferralReason = v.deferralReason.trim()
    if (ctx.sendAnswers) payload.surveyAnswers = ctx.answers

    return { ok: true, payload }
}