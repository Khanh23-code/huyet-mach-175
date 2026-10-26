import { useState } from 'react'
import {
    App,
    Button,
    Input,
    Modal,
    Popconfirm,
    Select,
    Space,
    Table,
    Tag,
    Tooltip,
} from 'antd'
import type { TableColumnsType } from 'antd'
import {
    CheckCircleOutlined,
    CloseCircleOutlined,
    DeleteOutlined,
    EditOutlined,
    PlayCircleOutlined,
    SendOutlined,
} from '@ant-design/icons'
import CampaignForm from './CampaignForm'
import {
    useDeleteCampaign,
    useMyCampaigns,
    useSubmitApproval,
    useUpdateCampaignStatus,
} from '../hooks'
import {
    CAMPAIGN_STATUS_COLOR,
    CAMPAIGN_STATUS_LABEL,
    CAMPAIGN_TYPE_LABEL,
    fmtDate,
    fmtOperatingDays,
    normalizeCampaignStatus,
    normalizeCampaignType,
} from '../format'
import type { CampaignListItem, CampaignStatus } from '../types'
import { parseApiError } from '@/services/apiError'

const PAGE_SIZE = 10

interface Props {
    onOpenDetail?: (campaignId: number) => void
    onEdit?: (campaign: CampaignListItem) => void
}

