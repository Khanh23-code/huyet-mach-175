import { addDaysIso, fmtBlood } from '../format'
import { BloodGroupTag, SectionCard, StatusPill } from '@/shared/components'
import { initials } from '@/shared/utils/string'
import { fmtDate } from '@/shared/formatters/date'
import type { SurveyDetail } from '../types'

export default function DonorHeader({ detail }: { detail: SurveyDetail }) {
    const d = detail.donor
    const nextEligible = d.lastDonationDate ? addDaysIso(d.lastDonationDate, 84) : null

    return (
        <SectionCard>
            <div className="sc-donor">
                <div className="sc-avatar sc-avatar--lg">{initials(d.fullName)}</div>

                <div className="sc-donor-main">
                    <div className="sc-eyebrow">HỒ SƠ NGƯỜI HIẾN</div>
                    <h2 className="sc-donor-name">{d.fullName}</h2>
                    <div className="sc-donor-meta">
                        <span>{d.donorCode} - CCCD {d.citizenId} - {d.gender} - {d.age} tuổi</span>
                        <BloodGroupTag value={fmtBlood(d.bloodType, d.rhFactor)} size="sm" />
                    </div>
                    <div className="sc-donor-meta">
                        <span>
                            SĐT {d.phoneNumber} - {detail.campaignName ?? 'Hiến tại viện'}
                        </span>
                        {detail.isWalkIn ? (
                            <StatusPill tone="neutral" dot={false}>Vãng lai</StatusPill>
                        ) : (
                            <span>- {detail.timeSlot || '—'}</span>
                        )}
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
                                Chưa đủ 84 ngày{nextEligible ? ` - đến ${fmtDate(nextEligible)}` : ''}
                            </span>
                        )}
                    </div>
                </div>
            </div>
        </SectionCard>
    )
}