import { Descriptions, Empty, Modal, Skeleton, Table, Tag } from 'antd'
import { STATUS_COLOR, STATUS_LABEL, normalizeStatus } from '@/features/reception/format'
import { useDonorDetail } from '../hooks'
import { SCREENING_LABEL, SESSION_LABEL, fmtBloodType, fmtDate } from '../format'
import type { DonorHistoryItem } from '../types'

interface Props {
    donorId: number | null
    onClose: () => void
}

export default function DonorDetailModal({ donorId, onClose }: Props) {
    const { data: donor, isLoading, isError } = useDonorDetail(donorId)

    return (
        <Modal
            title="Thông tin người hiến máu"
            open={donorId !== null}
            onCancel={onClose}
            footer={null}
            width={860}
            destroyOnHidden
        >
            {isLoading ? (
                <Skeleton active paragraph={{ rows: 6 }} />
            ) : isError || !donor ? (
                <Empty description="Không tải được thông tin người hiến" />
            ) : (
                <>
                    <Descriptions column={2} size="small" bordered style={{ marginBottom: 16 }}>
                        <Descriptions.Item label="Họ và tên" span={2}>
                            <b>{donor.fullName}</b>
                        </Descriptions.Item>
                        <Descriptions.Item label="Số CCCD">{donor.citizenId}</Descriptions.Item>
                        <Descriptions.Item label="Ngày sinh">{fmtDate(donor.dateOfBirth)}</Descriptions.Item>
                        <Descriptions.Item label="Giới tính">{donor.gender}</Descriptions.Item>
                        <Descriptions.Item label="Số điện thoại">{donor.phoneNumber}</Descriptions.Item>
                        <Descriptions.Item label="Email" span={2}>{donor.email || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Nhóm máu">
                            <Tag className="campaign-tag">{fmtBloodType(donor.bloodType, donor.rhFactor)}</Tag>
                        </Descriptions.Item>
                        <Descriptions.Item label="Tổng số lần hiến">{donor.totalDonations}</Descriptions.Item>
                        <Descriptions.Item label="Lần hiến gần nhất" span={2}>
                            {fmtDate(donor.lastDonationDate)}
                        </Descriptions.Item>
                    </Descriptions>

                    <div style={{ fontWeight: 600, marginBottom: 8 }}>Lịch sử hiến máu</div>
                    <Table<DonorHistoryItem>
                        size="small"
                        rowKey="appointmentId"
                        pagination={false}
                        dataSource={donor.donationHistory}
                        locale={{ emptyText: 'Chưa có lịch sử hiến máu' }}
                        scroll={{ x: 720, y: 260 }}
                        columns={[
                            {
                                title: 'Ngày hẹn',
                                render: (_, r) => (
                                    <div>
                                        {fmtDate(r.appointmentDate)}
                                        <div style={{ color: 'var(--c-text-muted)', fontSize: 12 }}>
                                            {r.isWalkIn ? 'Vãng lai' : r.timeSlot}
                                        </div>
                                    </div>
                                ),
                            },
                            { title: 'Đợt hiến', dataIndex: 'campaignName', render: (v) => v || 'Tại viện' },
                            {
                                title: 'Lịch hẹn',
                                render: (_, r) => {
                                    const st = normalizeStatus(r.status)
                                    return <Tag color={STATUS_COLOR[st]}>{STATUS_LABEL[st]}</Tag>
                                },
                            },
                            {
                                title: 'Sàng lọc',
                                render: (_, r) =>
                                    r.screeningStatus ? (
                                        <div>
                                            {SCREENING_LABEL[r.screeningStatus] ?? r.screeningStatus}
                                            {r.deferralReason && (
                                                <div style={{ color: 'var(--c-danger)', fontSize: 12 }}>{r.deferralReason}</div>
                                            )}
                                        </div>
                                    ) : '—',
                            },
                            {
                                title: 'Lấy máu',
                                render: (_, r) =>
                                    r.sessionStatus ? SESSION_LABEL[r.sessionStatus] ?? r.sessionStatus : '—',
                            },
                            {
                                title: 'Thể tích',
                                dataIndex: 'actualVolumeMl',
                                render: (v) => (v ? `${v} ml` : '—'),
                            },
                        ]}
                    />
                </>
            )}
        </Modal>
    )
}