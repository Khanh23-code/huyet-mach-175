import { Tag } from 'antd'
import { fmtDateTime } from '@/features/screening/format'
import type { BagSource } from '../bagSource'
import { BagSourceTag, ExpiryTag } from './BagSourceBadges'

interface Props {
    barcode: string
    bloodGroup: string
    volumeMl: number
    componentTypeCode?: string
    collectedAt: string
    expiredAt: string
    isExpired: boolean
    source: BagSource
    active: boolean
    onClick: () => void
}

export default function BagQueueItem(p: Props) {
    const { source } = p
    const who = source.donor
        ? `${source.donor.fullName} · ${source.donor.citizenId}`
        : source.sourceType === 'EXTERNAL'
            ? `${source.externalUnitName ?? '—'}${source.externalCode ? ` · Mã gốc ${source.externalCode}` : ''}`
            : source.sourceName

    return (
        <button
            type="button"
            className={`inv-queue__item${p.active ? ' is-active' : ''}${p.isExpired ? ' is-expired' : ''}`}
            onClick={p.onClick}
        >
            <div className="inv-queue__top">
                <span className="inv-mono">{p.barcode}</span>
                <Tag color="red" style={{ marginInlineEnd: 0 }}>{p.bloodGroup}</Tag>
            </div>
            <div className="inv-queue__tags">
                <BagSourceTag type={source.sourceType} />
                <ExpiryTag expiredAt={p.expiredAt} isExpired={p.isExpired} />
            </div>
            <div className="inv-queue__sub">{who}</div>
            {source.sourceType === 'CAMPAIGN' && (
                <div className="inv-queue__sub">{source.campaignName}</div>
            )}
            <div className="inv-queue__sub">
                {p.componentTypeCode ? `${p.componentTypeCode} · ` : ''}{p.volumeMl} ml · Thu thập {fmtDateTime(p.collectedAt)}
            </div>
        </button>
    )
}