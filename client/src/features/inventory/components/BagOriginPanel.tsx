import type { ReactNode } from 'react'
import { Alert, Tag } from 'antd'
import { WarningOutlined } from '@ant-design/icons'
import { fmtDateTime } from '@/shared/formatters/date'
import { fmtDay } from '../stockFormat'
import type { BagLabSummary, BagSource } from '../bagSource'
import { BagSourceTag, ExpiryTag } from './BagSourceBadges'
import './BagOrigin.css'

interface Props {
    source: BagSource
    bagBloodGroup: string
    collectedAt: string
    expiredAt: string
    isExpired: boolean
    labTest?: BagLabSummary | null
}

const Item = ({ label, children }: { label: string; children: ReactNode }) => (
    <div><dt>{label}</dt><dd>{children}</dd></div>
)

const IND: { key: keyof BagLabSummary; label: string }[] = [
    { key: 'hivResult', label: 'HIV' },
    { key: 'hbvResult', label: 'HBV' },
    { key: 'hcvResult', label: 'HCV' },
    { key: 'syphilisResult', label: 'Giang mai' },
    { key: 'irregularAntibody', label: 'Kháng thể BT' },
]

export default function BagOriginPanel({ source, bagBloodGroup, collectedAt, expiredAt, isExpired, labTest }: Props) {
    const { donor, collection } = source
    const groupMismatch = !!donor?.declaredBloodGroup && donor.declaredBloodGroup !== bagBloodGroup

    return (
        <div className="origin">
            <div className="origin__head">
                <BagSourceTag type={source.sourceType} />
                <b>{source.sourceName}</b>
                <ExpiryTag expiredAt={expiredAt} isExpired={isExpired} />
            </div>

            {isExpired && (
                <Alert
                    type="error" showIcon
                    title="Túi máu đã hết hạn sử dụng, không thể thực hiện nghiệp vụ. Vui lòng lập biên bản tiêu hủy."
                />
            )}

            <section className="origin__sec">
                <h4>Nguồn gốc túi</h4>
                <dl className="origin__grid">
                    {source.sourceType === 'CAMPAIGN' && (
                        <>
                            <Item label="Chiến dịch">{source.campaignName}</Item>
                            <Item label="Địa điểm">{source.campaignLocation || '—'}</Item>
                        </>
                    )}
                    {source.sourceType === 'WALK_IN' && <Item label="Hình thức">Hiến trực tiếp tại viện</Item>}
                    {source.sourceType === 'EXTERNAL' && (
                        <>
                            <Item label="Đơn vị cung cấp">{source.externalUnitName}</Item>
                            <Item label="Mã túi của đơn vị">
                                <span className="inv-mono">{source.externalCode || '—'}</span>
                            </Item>
                        </>
                    )}
                    <Item label="Thu thập">{fmtDateTime(collectedAt)}</Item>
                    <Item label="Hết hạn">{fmtDateTime(expiredAt)}</Item>
                </dl>
            </section>

            {source.sourceType === 'EXTERNAL' && (
                <Alert
                    type="info" showIcon
                    title="Túi nhận từ đơn vị ngoài: không có hồ sơ người hiến trong hệ thống. Nhóm máu ghi nhận do đơn vị cung cấp khai báo, hãy đối chiếu kỹ."
                />
            )}

            {donor && (
                <section className="origin__sec">
                    <h4>Người hiến</h4>
                    <dl className="origin__grid">
                        <Item label="Họ tên">{donor.fullName}</Item>
                        <Item label="CCCD"><span className="inv-mono">{donor.citizenId}</span></Item>
                        <Item label="Giới tính">{donor.gender}</Item>
                        <Item label="Tuổi">{donor.age} ({fmtDay(donor.dateOfBirth)})</Item>
                        <Item label="Điện thoại">{donor.phoneNumber}</Item>
                        <Item label="Nhóm máu khai báo">
                            {donor.declaredBloodGroup ?? '—'}
                            {groupMismatch && (
                                <Tag color="warning" icon={<WarningOutlined />} style={{ marginInlineStart: 6 }}>
                                    Khác nhóm túi ({bagBloodGroup})
                                </Tag>
                            )}
                        </Item>
                        <Item label="Số lần hiến">{donor.totalDonations}</Item>
                        <Item label="Lần hiến gần nhất">{fmtDay(donor.lastDonationDate)}</Item>
                    </dl>
                </section>
            )}

            {collection && (
                <section className="origin__sec">
                    <h4>Phiên lấy máu</h4>
                    <dl className="origin__grid">
                        <Item label="Thể tích thực tế">
                            {collection.actualVolumeMl != null ? `${collection.actualVolumeMl} ml` : '—'}
                        </Item>
                        <Item label="Hoàn tất lúc">{collection.completedAt ? fmtDateTime(collection.completedAt) : '—'}</Item>
                    </dl>
                    {collection.collectionIncident && (
                        <Alert
                            style={{ marginTop: 8 }} type="warning" showIcon
                            title={`Sự cố khi lấy máu: ${collection.collectionIncident}`}
                        />
                    )}
                </section>
            )}

            {labTest && (
                <section className="origin__sec">
                    <h4>Kết quả xét nghiệm</h4>
                    <dl className="origin__grid">
                        <Item label="Kết luận">
                            <Tag color={labTest.overallConclusion === 'PASSED' ? 'green' : 'red'}>
                                {labTest.overallConclusion === 'PASSED' ? 'Đạt chuẩn' : 'Không đạt'}
                            </Tag>
                        </Item>
                        <Item label="Nhóm máu khẳng định">{labTest.confirmedBloodGroup}</Item>
                        <Item label="Kỹ thuật viên">{labTest.technicianName}</Item>
                        <Item label="Thời điểm">{fmtDateTime(labTest.testedAt)}</Item>
                    </dl>
                    <div className="origin__inds">
                        {IND.map((i) => {
                            const pos = labTest[i.key] === 'POSITIVE'
                            return (
                                <Tag key={i.key} color={pos ? 'red' : 'green'}>
                                    {i.label}: {pos ? 'Dương tính' : 'Âm tính'}
                                </Tag>
                            )
                        })}
                    </div>
                </section>
            )}
        </div>
    )
}