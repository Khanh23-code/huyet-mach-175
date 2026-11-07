import { fmtDateTime } from '@/features/screening/format'
import BarcodeSvg from '@/features/sessions/components/BarcodeSvg'

export interface LabelData {
    barcode: string
    bloodGroup: string
    volumeMl: number
    collectedAt: string
    expiredAt?: string
    componentName?: string
}

export default function BagLabel({ bag }: { bag: LabelData }) {
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
                {bag.componentName && (
                    <>
                        <dt>Chế phẩm</dt>
                        <dd><b>{bag.componentName}</b></dd>
                    </>
                )}
                <dt>Thể tích</dt>
                <dd><b>{bag.volumeMl} ml</b></dd>
                <dt>Thu thập</dt>
                <dd className="ss-mono">{fmtDateTime(bag.collectedAt)}</dd>
                {bag.expiredAt && (
                    <>
                        <dt>Hạn dùng</dt>
                        <dd className="ss-mono">{fmtDateTime(bag.expiredAt)}</dd>
                    </>
                )}
            </dl>
        </div>
    )
}