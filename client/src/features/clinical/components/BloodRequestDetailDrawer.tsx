import { Drawer, Spin, Tag, Descriptions, Table, Alert, Button, Space } from 'antd'
import { EditOutlined, SendOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import { useClinicalRequestDetail } from '../hooks'
import { RequestStatusStepper } from './RequestStatusStepper'
import { getStatusMeta, getUrgencyMeta } from '../format'
import { formatBloodGroup } from '@/shared/formatters/medical'
import { formatDateTime } from '@/shared/formatters/date'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import type { BloodRequestAllocationDto, BloodRequestDetailDto, BloodRequestItemDetailDto } from '../types'
import './BloodRequestDetailDrawer.css'

interface BloodRequestDetailDrawerProps {
    requestId: number | null
    open: boolean
    onClose: () => void
    onEditDraft?: (detail: BloodRequestDetailDto) => void
    onSubmitDraft?: (detail: BloodRequestDetailDto) => void
}

export const BloodRequestDetailDrawer: React.FC<BloodRequestDetailDrawerProps> = ({
    requestId,
    open,
    onClose,
    onEditDraft,
    onSubmitDraft,
}) => {
    const { data: detail, isLoading } = useClinicalRequestDetail(requestId)
    const { can } = usePermission()
    const canEdit = can(PERMISSIONS.BLOOD_REQUEST_EDIT)

    const urgencyMeta = getUrgencyMeta(detail?.urgencyLevel)
    const statusMeta = getStatusMeta(detail?.status)

    const itemColumns: ColumnsType<BloodRequestItemDetailDto> = [
        {
            title: 'Chế phẩm máu',
            dataIndex: 'componentName',
            key: 'componentName',
            render: (text) => <span style={{ fontWeight: 600 }}>{text}</span>,
        },
        {
            title: 'Số lượng yêu cầu',
            dataIndex: 'requestedUnits',
            key: 'requestedUnits',
            align: 'center',
            render: (units) => `${units} túi`,
        },
        {
            title: 'Đã giữ chỗ / cấp phát',
            dataIndex: 'allocatedUnits',
            key: 'allocatedUnits',
            align: 'center',
            render: (allocated, record) => (
                <Tag color={allocated >= record.requestedUnits ? 'success' : allocated > 0 ? 'warning' : 'default'}>
                    {allocated} / {record.requestedUnits} túi
                </Tag>
            ),
        },
    ]

    const allocationColumns: ColumnsType<BloodRequestAllocationDto> = [
        {
            title: 'Mã vạch túi máu',
            dataIndex: 'barcode',
            key: 'barcode',
            render: (barcode) => <code style={{ fontWeight: 600, color: '#166534' }}>{barcode}</code>,
        },
        {
            title: 'Chế phẩm / Nhóm máu',
            key: 'product',
            render: (_, row) => (
                <div>
                    <div>{row.componentName}</div>
                    <small style={{ color: '#64748b' }}>
                        {formatBloodGroup(row.bloodType, row.rhFactor)} ({row.volumeMl} ml)
                    </small>
                </div>
            ),
        },
        {
            title: 'Trạng thái cấp phát',
            dataIndex: 'status',
            key: 'status',
            render: (status) => {
                const map: Record<string, { label: string; color: string }> = {
                    RESERVED: { label: 'Đã giữ chỗ', color: 'orange' },
                    ISSUED: { label: 'Đã xuất kho', color: 'blue' },
                    COMPLETED: { label: 'Đã tiếp nhận', color: 'green' },
                    RETURNED: { label: 'Đã hoàn trả', color: 'purple' },
                }
                const info = map[status] || { label: status, color: 'default' }
                return <Tag color={info.color}>{info.label}</Tag>
            },
        },
        {
            title: 'Tiến trình thực hiện',
            key: 'timeline',
            render: (_, row) => (
                <div style={{ fontSize: 12, color: '#475569' }}>
                    {row.reservedAt && <div>Giữ chỗ: {formatDateTime(row.reservedAt)} ({row.reservedByName || 'KTV'})</div>}
                    {row.issuedAt && <div>Xuất kho: {formatDateTime(row.issuedAt)} ({row.issuedByName || 'KTV'})</div>}
                    {row.receivedByName && <div style={{ color: '#15803d' }}>Thực nhận: {row.receivedByName}</div>}
                </div>
            ),
        },
    ]

    return (
        <Drawer
            title={
                detail ? (
                    <div className="request-drawer__header">
                        <span className="request-drawer__code">{detail.requestCode}</span>
                        <Tag color={statusMeta.color} style={{ marginLeft: 8 }}>
                            {statusMeta.label}
                        </Tag>
                        <Tag color={urgencyMeta.color} style={{ backgroundColor: urgencyMeta.bg, borderColor: urgencyMeta.border }}>
                            {urgencyMeta.label}
                        </Tag>
                    </div>
                ) : 'Chi tiết phiếu yêu cầu máu'
            }
            placement="right"
            width={680}
            open={open}
            onClose={onClose}
            destroyOnClose
            className="clinical-detail-drawer"
            footer={
                detail?.status === 'DRAFT' && canEdit ? (
                    <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 12 }}>
                        <Button onClick={() => onEditDraft?.(detail)} icon={<EditOutlined />}>
                            Tiếp tục chỉnh sửa
                        </Button>
                        <Button
                            type="primary"
                            icon={<SendOutlined />}
                            onClick={() => onSubmitDraft?.(detail)}
                            style={{ backgroundColor: '#1A7534', borderColor: '#1A7534' }}
                        >
                            Gửi phiếu yêu cầu
                        </Button>
                    </div>
                ) : undefined
            }
        >
            {isLoading ? (
                <div className="request-drawer__loading">
                    <Spin size="large" />
                    <p>Đang tải thông tin phiếu yêu cầu...</p>
                </div>
            ) : detail ? (
                <div className="request-drawer__content">
                    {/* Stepper tiến độ */}
                    <div className="request-drawer__stepper-box">
                        <div className="request-drawer__section-title">Tiến trình xử lý y lệnh</div>
                        <RequestStatusStepper currentStep={detail.currentStep} status={detail.status} />
                    </div>

                    {detail.rejectionReason && (
                        <Alert
                            type="error"
                            message="Lý do từ chối phiếu"
                            description={detail.rejectionReason}
                            showIcon
                            style={{ marginBottom: 20 }}
                        />
                    )}

                    {detail.status === 'DRAFT' && (
                        <Alert
                            type="warning"
                            message="Phiếu yêu cầu đang ở trạng thái Bản nháp"
                            description="Phiếu này chưa được gửi đến Ngân hàng máu để xử lý. Bạn có thể tiếp tục chỉnh sửa nội dung hoặc gửi chính thức ngay bây giờ."
                            showIcon
                            style={{ marginBottom: 20 }}
                            action={
                                canEdit ? (
                                    <Space>
                                        <Button size="small" onClick={() => onEditDraft?.(detail)} icon={<EditOutlined />}>
                                            Chỉnh sửa
                                        </Button>
                                        <Button
                                            size="small"
                                            type="primary"
                                            onClick={() => onSubmitDraft?.(detail)}
                                            icon={<SendOutlined />}
                                            style={{ backgroundColor: '#1A7534', borderColor: '#1A7534' }}
                                        >
                                            Gửi phiếu ngay
                                        </Button>
                                    </Space>
                                ) : undefined
                            }
                        />
                    )}

                    {/* Thông tin y lệnh */}
                    <div className="request-drawer__section">
                        <div className="request-drawer__section-title">Thông tin người bệnh & Y lệnh</div>
                        <Descriptions bordered size="small" column={2}>
                            <Descriptions.Item label="Người bệnh">
                                <strong>{detail.patientName}</strong> ({detail.patientCode})
                            </Descriptions.Item>
                            <Descriptions.Item label="Nhóm máu BN">
                                <Tag color="blue" style={{ fontWeight: 700 }}>
                                    {formatBloodGroup(detail.patientBloodType, detail.patientRh)}
                                </Tag>
                            </Descriptions.Item>
                            <Descriptions.Item label="Bác sĩ chỉ định">
                                {detail.doctorName || 'Chưa cập nhật'}
                            </Descriptions.Item>
                            <Descriptions.Item label="Khoa yêu cầu">
                                {detail.departmentName || 'Khoa Điều trị'}
                            </Descriptions.Item>
                            <Descriptions.Item label="Thời gian tạo">
                                {formatDateTime(detail.createdAt)}
                            </Descriptions.Item>
                            <Descriptions.Item label="Tổng số lượng">
                                <strong>{detail.totalAllocatedUnits} / {detail.totalRequestedUnits} túi</strong>
                            </Descriptions.Item>
                            <Descriptions.Item label="Chỉ định lâm sàng" span={2}>
                                {detail.diagnosis || 'Không có ghi chú thêm'}
                            </Descriptions.Item>
                        </Descriptions>
                    </div>

                    {/* Danh mục chế phẩm yêu cầu */}
                    <div className="request-drawer__section">
                        <div className="request-drawer__section-title">Danh sách chế phẩm yêu cầu</div>
                        <Table
                            dataSource={detail.items}
                            columns={itemColumns}
                            rowKey="requestItemId"
                            pagination={false}
                            size="small"
                        />
                    </div>

                    {/* Danh sách túi máu phân bổ / cấp phát */}
                    <div className="request-drawer__section">
                        <div className="request-drawer__section-title">
                            Túi máu giữ chỗ / Đã cấp phát ({detail.allocations?.length || 0})
                        </div>
                        {detail.allocations && detail.allocations.length > 0 ? (
                            <Table
                                dataSource={detail.allocations}
                                columns={allocationColumns}
                                rowKey="allocationId"
                                pagination={false}
                                size="small"
                            />
                        ) : (
                            <div className="request-drawer__no-alloc">
                                Chưa có túi máu nào được phân bổ cho phiếu này.
                            </div>
                        )}
                    </div>
                </div>
            ) : (
                <div className="request-drawer__empty">Không tìm thấy thông tin phiếu yêu cầu.</div>
            )}
        </Drawer>
    )
}
