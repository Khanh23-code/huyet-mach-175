import { useState } from 'react'
import { Alert } from 'antd'
import DonorHeader from './DonorHeader'
import SurveyPanel from './SurveyPanel'
import ClinicalForm from './ClinicalForm'
import { answersFrom, type Answers } from '../format'
import type { SurveyDetail } from '../types'

interface Props {
    detail: SurveyDetail
    /** Có quyền SCREENING_EDIT */
    canEdit: boolean
    onSaved: () => void
}

/** Render lại bằng key={appointmentId} ở nơi dùng để reset form khi đổi người hiến. */
export default function ScreeningWorkspace({ detail, canEdit, onSaved }: Props) {
    const done = detail.clinical.screeningStatus !== 'PENDING'
    const editable = canEdit && !done
    const hasServerAnswers = !!detail.survey.surveyAnswers
    // Khách vãng lai chưa có bảng hỏi: BE bắt buộc gửi kèm surveyAnswers
    const enterAnswers = editable && !hasServerAnswers

    const [answers, setAnswers] = useState<Answers>(() => answersFrom(detail.survey.surveyAnswers))

    const shown: Answers | null = enterAnswers ? answers : hasServerAnswers ? answersFrom(detail.survey.surveyAnswers) : null

    return (
        <>
            <DonorHeader detail={detail} />

            {editable && !detail.donor.isEligible84Days && (
                <Alert
                    type="warning"
                    showIcon
                    style={{ marginTop: 16 }}
                    title="Người hiến chưa đủ 84 ngày kể từ lần hiến gần nhất"
                    description="Cân nhắc kết luận Tạm hoãn."
                />
            )}

            <div className="sc-grid">
                <SurveyPanel
                    survey={detail.survey}
                    answers={shown}
                    editable={enterAnswers}
                    onChange={(k, val) => setAnswers((p) => ({ ...p, [k]: val }))}
                />
                <ClinicalForm
                    detail={detail}
                    editable={editable}
                    sendAnswers={enterAnswers}
                    answers={answers}
                    onSaved={onSaved}
                />
            </div>
        </>
    )
}