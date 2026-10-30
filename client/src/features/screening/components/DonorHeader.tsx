import { addDaysIso, fmtBlood } from '../format'
import { fmtDate, initials } from '@/features/donors/format'
import { Tag } from 'antd'
import type { SurveyDetail } from '../types'

export default function DonorHeader({ detail }: { detail: SurveyDetail }) {
    const d = detail.donor
    const nextEligible = d.lastDonationDate ? addDaysIso(d.lastDonationDate, 84) : null

    return (
        <div className="sc-card sc-donor">
            <div className="sc-avatar sc-avatar--lg">{initials(d.fullName)}</div>

            <div className="sc-donor-main">
                <div className="sc-eyebrow">HỒ SƠ NGƯỜI HIẾN</div>
                <h2 className="sc-donor-name">{d.fullName}</h2>
                <div className="sc-muted">
                    {d.donorCode} · CCCD {d.citizenId} · {d.gender} · {d.age} tuổi ·{' '}
                    <b className="sc-blood">{fmtBlood(d.bloodType, d.rhFactor)}</b>
                </div>
                <div className="sc-muted">
                    SĐT {d.phoneNumber} · {detail.campaignName ?? 'Hiến tại viện'} ·{' '}
                    {detail.isWalkIn ? <Tag>Vãng lai</Tag> : detail.timeSlot || '—'}
                </div>
            </div>

            <div className="sc-donor-stats">
                <div>
                    <small>Tổng số lần hiến</small>
                    <b>{d.totalDonations} lần</b>
                </div>
                <div>
                    <small>Lần hiến gần nhất</small>
                    <b>{d.lastDonationDate ? fmtDate(d.lastDonationDate) : 'Chưa hiến lần nào'}</b>
                    {d.isEligible84Days ? (
                        <span className="sc-ok">✓ Đủ 84 ngày</span>
                    ) : (
                        <span className="sc-warn">
                            Chưa đủ 84 ngày{nextEligible ? ` · từ ${fmtDate(nextEligible)}` : ''}
                        </span>
                    )}
                </div>
            </div>
        </div>
    )
}