import { fmtDateTime } from '@/features/screening/format'
import BarcodeSvg from './BarcodeSvg'
import type { SessionBloodBag } from '../types'

export default function BloodBagLabel({ bag }: { bag: SessionBloodBag }) {
    return (
        <div className="ss-label">
            <div className="ss-label-head">
                <span>BỆNH VIỆN 175 · NGÂN HÀNG MÁU</span>
                <b className="ss-label-group">{bag.bloodGroup}</b>
            </div>
            <BarcodeSvg value={bag.barcode} />
            <dl className="ss-label-rows">
                <dt>Mã ID</dt>
                <dd className="ss-mono">{bag.barcode}</dd>
                <dt>Thể tích</dt>
                <dd><b>{bag.volumeMl} ml</b></dd>
                <dt>Thu thập</dt>
                <dd className="ss-mono">{fmtDateTime(bag.collectedAt)}</dd>
            </dl>
        </div>
    )
}