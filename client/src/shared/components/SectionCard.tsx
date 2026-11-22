import type { ReactNode } from 'react'
import './SectionCard.css'

export interface SectionCardProps {
    eyebrow?: ReactNode
    title?: ReactNode
    subtitle?: ReactNode
    extra?: ReactNode
    noPadding?: boolean
    variant?: 'default' | 'muted'
    children: ReactNode
    className?: string
    bodyClassName?: string
}

export default function SectionCard({
    eyebrow,
    title,
    subtitle,
    extra,
    noPadding = false,
    variant = 'default',
    children,
    className,
    bodyClassName,
}: SectionCardProps) {
    const hasHeader = Boolean(eyebrow || title || subtitle || extra)

    const rootClasses = [
        'ui-section-card',
        variant === 'muted' ? 'ui-section-card--muted' : '',
        className ?? '',
    ]
        .filter(Boolean)
        .join(' ')

    const bodyClasses = [
        'ui-section-card__body',
        noPadding ? 'ui-section-card__body--flush' : '',
        bodyClassName ?? '',
    ]
        .filter(Boolean)
        .join(' ')

    return (
        <section className={rootClasses}>
            {hasHeader && (
                <div className="ui-section-card__head">
                    <div className="ui-section-card__titles">
                        {eyebrow && <div className="ui-section-card__eyebrow">{eyebrow}</div>}
                        {title && <h2 className="ui-section-card__title">{title}</h2>}
                        {subtitle && <p className="ui-section-card__subtitle">{subtitle}</p>}
                    </div>
                    {extra && <div className="ui-section-card__extra">{extra}</div>}
                </div>
            )}

            <div className={bodyClasses}>{children}</div>
        </section>
    )
}
