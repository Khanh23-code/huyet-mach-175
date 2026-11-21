import type { ReactNode } from 'react'
import './StatusPill.css'

export type StatusPillTone =
    | 'primary'
    | 'success'
    | 'warning'
    | 'urgent'
    | 'danger'
    | 'info'
    | 'blood'
    | 'purple'
    | 'neutral'

export interface StatusPillProps {
    tone?: StatusPillTone
    dot?: boolean
    pulse?: boolean
    title?: string
    className?: string
    children: ReactNode
}

export default function StatusPill({
    tone = 'neutral',
    dot = true,
    pulse = false,
    title,
    className,
    children,
}: StatusPillProps) {
    const classes = [
        'ui-status-pill',
        `ui-status-pill--${tone}`,
        pulse ? 'ui-status-pill--pulse' : '',
        className ?? '',
    ]
        .filter(Boolean)
        .join(' ')

    return (
        <span className={classes} title={title}>
            {dot && <span className="ui-status-pill__dot" aria-hidden="true" />}
            <span>{children}</span>
        </span>
    )
}
