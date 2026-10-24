import type { Slot } from '../types'

export default function SlotsTable({ slots, empty = 'Chưa có khung giờ' }: { slots: Slot[]; empty?: string }) {
    if (slots.length === 0) return <div className="ds-muted">{empty}</div>
    return (
        <ul className="ds-slotlist">
            {slots.map((s) => (
                <li key={`${s.startTime}-${s.endTime}`}>
                    <span>{s.startTime} – {s.endTime}</span>
                    <b>{s.capacity} người / slot</b>
                </li>
            ))}
        </ul>
    )
}
