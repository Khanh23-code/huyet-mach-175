import type { KeyboardEvent } from 'react'
import SlotsTable from './SlotsTable'
import { SITE_TYPE_LABEL } from '../format'
import type { Site } from '../types'

interface Props { site: Site; onOpen: (site: Site) => void }

export default function SiteCard({ site, onOpen }: Props) {
    const onKey = (e: KeyboardEvent) => {
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault()
            onOpen(site)
        }
    }

    return (
        <div className="ds-site" role="button" tabIndex={0} onClick={() => onOpen(site)} onKeyDown={onKey}>
            <div className="ds-site__top">
                <span className={`ds-tag ds-tag--${site.siteType}`}>{SITE_TYPE_LABEL[site.siteType]}</span>
                <span className="ds-status ds-status--on"><i />Đang hoạt động</span>
            </div>
            <h4>{site.siteName}</h4>
            <p className="ds-site__addr">{site.address}</p>
            <div className="ds-site__meta">{site.siteCode} · {site.phoneNumber}</div>
            <SlotsTable slots={site.slots} />
        </div>
    )
}
