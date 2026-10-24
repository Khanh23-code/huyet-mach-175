import { useState } from 'react'
import { Breadcrumb } from 'antd'
import ProposalForm from '../components/ProposalForm'
import MyProposals from '../components/MyProposals'
import { useMyProposals } from '../hooks'
import type { ProposalType } from '../types'
import './CampaignPage.css'

const TABS: { key: ProposalType; label: string }[] = [
    { key: 'NEW_SITE', label: 'Điểm hiến mới' },
    { key: 'UPDATE_SCHEDULE', label: 'Đổi lịch & sức chứa' },
]

export default function ProposalPage() {
    const [mode, setMode] = useState<ProposalType>('NEW_SITE')
    const pending = useMyProposals({ status: 'PENDING', pageNumber: 1, pageSize: 1 })
    const pendingTotal = pending.data?.total ?? 0

    return (
        <div className="ds">
            <Breadcrumb items={[{ title: 'Khoa Truyền máu' }, { title: 'Đề xuất lịch & điểm hiến' }]} />

            <div className="ds-title">
                <div>
                    <h1>Đề xuất lịch &amp; điểm hiến</h1>
                    <p>Tạo đề xuất để Quản trị hệ thống duyệt trước khi cấu hình được kích hoạt trên lịch đặt hẹn.</p>
                </div>
                {pendingTotal > 0 && <span className="ds-badge"><i />{pendingTotal} đang chờ duyệt</span>}
            </div>

            <section className="ds-card">
                <div className="ds-card__head ds-card__head--row">
                    <div>
                        <h3>Tạo đề xuất cấu hình</h3>
                        <p>Mọi thay đổi chỉ có hiệu lực sau khi SYS phê duyệt.</p>
                    </div>
                    <div className="ds-tabs" role="tablist">
                        {TABS.map((t) => (
                            <button
                                key={t.key}
                                type="button"
                                role="tab"
                                aria-selected={mode === t.key}
                                className={mode === t.key ? 'is-active' : ''}
                                onClick={() => setMode(t.key)}
                            >
                                {t.label}
                            </button>
                        ))}
                    </div>
                </div>
                <div className="ds-card__body">
                    <ProposalForm key={mode} mode={mode} />
                </div>
            </section>

            <MyProposals />
        </div>
    )
}
