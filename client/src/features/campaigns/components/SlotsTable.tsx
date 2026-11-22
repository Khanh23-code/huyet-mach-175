import type { CampaignSlot } from '../types'

export default function SlotsTable({ slots, empty = 'Chưa có khung giờ' }: { slots: CampaignSlot[]; empty?: string }) {
    if (!slots || slots.length === 0) return <div className="ds-muted">{empty}</div>
    return (
        <ul className="ds-slotlist">
            {slots.map((s, idx) => (
                <li key={s.slotId ?? `${s.startTime}-${s.endTime}-${idx}`}>
                    <span>{s.startTime.slice(0, 5)} – {s.endTime.slice(0, 5)}</span>
                    <b>{s.capacity} người / slot</b>
                </li>
            ))}
        </ul>
    )
}
