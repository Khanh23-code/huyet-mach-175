import { Tag } from 'antd'
import { statusMeta } from '../stockFormat'

export const BloodGroupTag = ({ value }: { value: string }) => (
    <Tag color="red" style={{ marginInlineEnd: 0, fontWeight: 600 }}>{value}</Tag>
)

export const BagStatusTag = ({ status, label }: { status: string; label?: string }) => {
    const m = statusMeta(status)
    return <Tag color={m.color} style={{ marginInlineEnd: 0 }}>{label || m.label}</Tag>
}