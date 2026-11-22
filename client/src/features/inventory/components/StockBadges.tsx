import { BloodGroupTag, StatusPill } from '@/shared/components'
import { statusMeta } from '../stockFormat'

export { BloodGroupTag }

export const BagStatusTag = ({ status, label }: { status: string; label?: string }) => {
    const m = statusMeta(status)
    return <StatusPill tone={m.tone}>{label || m.label}</StatusPill>
}