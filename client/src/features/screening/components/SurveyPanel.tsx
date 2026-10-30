import { Switch, Tag } from 'antd'
import { ExclamationCircleOutlined, FileTextOutlined } from '@ant-design/icons'
import { QUESTIONS, fmtDateTime, questionMeta, type Answers } from '../format'
import type { SurveyOnlineDetail } from '../types'

interface Props {
    survey: SurveyOnlineDetail
    /** Hiển thị đáp án (đọc từ BE hoặc đang nhập tại chỗ); null = chưa có */
    answers: Answers | null
    /** Khách vãng lai chưa có bảng hỏi: bác sĩ nhập tại chỗ */
    editable: boolean
    onChange: (key: string, value: boolean) => void
}

export default function SurveyPanel({ survey, answers, editable, onChange }: Props) {
    const keys = answers
        ? [...QUESTIONS.map((q) => q.key), ...Object.keys(answers).filter((k) => !QUESTIONS.some((q) => q.key === k))]
        : []

    const liveRisk = answers ? keys.filter((k) => questionMeta(k).risk && answers[k]).length : 0
    const risk = editable ? liveRisk : survey.riskScore

    let badge = <Tag>Chưa khai bảng hỏi</Tag>
    if (editable) badge = <Tag color="blue">Nhập tại chỗ</Tag>
    else if (survey.hasSubmittedSurvey)
        badge = risk > 0 || survey.hasRisk ? <Tag color="orange">Có {risk} cảnh báo</Tag> : <Tag color="green">Không có cảnh báo</Tag>

    return (
        <div className="sc-card sc-panel">
            <div className="sc-panel-head">
                <span className="sc-icon"><FileTextOutlined /></span>
                <div className="sc-panel-title">
                    <b>Tóm tắt bảng hỏi online</b>
                    <small>
                        {editable
                            ? 'Người hiến chưa khai trước, hãy hỏi trực tiếp và nhập kết quả'
                            : survey.hasSubmittedSurvey
                                ? `Hoàn thành lúc ${fmtDateTime(survey.surveySubmittedAt)}`
                                : 'Không có dữ liệu bảng hỏi'}
                    </small>
                </div>
                {badge}
            </div>

            {answers ? (
                <div className="sc-qa">
                    {keys.map((k) => {
                        const meta = questionMeta(k)
                        const yes = !!answers[k]
                        return (
                            <div className="sc-qa-row" key={k}>
                                <span>{meta.label}</span>
                                {editable ? (
                                    <Switch
                                        checked={yes}
                                        checkedChildren="Có"
                                        unCheckedChildren="Không"
                                        onChange={(val) => onChange(k, val)}
                                    />
                                ) : (
                                    <b className={yes && meta.risk ? 'sc-qa-risk' : 'sc-qa-ok'}>{yes ? 'Có' : 'Không'}</b>
                                )}
                            </div>
                        )
                    })}
                </div>
            ) : (
                <div className="sc-empty">Chưa có đáp án bảng hỏi</div>
            )}

            <div className="sc-note">
                <ExclamationCircleOutlined />
                <span>Kết quả bảng hỏi online chỉ để tham khảo, quyết định cuối cùng thuộc về nhân viên y tế.</span>
            </div>
        </div>
    )
}