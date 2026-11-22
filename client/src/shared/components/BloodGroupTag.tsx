import './BloodGroupTag.css'

export interface BloodGroupTagProps {
    value?: string | null
    size?: 'sm' | 'md' | 'lg'
    className?: string
}

export default function BloodGroupTag({
    value,
    size = 'md',
    className,
}: BloodGroupTagProps) {
    const label = value?.trim() || '—'
    const classes = ['ui-blood-tag', `ui-blood-tag--${size}`, className ?? '']
        .filter(Boolean)
        .join(' ')

    return <span className={classes}>{label}</span>
}
