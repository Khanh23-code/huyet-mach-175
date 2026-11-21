import type { ReactNode } from 'react'
import './PageContainer.css'

export interface PageContainerProps {
    children: ReactNode
    maxWidth?: 'full' | 'xl' | 'lg' | 'md'
    className?: string
}

export default function PageContainer({
    children,
    maxWidth = 'full',
    className,
}: PageContainerProps) {
    const classes = [
        'ui-page',
        maxWidth !== 'full' ? `ui-page--${maxWidth}` : '',
        className ?? '',
    ]
        .filter(Boolean)
        .join(' ')

    return <div className={classes}>{children}</div>
}
