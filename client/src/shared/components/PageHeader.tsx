import type { ReactNode } from 'react'
import { Breadcrumb, Button } from 'antd'
import { ArrowLeftOutlined } from '@ant-design/icons'
import { Link, useNavigate } from 'react-router-dom'
import './PageHeader.css'

export interface PageBreadcrumbItem {
    title: ReactNode
    path?: string
}

export interface PageHeaderProps {
    breadcrumb?: PageBreadcrumbItem[]
    title: ReactNode
    subtitle?: ReactNode
    titleExtra?: ReactNode
    backTo?: string | number
    onBack?: () => void
    backLabel?: string
    extra?: ReactNode
    bottom?: ReactNode
    hideInPrint?: boolean
    className?: string
}

export default function PageHeader({
    breadcrumb,
    title,
    subtitle,
    titleExtra,
    backTo,
    onBack,
    backLabel = 'Quay lại',
    extra,
    bottom,
    hideInPrint = false,
    className,
}: PageHeaderProps) {
    const navigate = useNavigate()

    const handleBack = () => {
        if (onBack) {
            onBack()
            return
        }
        if (typeof backTo === 'number') {
            navigate(backTo)
        } else if (typeof backTo === 'string') {
            navigate(backTo)
        }
    }

    const showBack = Boolean(onBack || backTo !== undefined)

    const rootClasses = [
        'ui-page-header',
        hideInPrint ? 'ui-page-header--hide-print' : '',
        className ?? '',
    ]
        .filter(Boolean)
        .join(' ')

    return (
        <header className={rootClasses}>
            {breadcrumb && breadcrumb.length > 0 && (
                <Breadcrumb
                    className="ui-page-header__breadcrumb"
                    items={breadcrumb.map((item) => ({
                        title: item.path ? <Link to={item.path}>{item.title}</Link> : item.title,
                    }))}
                />
            )}

            <div className="ui-page-header__row">
                <div className="ui-page-header__left">
                    {showBack && (
                        <Button
                            type="default"
                            icon={<ArrowLeftOutlined />}
                            onClick={handleBack}
                            aria-label={backLabel}
                            className="ui-page-header__back"
                        />
                    )}

                    <div className="ui-page-header__heading">
                        <div className="ui-page-header__title-line">
                            <h1 className="ui-page-header__title">{title}</h1>
                            {titleExtra}
                        </div>
                        {subtitle && <p className="ui-page-header__subtitle">{subtitle}</p>}
                    </div>
                </div>

                {extra && <div className="ui-page-header__extra">{extra}</div>}
            </div>

            {bottom && <div className="ui-page-header__bottom">{bottom}</div>}
        </header>
    )
}
