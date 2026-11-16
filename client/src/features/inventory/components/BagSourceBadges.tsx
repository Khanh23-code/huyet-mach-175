import { Tag } from 'antd'
import { ClockCircleOutlined } from '@ant-design/icons'
import { SOURCE_META, expiryInfo } from '../bagSource'
import type { BagSourceType } from '../bagSource'

export const BagSourceTag = ({ type }: { type: BagSourceType }) => {
    const m = SOURCE_META[type] ?? SOURCE_META.UNKNOWN
    return <Tag color={m.color} style={{ marginInlineEnd: 0 }}>{m.label}</Tag>
}

export const ExpiryTag = ({ expiredAt, isExpired }: { expiredAt: string; isExpired?: boolean }) => {
    const e = expiryInfo(expiredAt, isExpired)
    if (e.level === 'ok') return null
    return (
        <Tag color={e.color} icon={<ClockCircleOutlined />} style={{ marginInlineEnd: 0 }}>
            {e.label}
        </Tag>
    )
}