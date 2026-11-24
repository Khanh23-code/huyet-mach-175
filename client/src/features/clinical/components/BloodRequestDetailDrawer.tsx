import { useState } from 'react'
import { App, Drawer, Spin, Descriptions, Table, Alert, Button, Space } from 'antd'
import { EditOutlined, HeartOutlined, SendOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import { BloodGroupTag, StatusPill, UrgencyTag, type StatusPillTone } from '@/shared/components'
import { parseApiError } from '@/services/apiError'
import { clinicalApi } from '../clinicalApi'
import { useClinicalRequestDetail } from '../hooks'
import { ConfirmTransfusionModal } from './ConfirmTransfusionModal'
import { RequestStatusStepper } from './RequestStatusStepper'
import { getStatusMeta, getStatusTone } from '../format'
import { formatBloodGroup } from '@/shared/formatters/medical'
import { formatDateTime } from '@/shared/formatters/date'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import type {
    BloodRequestAllocationDto,
    BloodRequestDetailDto,
    BloodRequestItemDetailDto,
    TransfusionScanDto,
} from '../types'
import './BloodRequestDetailDrawer.css'

interface BloodRequestDetailDrawerProps {
    requestId: number | null
    open: boolean
    onClose: () => void
    onEditDraft?: (detail: BloodRequestDetailDto) => void
    onSubmitDraft?: (detail: BloodRequestDetailDto) => void
}

const ALLOCATION_STATUS_MAP: Record<string, { label: string; tone: StatusPillTone }> = {
    RESERVED: { label: 'Đã giữ chỗ', tone: 'warning' },
    ISSUED: { label: 'Đã xuất kho', tone: 'info' },
    COMPLETED: { label: 'Đã truyền / hoàn tất', tone: 'success' },
    RETURNED: { label: 'Đã hoàn trả', tone: 'purple' },
}

export const BloodRequestDetailDrawer: React.FC<BloodRequestDetailDrawerProps> = ({
    requestId,
    open,
    onClose,
    onEditDraft,
    onSubmitDraft,
}) => {
    const { message } = App.useApp()
    const { data: detail, isLoading } = useClinicalRequestDetail(requestId)
    const { can } = usePermission()
    const canEdit = can(PERMISSIONS.BLOOD_REQUEST_EDIT)

    const [loadingTransfusionId, setLoadingTransfusionId] = useState<number | null>(null)
    const [transfusionBag, setTransfusionBag] = useState<TransfusionScanDto | null>(null)
    const [transfusionModalOpen, setTransfusionModalOpen] = useState(false)

    const statusMeta = getStatusMeta(detail?.status)

    const handleQuickTransfusion = async (row: BloodRequestAllocationDto) => {
        if (loadingTransfusionId !== null) return
        setLoadingTransfusionId(row.allocationId)
        try {
            const scanData = await clinicalApi.scanTransfusionBag(row.barcode)
            if (scanData.bagStatus !== 'ISSUED') {
                message.error(
                    `Túi máu ${scanData.barcode} hiện không ở trạng thái đã xuất kho, không thể xác nhận truyền máu.`,
                )
                return
            }
            setTransfusionBag(scanData)
            setTransfusionModalOpen(true)
        } catch (err) {
            message.error(parseApiError(err).message)
        } finally {
            setLoadingTransfusionId(null)
        }
    }

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
            render: (units) => <span className="tabular-nums">{units} túi</span>,
        },
        {
            title: 'Đã giữ chỗ / cấp phát',
            dataIndex: 'allocatedUnits',
            key: 'allocatedUnits',
            align: 'center',
            render: (allocated, record) => (
                <StatusPill
                    tone={allocated >= record.requestedUnits ? 'success' : allocated > 0 ? 'warning' : 'neutral'}
                    dot={false}
                >
                    {allocated} / {record.requestedUnits} túi
                </StatusPill>
            ),
        },
    ]

    const allocationColumns: ColumnsType<BloodRequestAllocationDto> = [
        {
            title: 'Mã vạch túi máu',
            dataIndex: 'barcode',
            key: 'barcode',
            render: (barcode) => (
                <code className="text-mono" style={{ fontWeight: 600, color: 'var(--c-primary)' }}>
                    {barcode}
                </code>
            ),
        },
        {
            title: 'Chế phẩm / Nhóm máu',
            key: 'product',
            render: (_, row) => (
                <div>
                    <div>{row.componentName}</div>
                    <small style={{ color: 'var(--c-text-muted)' }}>
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
                const info = ALLOCATION_STATUS_MAP[status] || { label: status, tone: 'neutral' as StatusPillTone }
                return <StatusPill tone={info.tone}>{info.label}</StatusPill>
            },
        },
        {
            title: 'Tiến trình thực hiện',
            key: 'timeline',
            render: (_, row) => (
                <div style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-secondary)' }}>
                    {row.reservedAt && <div>Giữ chỗ: {formatDateTime(row.reservedAt)} ({row.reservedByName || 'KTV'})</div>}
                    {row.issuedAt && <div>Xuất kho: {formatDateTime(row.issuedAt)} ({row.issuedByName || 'KTV'})</div>}
                    {row.receivedByName && <div style={{ color: 'var(--c-success)' }}>Thực nhận: {row.receivedByName}</div>}
                </div>
            ),
        },
        ...(canEdit
            ? [
                  {
                      title: 'Thao tác',
                      key: 'action',
                      align: 'right' as const,
                      render: (_: unknown, row: BloodRequestAllocationDto) =>
                          row.status === 'ISSUED' && (!row.bagStatus || row.bagStatus === 'ISSUED') ? (
                              <Button
                                  size="small"
                                  type="primary"
                                  ghost
                                  icon={<HeartOutlined />}
                                  loading={loadingTransfusionId === row.allocationId}
                                  onClick={() => void handleQuickTransfusion(row)}
                              >
                                  Truyền máu
                              </Button>
                          ) : (
                              <span style={{ color: 'var(--c-text-muted)' }}>—</span>
                          ),
                  },
              ]
            : []),
    ]

    return (
        <>
        <Drawer
            title={
                detail ? (
                    <div className="request-drawer__header">
                        <span className="request-drawer__code">{detail.requestCode}</span>
                        <StatusPill tone={getStatusTone(detail.status)}>
                            {statusMeta.label}
                        </StatusPill>
                        <UrgencyTag level={detail.urgencyLevel} />
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
                        <div className="request-drawer__section-title">Thông tin người bệnh &amp; Y lệnh</div>
                        <Descriptions bordered size="small" column={2}>
                            <Descriptions.Item label="Người bệnh">
                                <strong>{detail.patientName}</strong> ({detail.patientCode})
                            </Descriptions.Item>
                            <Descriptions.Item label="Nhóm máu BN">
                                <BloodGroupTag value={formatBloodGroup(detail.patientBloodType, detail.patientRh)} />
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
                                <strong className="tabular-nums">
                                    {detail.totalAllocatedUnits} / {detail.totalRequestedUnits} túi
                                </strong>
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
        <ConfirmTransfusionModal
            open={transfusionModalOpen}
            bag={transfusionBag}
            onClose={() => setTransfusionModalOpen(false)}
        />
        </>
    )
}