export default function MyCampaigns({ onOpenDetail, onEdit }: Props) {
    const { message } = App.useApp()
    const [page, setPage] = useState(1)
    const [statusFilter, setStatusFilter] = useState<CampaignStatus | undefined>(undefined)
    const [keyword, setKeyword] = useState<string>('')
    const [editingCampaign, setEditingCampaign] = useState<CampaignListItem | null>(null)

    const { data, isLoading, isFetching } = useMyCampaigns({
        pageNumber: page,
        pageSize: PAGE_SIZE,
        status: statusFilter,
        keyword: keyword || undefined,
    })

    const submitApprovalMutation = useSubmitApproval()
    const deleteMutation = useDeleteCampaign()
    const updateStatusMutation = useUpdateCampaignStatus()

    const handleSubmitApproval = async (campaign: CampaignListItem) => {
        try {
            await submitApprovalMutation.mutateAsync(campaign.campaignId)
            message.success(`Đã gửi đợt hiến "${campaign.campaignName}" lên phê duyệt`)
        } catch (err) {
            message.error(parseApiError(err).message)
        }
    }

    const handleDelete = async (campaign: CampaignListItem) => {
        try {
            await deleteMutation.mutateAsync(campaign.campaignId)
            message.success(`Đã xóa bản nháp "${campaign.campaignName}"`)
        } catch (err) {
            message.error(parseApiError(err).message)
        }
    }

    const handleUpdateStatus = async (campaign: CampaignListItem, newStatus: CampaignStatus, label: string) => {
        try {
            await updateStatusMutation.mutateAsync({ id: campaign.campaignId, status: newStatus })
            message.success(`Đã chuyển chiến dịch sang trạng thái: ${label}`)
        } catch (err) {
            message.error(parseApiError(err).message)
        }
    }

    const handleEditClick = (campaign: CampaignListItem) => {
        if (onEdit) {
            onEdit(campaign)
        } else {
            setEditingCampaign(campaign)
        }
    }

    const columns: TableColumnsType<CampaignListItem> = [
        {
            title: 'CHIẾN DỊCH / ĐIỂM HIẾN',
            width: 280,
            render: (_, c) => {
                const cType = normalizeCampaignType(c.campaignType)
                return (
                    <div>
                        <a
                            style={{ fontWeight: 600, color: 'inherit' }}
                            onClick={() => onOpenDetail?.(c.campaignId)}
                        >
                            {c.campaignName}
                        </a>
                        <div className="ds-muted ds-small">{c.locationName}</div>
                        <Space size={4} orientation="horizontal" style={{ marginTop: 4 }}>
                            <Tag color={cType === 'EVENT' ? 'blue' : 'orange'}>
                                {CAMPAIGN_TYPE_LABEL[cType]}
                            </Tag>
                            {cType === 'RECURRING' && c.operatingDays && (
                                <span className="ds-muted ds-small">({fmtOperatingDays(c.operatingDays)})</span>
                            )}
                        </Space>
                    </div>
                )
            },
        },
        {
            title: 'THỜI GIAN',
            width: 200,
            render: (_, c) => (
                <div className="ds-small">
                    <div><b>Bắt đầu:</b> {fmtDate(c.startDate)}</div>
                    <div><b>Kết thúc:</b> {fmtDate(c.endDate)}</div>
                </div>
            ),
        },
        {
            title: 'TIẾN ĐỘ THU GOM',
            width: 170,
            render: (_, c) => {
                const cStatus = normalizeCampaignStatus(c.status)
                if (cStatus === 'REJECTED' || cStatus === 'CANCELLED') {
                    return <span className="ds-muted ds-small">Không tiếp nhận đăng ký</span>
                }
                return (
                    <div>
                        <div>
                            <b>{c.totalRegistered}</b> / {c.targetDonations} đăng ký
                        </div>
                        <div className="ds-muted ds-small">
                            Đã thu gom: <b>{c.totalCollected}</b> túi
                        </div>
                    </div>
                )
            },
        },
        {
            title: 'TRẠNG THÁI',
            dataIndex: 'status',
            width: 160,
            render: (s: unknown) => {
                const cStatus = normalizeCampaignStatus(s)
                return (
                    <div>
                        <Tag color={CAMPAIGN_STATUS_COLOR[cStatus]} style={{ fontWeight: 600 }}>
                            {CAMPAIGN_STATUS_LABEL[cStatus]}
                        </Tag>
                    </div>
                )
            },
        },
        {
            title: 'THAO TÁC',
            width: 180,
            align: 'right',
            render: (_, c) => {
                const status = normalizeCampaignStatus(c.status)
                return (
                    <Space size="small">
                        {status === 'DRAFT' && (
                            <>
                                <Tooltip title="Gửi duyệt">
                                    <Button
                                        type="text"
                                        icon={<SendOutlined style={{ color: '#1a7534', fontSize: 16 }} />}
                                        loading={submitApprovalMutation.isPending}
                                        onClick={() => handleSubmitApproval(c)}
                                    />
                                </Tooltip>
                                <Tooltip title="Chỉnh sửa (Điền lên form)">
                                    <Button
                                        type="text"
                                        icon={<EditOutlined style={{ color: '#0284c7', fontSize: 16 }} />}
                                        onClick={() => handleEditClick(c)}
                                    />
                                </Tooltip>
                                <Popconfirm
                                    title="Xóa bản nháp này?"
                                    description="Bạn có chắc chắn muốn xóa bản nháp chiến dịch này?"
                                    okText="Xóa"
                                    cancelText="Hủy"
                                    okButtonProps={{ danger: true }}
                                    onConfirm={() => handleDelete(c)}
                                >
                                    <Button
                                        type="text"
                                        danger
                                        icon={<DeleteOutlined style={{ fontSize: 16 }} />}
                                        loading={deleteMutation.isPending}
                                    />
                                </Popconfirm>
                            </>
                        )}

                        {status === 'REJECTED' && (
                            <>
                                <Tooltip title="Sửa & chuẩn bị gửi lại">
                                    <Button
                                        type="text"
                                        icon={<EditOutlined style={{ color: '#0284c7', fontSize: 16 }} />}
                                        onClick={() => handleEditClick(c)}
                                    />
                                </Tooltip>
                                <Tooltip title="Gửi duyệt lại">
                                    <Button
                                        type="text"
                                        icon={<SendOutlined style={{ color: '#1a7534', fontSize: 16 }} />}
                                        loading={submitApprovalMutation.isPending}
                                        onClick={() => handleSubmitApproval(c)}
                                    />
                                </Tooltip>
                            </>
                        )}

                        {status === 'APPROVED' && (
                            <>
                                <Tooltip title="Bắt đầu diễn ra">
                                    <Button
                                        type="text"
                                        icon={<PlayCircleOutlined style={{ color: '#1a7534', fontSize: 16 }} />}
                                        onClick={() => handleUpdateStatus(c, 'IN_PROGRESS', 'Đang diễn ra')}
                                    />
                                </Tooltip>
                                <Popconfirm
                                    title="Hủy đợt hiến máu này?"
                                    description="Đợt hiến đã duyệt này sẽ chuyển sang trạng thái Hủy."
                                    onConfirm={() => handleUpdateStatus(c, 'CANCELLED', 'Đã hủy')}
                                >
                                    <Button
                                        type="text"
                                        danger
                                        icon={<CloseCircleOutlined style={{ fontSize: 16 }} />}
                                    />
                                </Popconfirm>
                            </>
                        )}

                        {status === 'IN_PROGRESS' && (
                            <>
                                <Tooltip title="Hoàn thành đợt hiến máu">
                                    <Button
                                        type="text"
                                        icon={<CheckCircleOutlined style={{ color: '#6d28d9', fontSize: 16 }} />}
                                        onClick={() => handleUpdateStatus(c, 'COMPLETED', 'Đã hoàn thành')}
                                    />
                                </Tooltip>
                                <Popconfirm
                                    title="Hủy đợt hiến máu này?"
                                    description="Đợt hiến đang diễn ra sẽ bị hủy."
                                    onConfirm={() => handleUpdateStatus(c, 'CANCELLED', 'Đã hủy')}
                                >
                                    <Button
                                        type="text"
                                        danger
                                        icon={<CloseCircleOutlined style={{ fontSize: 16 }} />}
                                    />
                                </Popconfirm>
                            </>
                        )}
                    </Space>
                )
            },
        },
    ]

    return (
        <section className="ds-card">
            <div className="ds-card__head ds-card__head--row">
                <div>
                    <h3>Đợt hiến máu của tôi</h3>
                    <p>Theo dõi tiến độ, gửi phê duyệt hoặc cập nhật trạng thái các đợt hiến do bạn khởi tạo.</p>
                </div>
                <Space wrap>
                    <Input.Search
                        placeholder="Tìm theo tên hoặc địa điểm..."
                        allowClear
                        onSearch={(v) => {
                            setKeyword(v.trim())
                            setPage(1)
                        }}
                        style={{ width: 220 }}
                    />
                    <Select
                        placeholder="Tất cả trạng thái"
                        allowClear
                        style={{ width: 160 }}
                        value={statusFilter}
                        onChange={(v) => {
                            setStatusFilter(v)
                            setPage(1)
                        }}
                        options={[
                            { value: 'DRAFT', label: 'Bản nháp' },
                            { value: 'PENDING_APPROVAL', label: 'Chờ phê duyệt' },
                            { value: 'APPROVED', label: 'Đã phê duyệt' },
                            { value: 'IN_PROGRESS', label: 'Đang diễn ra' },
                            { value: 'COMPLETED', label: 'Đã hoàn thành' },
                            { value: 'CANCELLED', label: 'Đã hủy' },
                            { value: 'REJECTED', label: 'Bị từ chối' },
                        ]}
                    />
                </Space>
            </div>

            <Table<CampaignListItem>
                rowKey="campaignId"
                columns={columns}
                dataSource={data?.items ?? []}
                loading={isLoading || isFetching}
                scroll={{ x: 960 }}
                locale={{ emptyText: 'Chưa có đợt hiến máu nào' }}
                pagination={{
                    current: page,
                    pageSize: PAGE_SIZE,
                    total: data?.totalCount ?? 0,
                    showSizeChanger: false,
                    hideOnSinglePage: true,
                    onChange: setPage,
                }}
            />

            {/* Modal chỉnh sửa đợt hiến khi không dùng chế độ onEdit */}
            <Modal
                title={`Chỉnh sửa đợt hiến máu: ${editingCampaign?.campaignName ?? ''}`}
                open={Boolean(editingCampaign)}
                onCancel={() => setEditingCampaign(null)}
                footer={null}
                width={860}
                destroyOnHidden
            >
                {editingCampaign && (
                    <CampaignForm
                        campaign={editingCampaign}
                        onSuccess={() => setEditingCampaign(null)}
                        onCancel={() => setEditingCampaign(null)}
                    />
                )}
            </Modal>
        </section>
    )
}
