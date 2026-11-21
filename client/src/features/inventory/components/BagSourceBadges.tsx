import { ClockCircleOutlined } from '@ant-design/icons'
import { StatusPill } from '@/shared/components'
import { SOURCE_META, expiryInfo } from '../bagSource'
import type { BagSourceType } from '../bagSource'

export const BagSourceTag = ({ type }: { type: BagSourceType }) => {
    const m = SOURCE_META[type] ?? SOURCE_META.UNKNOWN
    return <StatusPill tone={m.tone}>{m.label}</StatusPill>
}

export const ExpiryTag = ({ expiredAt, isExpired }: { expiredAt: string; isExpired?: boolean }) => {
    const e = expiryInfo(expiredAt, isExpired)
    if (e.level === 'ok') return null
    return (
        <StatusPill tone={e.tone} dot={false}>
            <ClockCircleOutlined />
            <span>{e.label}</span>
        </StatusPill>
    )
}