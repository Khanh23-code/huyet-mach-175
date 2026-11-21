import './UrgencyTag.css'

export type ClinicalUrgencyLevel = 'ROUTINE' | 'URGENT' | 'EMERGENCY'

const DEFAULT_LABELS: Record<ClinicalUrgencyLevel, string> = {
    ROUTINE: 'Kế hoạch',
    URGENT: 'Khẩn',
    EMERGENCY: 'Cấp cứu',
}

export interface UrgencyTagProps {
    level?: ClinicalUrgencyLevel | string | null
    label?: string
    showDot?: boolean
    className?: string
}

export default function UrgencyTag({
    level,
    label,
    showDot = true,
    className,
}: UrgencyTagProps) {
    const normalized: ClinicalUrgencyLevel =
        level === 'EMERGENCY' || level === 'URGENT' || level === 'ROUTINE' ? level : 'ROUTINE'

    const text = label ?? DEFAULT_LABELS[normalized]
    const classes = ['ui-urgency-tag', `ui-urgency-tag--${normalized}`, className ?? '']
        .filter(Boolean)
        .join(' ')

    return (
        <span className={classes}>
            {showDot && <span className="ui-urgency-tag__dot" aria-hidden="true" />}
            <span>{text}</span>
        </span>
    )
}
