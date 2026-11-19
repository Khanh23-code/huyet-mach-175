import { useEffect, useMemo, useState } from 'react'
import {
    Alert,
    Button,
    Checkbox,
    Empty,
    Input,
    Modal,
    Pagination,
    Skeleton,
    Space,
    Switch,
    Table,
    Tag,
    message,
} from 'antd'
import {
    CheckCircleOutlined,
    CloseCircleOutlined,
    ExclamationCircleOutlined,
    ReloadOutlined,
    RightOutlined,
    SearchOutlined,
} from '@ant-design/icons'
import { PERMISSIONS } from '@/config/permissions'
import Can from '@/features/auth/components/Can'
import { usePermission } from '@/features/auth/usePermission'
import {
    useAllocateBags,
    useAvailableBags,
    useCoordinationQueue,
    useRequestDetail,
} from '../hooks'
import type {
    AllocateBloodBagsResponse,
    BloodRequestItemDetailDto,
    UrgencyLevel,
} from '../types'
import RejectRequestModal from '../components/RejectRequestModal'
import AllocateSuccessModal from '../components/AllocateSuccessModal'
import './CoordinationQueuePage.css'

export default function CoordinationQueuePage() {
    const { can } = usePermission()
    const canApprove = can(PERMISSIONS.BLOOD_REQUEST_APPROVE)

    // 1. Quản lý danh sách hàng đợi
    const {
        data: queueData,
        loading: queueLoading,
        isFetching: queueFetching,
        error: queueError,
        filter,
        setFilter,
        refetch: refetchQueue,
    } = useCoordinationQueue()

    // ID phiếu đang được chọn
    const [selectedRequestId, setSelectedRequestId] = useState<number | null>(null)

    // 2. Chi tiết phiếu đang chọn
    const {
        data: requestDetail,
        loading: detailLoading,
        isFetching: detailFetching,
        error: detailError,
        refetch: refetchDetail,
    } = useRequestDetail(selectedRequestId)

    // Sub-tab: 'review' (Thẩm định) | 'allocation' (Chọn & giữ chỗ)
    const [activeTab, setActiveTab] = useState<'review' | 'allocation'>('review')

    // Ghi nhận những phiếu đã được thẩm định & bấm "Tiến hành điều phối" trong phiên UI
    const [reviewedRequestIds, setReviewedRequestIds] = useState<Record<number, boolean>>({})

    // Dòng chế phẩm đang chọn để điều phối
    const [activeItemId, setActiveItemId] = useState<number | null>(null)

    // Chế độ tương thích nhóm máu
    const [includeCompatible, setIncludeCompatible] = useState<boolean>(false)

    // Danh sách túi máu đang được chọn bằng checkbox
    const [selectedBagIds, setSelectedBagIds] = useState<number[]>([])

    // State các Modal & Mutation
    const [showRejectModal, setShowRejectModal] = useState<boolean>(false)
    const allocateMutation = useAllocateBags()
    const allocateSubmitting = allocateMutation.isPending
    const [allocateResult, setAllocateResult] = useState<AllocateBloodBagsResponse | null>(null)
    const [showSuccessModal, setShowSuccessModal] = useState<boolean>(false)

    const isPendingRequest = requestDetail?.status === 'PENDING'
    const isCurrentRequestReviewed = Boolean(requestDetail && reviewedRequestIds[requestDetail.requestId])
    const canEnterAllocationTab = Boolean(canApprove && isPendingRequest && isCurrentRequestReviewed)

    // Tự động chọn phiếu đầu tiên khi danh sách hàng đợi tải xong
    useEffect(() => {
        if (queueData && queueData.items.length > 0 && selectedRequestId === null) {
            setSelectedRequestId(queueData.items[0].requestId)
        }
    }, [queueData, selectedRequestId])

    // Khi chi tiết phiếu tải xong, tự động chọn dòng y lệnh đầu tiên
    useEffect(() => {
        if (requestDetail && requestDetail.items && requestDetail.items.length > 0) {
            // Ưu tiên dòng còn thiếu túi
            const itemWithShortage = requestDetail.items.find(
                (i) => i.requestedUnits > i.allocatedUnits,
            )
            setActiveItemId(
                itemWithShortage ? itemWithShortage.requestItemId : requestDetail.items[0].requestItemId,
            )
        } else {
            setActiveItemId(null)
        }
        // Xóa danh sách túi đã chọn khi đổi phiếu
        setSelectedBagIds([])
    }, [requestDetail])

    // Không cho phép đứng ở tab Chọn & giữ chỗ nếu chưa thẩm định hoặc phiếu không còn PENDING / thiếu quyền
    useEffect(() => {
        if (activeTab === 'allocation' && !canEnterAllocationTab) {
            setActiveTab('review')
        }
    }, [activeTab, canEnterAllocationTab])

    // 3. Tải danh sách túi máu khả dụng cho dòng y lệnh đang chọn (chỉ gọi khi đã mở bước Chọn & giữ chỗ)
    const {
        bags: availableBags,
        loading: bagsLoading,
        isFetching: bagsFetching,
        error: bagsError,
        refetch: refetchBags,
    } = useAvailableBags(
        selectedRequestId,
        activeItemId,
        includeCompatible,
        activeTab === 'allocation' && canEnterAllocationTab,
    )

    // Xóa lựa chọn túi khi chuyển dòng y lệnh hoặc đổi chế độ tương thích
    useEffect(() => {
        setSelectedBagIds([])
    }, [activeItemId, includeCompatible])

    // Dòng y lệnh hiện tại
    const currentItem: BloodRequestItemDetailDto | null = useMemo(() => {
        if (!requestDetail || !requestDetail.items || !activeItemId) return null
        return requestDetail.items.find((i) => i.requestItemId === activeItemId) || null
    }, [requestDetail, activeItemId])

    // Số túi còn thiếu cho dòng y lệnh đang chọn
    const remainingUnits = useMemo(() => {
        if (!currentItem) return 0
        return Math.max(0, currentItem.requestedUnits - currentItem.allocatedUnits)
    }, [currentItem])

    // Đếm số ca cấp cứu đang chờ trong hàng đợi
    const emergencyCount = useMemo(() => {
        if (!queueData || !queueData.items) return 0
        return queueData.items.filter((item) => item.urgencyLevel === 'EMERGENCY').length
    }, [queueData])

    // Helper render nhãn mức độ ưu tiên
    const renderUrgencyTag = (level: UrgencyLevel) => {
        switch (level) {
            case 'EMERGENCY':
                return <Tag color="error">Cấp cứu</Tag>
            case 'URGENT':
                return <Tag color="warning">Khẩn</Tag>
            case 'ROUTINE':
                return <Tag color="processing">Kế hoạch</Tag>
            default:
                return <Tag>{level}</Tag>
        }
    }

    // Xử lý toggle chọn túi máu
    const handleToggleBag = (bagId: number) => {
        if (!canApprove || !isPendingRequest) return
        if (selectedBagIds.includes(bagId)) {
            setSelectedBagIds((prev) => prev.filter((id) => id !== bagId))
        } else {
            if (selectedBagIds.length >= remainingUnits) {
                message.warning(
                    `Dòng y lệnh chỉ còn thiếu ${remainingUnits} túi. Bạn không thể chọn vượt quá số lượng này.`,
                )
                return
            }
            setSelectedBagIds((prev) => [...prev, bagId])
        }
    }

    // Xử lý xác nhận giữ chỗ
    const handleConfirmAllocate = () => {
        if (!canApprove) {
            message.error('Bạn không có quyền điều phối hoặc giữ chỗ túi máu.')
            return
        }
        if (!isPendingRequest) {
            message.warning('Chỉ có thể giữ chỗ túi máu cho phiếu đang ở trạng thái PENDING.')
            return
        }
        if (!selectedRequestId || !activeItemId || selectedBagIds.length === 0) {
            message.warning('Vui lòng chọn ít nhất 1 túi máu để giữ chỗ.')
            return
        }

        const selectedBagsDetail = availableBags.filter((b) => selectedBagIds.includes(b.bagId))

        Modal.confirm({
            title: 'Xác nhận giữ chỗ túi máu',
            icon: <ExclamationCircleOutlined style={{ color: '#16a34a' }} />,
            content: (
                <div style={{ marginTop: 10 }}>
                    <p>
                        Bạn đang xác nhận giữ chỗ <strong>{selectedBagIds.length} túi máu</strong> cho dòng y lệnh{' '}
                        <strong>{currentItem?.componentName}</strong> của phiếu{' '}
                        <strong>{requestDetail?.requestCode}</strong>:
                    </p>
                    <ul style={{ paddingLeft: 20, maxHeight: 120, overflowY: 'auto' }}>
                        {selectedBagsDetail.map((b) => (
                            <li key={b.bagId}>
                                <code>{b.barcode}</code> ({b.bloodType} · {b.volumeMl}ml)
                            </li>
                        ))}
                    </ul>
                </div>
            ),
            okText: 'Xác nhận giữ chỗ',
            cancelText: 'Hủy bỏ',
            okButtonProps: { style: { backgroundColor: '#1A7534', borderColor: '#1A7534' } },
            onOk: async () => {
                try {
                    const res = await allocateMutation.mutateAsync({
                        requestId: selectedRequestId,
                        requestItemId: activeItemId,
                        data: { bagIds: selectedBagIds },
                    })
                    setSelectedBagIds([])
                    setAllocateResult(res)
                    setShowSuccessModal(true)
                } catch (err: unknown) {
                    const msg =
                        (err as { response?: { data?: { message?: string } } })?.response?.data?.message ||
                        (err instanceof Error ? err.message : 'Giữ chỗ túi máu không thành công.')
                    message.error(msg)
                    // Nếu lỗi (ví dụ 409 conflict do túi đã bị giữ), làm mới danh sách túi
                    refetchBags()
                }
            },
        })
    }

    // Kiểm tra túi máu cận hạn (còn dưới 7 ngày)
    const isNearExpiry = (expiresAt: string) => {
        const diffDays = (new Date(expiresAt).getTime() - new Date().getTime()) / (1000 * 3600 * 24)
        return diffDays >= 0 && diffDays <= 7
    }

    return (
        <div className="coordination-queue-container">
            {/* Header & Title */}
            <div>
                <div className="queue-breadcrumb">Kho máu / Hàng đợi yêu cầu máu</div>
                <div className="queue-header-row">
                    <div>
                        <h1 className="queue-title">Hàng đợi yêu cầu máu</h1>
                        <p className="queue-subtitle">
                            Thẩm định yêu cầu, chọn và giữ chỗ túi máu cho các phiếu đang chờ xử lý.
                        </p>
                    </div>

                    <Space size={12}>
                        {emergencyCount > 0 && (
                            <div className="emergency-alert-badge">
                                <span className="emergency-dot" />
                                <span>{emergencyCount} Cấp cứu đang chờ</span>
                            </div>
                        )}
                        <Button
                            icon={<ReloadOutlined spin={queueLoading || queueFetching || detailFetching || bagsFetching} />}
                            onClick={() => {
                                refetchQueue()
                                if (selectedRequestId) refetchDetail()
                                if (activeTab === 'allocation' && canEnterAllocationTab) {
                                    refetchBags()
                                }
                            }}
                            size="small"
                        >
                            Làm mới
                        </Button>
                    </Space>
                </div>
            </div>

            {/* Layout 2 cột: Cột trái (Hàng đợi) | Cột phải (Chi tiết & Workspace) */}
            <div className="queue-split-layout">
                {/* CỘT TRÁI: DANH SÁCH HÀNG ĐỢI */}
                <div className="queue-list-card">
                    <div className="queue-list-header">
                        <h2 className="queue-list-title">
                            Hàng đợi ({queueData?.totalCount ?? 0})
                        </h2>
                    </div>

                    {/* Ô tìm kiếm */}
                    <Input
                        placeholder="Tìm mã phiếu, bệnh nhân, khoa..."
                        prefix={<SearchOutlined style={{ color: '#94a3b8' }} />}
                        allowClear
                        value={filter.search}
                        onChange={(e) => setFilter((prev) => ({ ...prev, search: e.target.value, pageNumber: 1 }))}
                        className="queue-search-input"
                    />

                    {/* Bộ lọc mức độ ưu tiên */}
                    <div className="queue-filter-pills">
                        <button
                            type="button"
                            className={`filter-pill-btn ${filter.urgencyLevel === undefined ? 'is-active' : ''}`}
                            onClick={() => setFilter((prev) => ({ ...prev, urgencyLevel: undefined, pageNumber: 1 }))}
                        >
                            Tất cả
                        </button>
                        <button
                            type="button"
                            className={`filter-pill-btn ${filter.urgencyLevel === 'EMERGENCY' ? 'is-active' : ''}`}
                            onClick={() => setFilter((prev) => ({ ...prev, urgencyLevel: 'EMERGENCY', pageNumber: 1 }))}
                        >
                            Cấp cứu
                        </button>
                        <button
                            type="button"
                            className={`filter-pill-btn ${filter.urgencyLevel === 'URGENT' ? 'is-active' : ''}`}
                            onClick={() => setFilter((prev) => ({ ...prev, urgencyLevel: 'URGENT', pageNumber: 1 }))}
                        >
                            Khẩn
                        </button>
                        <button
                            type="button"
                            className={`filter-pill-btn ${filter.urgencyLevel === 'ROUTINE' ? 'is-active' : ''}`}
                            onClick={() => setFilter((prev) => ({ ...prev, urgencyLevel: 'ROUTINE', pageNumber: 1 }))}
                        >
                            Kế hoạch
                        </button>
                    </div>

                    {/* Error State */}
                    {queueError && (
                        <Alert
                            message="Lỗi tải hàng đợi"
                            description={queueError}
                            type="error"
                            showIcon
                        />
                    )}

                    {/* Loading State */}
                    {queueLoading && !queueData && (
                        <Skeleton active paragraph={{ rows: 5 }} />
                    )}

                    {/* Empty State */}
                    {!queueLoading && queueData && queueData.items.length === 0 && (
                        <Empty
                            description="Không có phiếu yêu cầu nào trong hàng đợi."
                            image={Empty.PRESENTED_IMAGE_SIMPLE}
                        />
                    )}

                    {/* Danh sách phiếu */}
                    {queueData && queueData.items.length > 0 && (
                        <div className="queue-items-container">
                            {queueData.items.map((item) => {
                                const isSelected = selectedRequestId === item.requestId
                                const isEmergency = item.urgencyLevel === 'EMERGENCY'
                                const isUrgent = item.urgencyLevel === 'URGENT'

                                return (
                                    <div
                                        key={item.requestId}
                                        className={`queue-item-card ${isSelected ? 'is-selected' : ''} ${
                                            isEmergency ? 'is-emergency' : isUrgent ? 'is-urgent' : ''
                                        }`}
                                        onClick={() => {
                                            setSelectedRequestId(item.requestId)
                                            setActiveTab('review')
                                        }}
                                    >
                                        <div className="queue-item-top">
                                            <div>
                                                <div className="queue-item-code">{item.requestCode}</div>
                                                <div className="queue-item-dept">
                                                    {item.departmentName || 'Khoa lâm sàng'}
                                                </div>
                                            </div>
                                            <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                                                <Tag color="magenta">
                                                    {item.patientBloodType}
                                                    {item.patientRh === 'POSITIVE' ? '+' : '-'}
                                                </Tag>
                                                {renderUrgencyTag(item.urgencyLevel)}
                                            </div>
                                        </div>

                                        <div className="queue-item-patient">
                                            {item.patientName} ({item.patientCode})
                                        </div>

                                        <div className="queue-item-bottom">
                                            <span>
                                                Yêu cầu: <strong>{item.totalRequestedUnits}</strong> túi · Đã giữ:{' '}
                                                <strong>{item.totalAllocatedUnits}</strong>
                                            </span>
                                            <span>{new Date(item.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                                        </div>
                                    </div>
                                )
                            })}
                        </div>
                    )}

                    {/* Phân trang */}
                    {queueData && queueData.totalCount > 0 && (
                        <div style={{ display: 'flex', justifyContent: 'center', paddingTop: 8 }}>
                            <Pagination
                                size="small"
                                current={filter.pageNumber}
                                pageSize={filter.pageSize}
                                total={queueData.totalCount}
                                showSizeChanger={false}
                                onChange={(page) => setFilter((prev) => ({ ...prev, pageNumber: page }))}
                            />
                        </div>
                    )}
                </div>

                {/* CỘT PHẢI: CHI TIẾT PHIẾU & WORKSPACE ĐIỀU PHỐI */}
                <div className="queue-detail-card">
                    {detailLoading && !requestDetail && (
                        <Skeleton active paragraph={{ rows: 10 }} />
                    )}

                    {detailError && (
                        <Alert
                            message="Lỗi tải chi tiết phiếu"
                            description={detailError}
                            type="error"
                            showIcon
                        />
                    )}

                    {!selectedRequestId && !detailLoading && (
                        <Empty
                            description="Vui lòng chọn một phiếu yêu cầu từ danh sách bên trái để thẩm định và điều phối."
                            image={Empty.PRESENTED_IMAGE_SIMPLE}
                        />
                    )}

                    {requestDetail && (
                        <>
                            {/* Header chi tiết phiếu */}
                            <div className="detail-header-panel">
                                <div className="detail-dept-tag">
                                    {requestDetail.departmentName || 'Khoa lâm sàng'}
                                </div>
                                <div className="detail-code-row">
                                    <span className="detail-request-code">{requestDetail.requestCode}</span>
                                    {renderUrgencyTag(requestDetail.urgencyLevel)}
                                    <Tag color={requestDetail.status === 'READY' ? 'green' : requestDetail.status === 'REJECTED' ? 'red' : 'blue'}>
                                        {requestDetail.status}
                                    </Tag>
                                </div>
                                <div className="detail-patient-summary">
                                    {requestDetail.patientName} · Nhóm máu {requestDetail.patientBloodType}
                                    {requestDetail.patientRh === 'POSITIVE' ? '+' : '-'} · Tổng {requestDetail.totalRequestedUnits} túi
                                </div>
                            </div>

                            {/* Sub-tabs: Thẩm định vs Chọn & giữ chỗ */}
                            <div className="workspace-tabs">
                                <button
                                    type="button"
                                    className={`workspace-tab-btn ${activeTab === 'review' ? 'is-active' : ''}`}
                                    onClick={() => setActiveTab('review')}
                                >
                                    Thẩm định phiếu yêu cầu
                                </button>
                                <Can do={PERMISSIONS.BLOOD_REQUEST_APPROVE}>
                                    <button
                                        type="button"
                                        className={`workspace-tab-btn ${activeTab === 'allocation' ? 'is-active' : ''}`}
                                        disabled={!canEnterAllocationTab}
                                        title={
                                            !isPendingRequest
                                                ? 'Chỉ điều phối được phiếu ở trạng thái PENDING'
                                                : !isCurrentRequestReviewed
                                                  ? 'Vui lòng thẩm định thông tin và nhấn "Tiến hành điều phối" trước khi chọn túi máu'
                                                  : undefined
                                        }
                                        onClick={() => {
                                            if (canEnterAllocationTab) {
                                                setActiveTab('allocation')
                                            }
                                        }}
                                    >
                                        Chọn & giữ chỗ túi máu
                                    </button>
                                </Can>
                            </div>

                            {/* TAB 1: THẨM ĐỊNH PHIẾU YÊU CẦU */}
                            {activeTab === 'review' && (
                                <div className="review-panel">
                                    {/* Grid thông tin người bệnh & chỉ định */}
                                    <div className="review-grid">
                                        <div className="review-item">
                                            <span className="review-label">Bệnh nhân</span>
                                            <span className="review-value">
                                                {requestDetail.patientName} ({requestDetail.patientCode})
                                            </span>
                                        </div>
                                        <div className="review-item">
                                            <span className="review-label">Khoa yêu cầu</span>
                                            <span className="review-value">{requestDetail.departmentName || 'Chưa cập nhật'}</span>
                                        </div>
                                        <div className="review-item">
                                            <span className="review-label">Bác sĩ chỉ định</span>
                                            <span className="review-value">{requestDetail.doctorName || 'Chưa cập nhật'}</span>
                                        </div>
                                        <div className="review-item">
                                            <span className="review-label">Thời gian tạo phiếu</span>
                                            <span className="review-value">
                                                {new Date(requestDetail.createdAt).toLocaleString('vi-VN')}
                                            </span>
                                        </div>
                                        <div className="review-item" style={{ gridColumn: 'span 2' }}>
                                            <span className="review-label">Chẩn đoán lâm sàng</span>
                                            <span className="review-value">{requestDetail.diagnosis || 'Không có ghi chú'}</span>
                                        </div>
                                    </div>

                                    {/* Bảng danh sách chế phẩm yêu cầu */}
                                    <div>
                                        <div className="section-subheading">Danh sách chế phẩm yêu cầu</div>
                                        <Table
                                            dataSource={requestDetail.items}
                                            rowKey="requestItemId"
                                            pagination={false}
                                            size="small"
                                            columns={[
                                                {
                                                    title: 'Tên chế phẩm',
                                                    dataIndex: 'componentName',
                                                    key: 'componentName',
                                                    render: (val, row) => (
                                                        <div>
                                                            <strong>{val}</strong>
                                                            <div style={{ fontSize: 11, color: '#64748b' }}>
                                                                Mã: {row.componentCode}
                                                            </div>
                                                        </div>
                                                    ),
                                                },
                                                {
                                                    title: 'Yêu cầu',
                                                    dataIndex: 'requestedUnits',
                                                    key: 'requestedUnits',
                                                    align: 'center',
                                                    render: (val) => `${val} túi`,
                                                },
                                                {
                                                    title: 'Đã giữ chỗ',
                                                    dataIndex: 'allocatedUnits',
                                                    key: 'allocatedUnits',
                                                    align: 'center',
                                                    render: (val) => (
                                                        <Tag color={val > 0 ? 'green' : 'default'}>{val} túi</Tag>
                                                    ),
                                                },
                                                {
                                                    title: 'Còn thiếu',
                                                    key: 'shortage',
                                                    align: 'center',
                                                    render: (_, row) => {
                                                        const diff = row.requestedUnits - row.allocatedUnits
                                                        return diff > 0 ? (
                                                            <Tag color="red">{diff} túi</Tag>
                                                        ) : (
                                                            <Tag color="green">Đã đủ</Tag>
                                                        )
                                                    },
                                                },
                                            ]}
                                        />
                                    </div>

                                    {/* Bảng các túi máu đã giữ chỗ trước đó (nếu có) */}
                                    {requestDetail.allocations && requestDetail.allocations.length > 0 && (
                                        <div>
                                            <div className="section-subheading">Túi máu đã được giữ chỗ / cấp phát</div>
                                            <Table
                                                dataSource={requestDetail.allocations}
                                                rowKey="allocationId"
                                                pagination={false}
                                                size="small"
                                                columns={[
                                                    {
                                                        title: 'Mã túi (Barcode)',
                                                        dataIndex: 'barcode',
                                                        key: 'barcode',
                                                        render: (val) => <code style={{ fontWeight: 700 }}>{val}</code>,
                                                    },
                                                    {
                                                        title: 'Chế phẩm',
                                                        dataIndex: 'componentName',
                                                        key: 'componentName',
                                                    },
                                                    {
                                                        title: 'Thể tích',
                                                        dataIndex: 'volumeMl',
                                                        key: 'volumeMl',
                                                        render: (val) => `${val} ml`,
                                                    },
                                                    {
                                                        title: 'Trạng thái phân bổ',
                                                        dataIndex: 'status',
                                                        key: 'status',
                                                        render: (val) => (
                                                            <Tag color={val === 'RESERVED' ? 'blue' : val === 'ISSUED' ? 'cyan' : 'green'}>
                                                                {val}
                                                            </Tag>
                                                        ),
                                                    },
                                                    {
                                                        title: 'Người giữ chỗ',
                                                        dataIndex: 'reservedByName',
                                                        key: 'reservedByName',
                                                    },
                                                ]}
                                            />
                                        </div>
                                    )}

                                    {!canApprove ? (
                                        <Alert
                                            type="warning"
                                            showIcon
                                            message="Chế độ chỉ xem"
                                            description="Tài khoản hiện tại không có quyền điều phối hoặc giữ chỗ túi máu (BLOOD_REQUEST_APPROVE)."
                                        />
                                    ) : !isPendingRequest ? (
                                        <Alert
                                            type="info"
                                            showIcon
                                            message={`Phiếu đang ở trạng thái ${requestDetail.status}`}
                                            description="Chỉ các phiếu đang ở trạng thái PENDING mới có thể thẩm định từ chối hoặc tiếp tục chọn giữ chỗ túi máu."
                                        />
                                    ) : (
                                        <Alert
                                            type="info"
                                            showIcon
                                            message="Lưu ý quy trình thẩm định"
                                            description="Nút 'Tiến hành điều phối' xác nhận hoàn tất rà soát thông tin để mở bước Chọn & giữ chỗ túi máu; trạng thái phiếu trên hệ thống vẫn giữ là PENDING cho đến khi xác nhận giữ chỗ đủ túi."
                                        />
                                    )}

                                    {/* Footer thao tác: Từ chối / Chuyển sang chọn túi */}
                                    <Can do={PERMISSIONS.BLOOD_REQUEST_APPROVE}>
                                        <div className="review-actions-bar">
                                            <Button
                                                danger
                                                icon={<CloseCircleOutlined />}
                                                onClick={() => setShowRejectModal(true)}
                                                disabled={!isPendingRequest}
                                            >
                                                Từ chối yêu cầu
                                            </Button>

                                            <Button
                                                type="primary"
                                                style={{ backgroundColor: '#1A7534', borderColor: '#1A7534' }}
                                                icon={<RightOutlined />}
                                                disabled={!isPendingRequest}
                                                onClick={() => {
                                                    if (!requestDetail || !isPendingRequest) return
                                                    setReviewedRequestIds((prev) => ({
                                                        ...prev,
                                                        [requestDetail.requestId]: true,
                                                    }))
                                                    setActiveTab('allocation')
                                                }}
                                            >
                                                Tiến hành điều phối
                                            </Button>
                                        </div>
                                    </Can>
                                </div>
                            )}

                            {/* TAB 2: CHỌN & GIỮ CHỖ TÚI MÁU */}
                            {activeTab === 'allocation' && canEnterAllocationTab && (
                                <div className="allocation-panel">
                                    {/* Bộ chọn dòng y lệnh (nếu phiếu có nhiều dòng chế phẩm) */}
                                    {requestDetail.items && requestDetail.items.length > 1 && (
                                        <div>
                                            <div style={{ fontSize: 13, fontWeight: 600, color: '#334155', marginBottom: 6 }}>
                                                Chọn dòng chế phẩm cần điều phối:
                                            </div>
                                            <div className="item-selector-bar">
                                                {requestDetail.items.map((it) => {
                                                    const shortage = it.requestedUnits - it.allocatedUnits
                                                    const isActive = activeItemId === it.requestItemId
                                                    return (
                                                        <button
                                                            key={it.requestItemId}
                                                            type="button"
                                                            className={`item-pill-btn ${isActive ? 'is-active' : ''}`}
                                                            onClick={() => setActiveItemId(it.requestItemId)}
                                                        >
                                                            <span>{it.componentName}</span>
                                                            <Tag color={shortage > 0 ? 'red' : 'green'}>
                                                                {shortage > 0 ? `Thiếu ${shortage}/${it.requestedUnits}` : 'Đã đủ'}
                                                            </Tag>
                                                        </button>
                                                    )
                                                })}
                                            </div>
                                        </div>
                                    )}

                                    {/* Thanh thông tin dòng y lệnh & Toggle tương thích */}
                                    <div className="allocation-controls-row">
                                        <div>
                                            <span style={{ fontSize: 13, color: '#475569' }}>
                                                Chế phẩm:{' '}
                                                <strong style={{ color: '#0f172a' }}>
                                                    {currentItem?.componentName}
                                                </strong>{' '}
                                                · Yêu cầu: <strong>{currentItem?.requestedUnits}</strong> túi · Đã
                                                giữ: <strong>{currentItem?.allocatedUnits}</strong> túi · Còn thiếu:{' '}
                                                <strong style={{ color: remainingUnits > 0 ? '#dc2626' : '#16a34a' }}>
                                                    {remainingUnits} túi
                                                </strong>
                                            </span>
                                        </div>

                                        <div className="compatibility-toggle">
                                            <span>Bao gồm túi tương thích an toàn:</span>
                                            <Switch
                                                checked={includeCompatible}
                                                onChange={(val) => setIncludeCompatible(val)}
                                                size="small"
                                            />
                                        </div>
                                    </div>

                                    {/* Thông báo nếu dòng y lệnh đã đủ số lượng */}
                                    {remainingUnits <= 0 && (
                                        <Alert
                                            type="success"
                                            showIcon
                                            message="Dòng y lệnh này đã được giữ chỗ đủ số lượng."
                                            description="Không cần giữ chỗ thêm. Bạn có thể chọn dòng chế phẩm khác nếu phiếu còn thiếu."
                                        />
                                    )}

                                    {/* Lỗi khi tải túi máu */}
                                    {bagsError && (
                                        <Alert
                                            type="error"
                                            showIcon
                                            message="Không thể tải danh sách túi máu khả dụng"
                                            description={bagsError}
                                        />
                                    )}

                                    {/* Danh sách túi máu khả dụng */}
                                    {bagsLoading && <Skeleton active paragraph={{ rows: 4 }} />}

                                    {!bagsLoading && !bagsError && availableBags.length === 0 && remainingUnits > 0 && (
                                        <Empty
                                            description="Không có túi máu khả dụng nào trong kho khớp với điều kiện hiện tại."
                                            image={Empty.PRESENTED_IMAGE_SIMPLE}
                                        >
                                            {!includeCompatible && (
                                                <Button
                                                    size="small"
                                                    onClick={() => setIncludeCompatible(true)}
                                                >
                                                    Tìm cả túi hòa hợp an toàn
                                                </Button>
                                            )}
                                        </Empty>
                                    )}

                                    {!bagsLoading && availableBags.length > 0 && remainingUnits > 0 && (
                                        <div className="available-bags-list">
                                            {availableBags.map((bag) => {
                                                const isChecked = selectedBagIds.includes(bag.bagId)
                                                const nearExpiry = isNearExpiry(bag.expiresAt)

                                                return (
                                                    <div
                                                        key={bag.bagId}
                                                        className={`bag-card-row ${isChecked ? 'is-checked' : ''}`}
                                                        onClick={() => handleToggleBag(bag.bagId)}
                                                    >
                                                        <div className="bag-card-left">
                                                            <Checkbox
                                                                checked={isChecked}
                                                                onChange={() => handleToggleBag(bag.bagId)}
                                                                onClick={(e) => e.stopPropagation()}
                                                            />
                                                            <div>
                                                                <div className="bag-barcode">{bag.barcode}</div>
                                                                <div className="bag-meta-info">
                                                                    <span>{bag.volumeMl} ml</span>
                                                                    <span>·</span>
                                                                    <span className={nearExpiry ? 'is-expiring' : ''}>
                                                                        HSD: {new Date(bag.expiresAt).toLocaleDateString('vi-VN')}
                                                                    </span>
                                                                    {bag.locationName && (
                                                                        <>
                                                                            <span>·</span>
                                                                            <span>Vị trí: {bag.locationName}</span>
                                                                        </>
                                                                    )}
                                                                </div>
                                                            </div>
                                                        </div>

                                                        <div className="bag-card-right">
                                                            <Tag color="red" style={{ fontWeight: 700 }}>
                                                                {bag.bloodType}
                                                            </Tag>
                                                            {bag.isExactMatch ? (
                                                                <Tag color="blue">Trùng nhóm</Tag>
                                                            ) : (
                                                                <Tag color="purple">Hòa hợp an toàn</Tag>
                                                            )}
                                                            <Tag color="success">Khả dụng</Tag>
                                                        </div>
                                                    </div>
                                                )
                                            })}
                                        </div>
                                    )}

                                    {/* Footer giữ chỗ */}
                                    <div className="allocation-footer">
                                        <span className="allocation-counter-text">
                                            Đã chọn{' '}
                                            <span style={{ color: varColor(selectedBagIds.length, remainingUnits) }}>
                                                {selectedBagIds.length}
                                            </span>{' '}
                                            / {remainingUnits} túi còn thiếu
                                        </span>

                                        <Button
                                            type="primary"
                                            style={{ backgroundColor: '#1A7534', borderColor: '#1A7534' }}
                                            icon={<CheckCircleOutlined />}
                                            disabled={
                                                !canApprove ||
                                                !isPendingRequest ||
                                                selectedBagIds.length === 0 ||
                                                remainingUnits <= 0
                                            }
                                            loading={allocateSubmitting}
                                            onClick={handleConfirmAllocate}
                                        >
                                            Xác nhận giữ chỗ ({selectedBagIds.length})
                                        </Button>
                                    </div>
                                </div>
                            )}
                        </>
                    )}
                </div>
            </div>

            {/* Modal Từ chối phiếu */}
            <RejectRequestModal
                visible={showRejectModal}
                request={requestDetail}
                onClose={() => setShowRejectModal(false)}
                onSuccess={() => {
                    setActiveTab('review')
                }}
            />

            {/* Modal Báo kết quả giữ chỗ thành công */}
            <AllocateSuccessModal
                visible={showSuccessModal}
                result={allocateResult}
                onClose={() => {
                    setShowSuccessModal(false)
                    if (allocateResult?.requestStatus !== 'PENDING') {
                        setActiveTab('review')
                    }
                }}
            />
        </div>
    )
}

function varColor(selected: number, remaining: number): string {
    if (selected === 0) return '#64748b'
    if (selected === remaining) return '#16a34a'
    return '#ea580c'
}
