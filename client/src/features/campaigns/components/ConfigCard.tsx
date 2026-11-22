import type { ReactNode } from 'react'
import SlotsTable from './SlotsTable'
import type { CampaignSlot } from '../types'

interface Props {
    label: string
    title: string
    subtitle?: ReactNode
    /** Bỏ trống nếu chưa có cấu hình để hiển thị. */
    slots?: CampaignSlot[]
    highlight?: boolean
}

/** Thẻ cấu hình dùng chung cho modal duyệt và chi tiết chiến dịch. */
export default function ConfigCard({ label, title, subtitle, slots, highlight }: Props) {
    return (
        <div className={`ds-config${highlight ? ' ds-config--proposed' : ''}`}>
            <div className="ds-config__label">{label}</div>
            <div className="ds-config__title">{title}</div>
            {subtitle && <div className="ds-config__sub">{subtitle}</div>}
            {slots && <SlotsTable slots={slots} />}
        </div>
    )
}
