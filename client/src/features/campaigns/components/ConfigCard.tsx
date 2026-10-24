import type { ReactNode } from 'react'
import SlotsTable from './SlotsTable'
import type { Slot } from '../types'

interface Props {
    label: string
    title: string
    subtitle?: ReactNode
    /** Bỏ trống nếu chưa có cấu hình để hiển thị (vd. điểm hiến mới). */
    slots?: Slot[]
    highlight?: boolean
}

/** Thẻ cấu hình dùng chung cho popup duyệt đề xuất và popup chi tiết điểm hiến. */
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
