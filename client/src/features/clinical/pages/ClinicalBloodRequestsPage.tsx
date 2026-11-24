import { useState, useMemo, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
    Alert,
    Table,
    Input,
    Button,
    Select,
    Form,
    InputNumber,
    Tag,
    message,
    Dropdown,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
    SearchOutlined,
    FilterOutlined,
    ScanOutlined,
    RightOutlined,
    SyncOutlined,
    CloseCircleOutlined,
} from '@ant-design/icons'
import {
    BloodGroupTag,
    PageContainer,
    PageHeader,
    SectionCard,
    UrgencyTag,
} from '@/shared/components'
import {
    useClinicalRequests,
    useCreateBloodRequest,
    useUpdateDraftRequest,
    useSubmitDraftRequest,
    usePendingHandoverSessions,
    useComponentTypes,
} from '../hooks'
import { RequestStatusStepper } from '../components/RequestStatusStepper'
import { SelectPatientHisModal } from '../components/SelectPatientHisModal'
import { ConfirmSendRequestModal, type ConfirmSendRequestData } from '../components/ConfirmSendRequestModal'
import { BloodRequestDetailDrawer } from '../components/BloodRequestDetailDrawer'
import { ClinicalReturnCreatePanel } from '../components/ClinicalReturnCreatePanel'
import { getUrgencyMeta, getStatusMeta, formatUnits } from '../format'
import { formatBloodGroup, parseBloodGroup } from '@/shared/formatters/medical'
import { useQueryClient } from '@tanstack/react-query'
import { useCurrentUser } from '@/features/auth/useCurrentUser'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { getInitials } from '@/shared/utils/string'
import { BLOOD_GROUPS_DISPLAY } from '@/shared/constants/medical'
import type {
    BloodRequestDetailDto,
    ClinicalBloodRequestDto,
    HisPatientDto,
    UpdateBloodRequestDto,
    UrgencyLevel,
} from '../types'
import './ClinicalBloodRequestsPage.css'

type ConfirmSubmitContext =
    | { mode: 'create' }
    | { mode: 'edit-and-submit'; draftId: number; requestCode: string }
    | { mode: 'submit-draft'; draftId: number; requestCode: string }

export default function ClinicalBloodRequestsPage() {
    const navigate = useNavigate()
    const [searchParams] = useSearchParams()
    const queryClient = useQueryClient()
    const { displayName, user } = useCurrentUser()
    const { can } = usePermission()

    const canViewRequests = can(PERMISSIONS.BLOOD_REQUEST_VIEW)
    const canEditRequests = can(PERMISSIONS.BLOOD_REQUEST_EDIT)

    const initialTab = searchParams.get('tab') === 'returns'
        ? 'returns'
        : searchParams.get('tab') === 'create'
          ? 'create'
          : 'list'

    // Tab active: 'list' | 'create' | 'returns' (Nhận bàn giao điều hướng sang màn hình riêng /clinical/handovers)
    const [activeTab, setActiveTab] = useState<'list' | 'create' | 'returns'>(initialTab)

    // Filter state cho tab Danh sách
    const [searchKeyword, setSearchKeyword] = useState('')
    const [urgencyFilter, setUrgencyFilter] = useState<string | undefined>()
    const [statusFilter, setStatusFilter] = useState<string | undefined>()
    const [filterDropdownOpen, setFilterDropdownOpen] = useState(false)
    const [pageNumber, setPageNumber] = useState(1)
    const pageSize = 10

    // Query danh sách phiếu (chỉ gọi lần đầu khi vào tab Danh sách trong phiên)
    const {
        data: requestPage,
        isLoading: isRequestsLoading,
        isFetching: isRequestsFetching,
        refetch: refetchRequests,
    } = useClinicalRequests(
        {
            search: searchKeyword.trim() || undefined,
            urgencyLevel: urgencyFilter,
            status: statusFilter,
            pageNumber,
            pageSize,
        },
        { enabled: canViewRequests && activeTab === 'list' },
    )

    const {
        data: pendingSessions,
        refetch: refetchPendingSessions,
    } = usePendingHandoverSessions(undefined, { enabled: canViewRequests, refetchInterval: 15000 })
    const pendingCount = pendingSessions?.length || 0

    // Component types
    const { data: componentTypes = [] } = useComponentTypes()

    // Form tạo mới / chỉnh sửa phiếu (W3.1)
    const [form] = Form.useForm()
    const [selectedPatient, setSelectedPatient] = useState<HisPatientDto | null>(null)
    const [editingDraft, setEditingDraft] = useState<BloodRequestDetailDto | null>(null)
    const [draftToLoad, setDraftToLoad] = useState<BloodRequestDetailDto | null>(null)
    const [shouldResetCreateForm, setShouldResetCreateForm] = useState(false)

    const createMutation = useCreateBloodRequest()
    const updateDraftMutation = useUpdateDraftRequest()
    const submitDraftMutation = useSubmitDraftRequest()
    const isSubmittingRequest =
        createMutation.isPending || updateDraftMutation.isPending || submitDraftMutation.isPending

    const resetCreateFormNow = () => {
        form.resetFields()
        form.setFieldsValue({
            urgencyLevel: 'URGENT',
            requestedUnits: 2,
            componentTypeId: componentTypes[0]?.componentTypeId || 2,
        })
        setSelectedPatient(null)
        setEditingDraft(null)
    }

    useEffect(() => {
        if (activeTab !== 'create') return

        if (shouldResetCreateForm) {
            form.resetFields()
            form.setFieldsValue({
                urgencyLevel: 'URGENT',
                requestedUnits: 2,
                componentTypeId: componentTypes[0]?.componentTypeId || 2,
            })
            setSelectedPatient(null)
            setEditingDraft(null)
            setShouldResetCreateForm(false)
            return
        }

        if (draftToLoad) {
            const bloodGroupStr = formatBloodGroup(draftToLoad.patientBloodType, draftToLoad.patientRh)
            const item = draftToLoad.items[0]
            const patientObj: HisPatientDto = {
                patientCode: draftToLoad.patientCode,
                patientName: draftToLoad.patientName,
                age: 0,
                gender: 'Nam',
                room: '',
                bed: '',
                diagnosis: draftToLoad.diagnosis || '',
                bloodType: draftToLoad.patientBloodType,
                rhFactor: draftToLoad.patientRh,
                bloodGroupDisplay: bloodGroupStr,
                initials: getInitials(draftToLoad.patientName),
            }
            setSelectedPatient(patientObj)
            form.setFieldsValue({
                patientDisplay: `${draftToLoad.patientName} - ${draftToLoad.patientCode}`,
                bloodGroup: bloodGroupStr,
                componentTypeId: item?.componentTypeId || (componentTypes[0]?.componentTypeId || 2),
                requestedUnits: item?.requestedUnits || 2,
                urgencyLevel: draftToLoad.urgencyLevel,
                diagnosis: draftToLoad.diagnosis || '',
            })
            setDraftToLoad(null)
            return
        }

        if (componentTypes.length > 0 && !form.getFieldValue('componentTypeId')) {
            form.setFieldsValue({ componentTypeId: componentTypes[0].componentTypeId })
        }
    }, [activeTab, shouldResetCreateForm, draftToLoad, componentTypes, form])

    // Modal state
    const [isHisModalOpen, setIsHisModalOpen] = useState(false)
    const [isConfirmModalOpen, setIsConfirmModalOpen] = useState(false)
    const [confirmData, setConfirmData] = useState<ConfirmSendRequestData | null>(null)
    const [confirmContext, setConfirmContext] = useState<ConfirmSubmitContext | null>(null)

    // Drawer state
    const [selectedRequestId, setSelectedRequestId] = useState<number | null>(null)
    const [isDrawerOpen, setIsDrawerOpen] = useState(false)

    // Xử lý khi chọn bệnh nhân từ popup HIS
    const handleSelectHisPatient = (patient: HisPatientDto) => {
        setSelectedPatient(patient)
        const displayVal = `${patient.patientName} - ${patient.patientCode}`
        form.setFieldsValue({
            patientDisplay: displayVal,
            bloodGroup: patient.bloodGroupDisplay,
            diagnosis: patient.diagnosis,
        })
    }

    const buildDraftUpdateDto = (
        values: any,
        patient: HisPatientDto,
        currentDraft: BloodRequestDetailDto,
    ): UpdateBloodRequestDto => {
        const { bloodType, rhFactor } = parseBloodGroup(values.bloodGroup)
        const primaryItem = {
            componentTypeId: values.componentTypeId,
            requestedUnits: values.requestedUnits,
        }
        const extraItems = currentDraft.items
            .slice(1)
            .filter((i) => i.componentTypeId !== values.componentTypeId)
            .map((i) => ({
                componentTypeId: i.componentTypeId,
                requestedUnits: i.requestedUnits,
            }))

        return {
            patientCode: patient.patientCode,
            patientName: patient.patientName,
            patientBloodType: bloodType,
            patientRh: rhFactor,
            urgencyLevel: values.urgencyLevel,
            diagnosis: values.diagnosis,
            items: [primaryItem, ...extraItems],
        }
    }

    // Xử lý lưu nháp phiếu yêu cầu (tạo mới hoặc cập nhật phiếu DRAFT gốc)
    const handleSaveDraft = async () => {
        if (isSubmittingRequest) return
        if (!canEditRequests) {
            message.error('Bạn không có quyền lập hoặc chỉnh sửa phiếu yêu cầu máu.')
            return
        }

        try {
            const values = await form.validateFields()
            if (!selectedPatient) {
                message.warning('Vui lòng chọn người bệnh từ HIS trước khi lưu nháp.')
                return
            }

            if (editingDraft) {
                const updated = await updateDraftMutation.mutateAsync({
                    id: editingDraft.requestId,
                    dto: buildDraftUpdateDto(values, selectedPatient, editingDraft),
                })
                message.success(`Đã cập nhật phiếu nháp ${updated.requestCode}`)
            } else {
                const { bloodType, rhFactor } = parseBloodGroup(values.bloodGroup)
                await createMutation.mutateAsync({
                    patientCode: selectedPatient.patientCode,
                    patientName: selectedPatient.patientName,
                    patientBloodType: bloodType,
                    patientRh: rhFactor,
                    urgencyLevel: values.urgencyLevel,
                    diagnosis: values.diagnosis,
                    isDraft: true,
                    items: [
                        {
                            componentTypeId: values.componentTypeId,
                            requestedUnits: values.requestedUnits,
                        },
                    ],
                })
            }

            resetCreateFormNow()
            setActiveTab('list')
        } catch {
            // Validation or mutation error
        }
    }

    // Xử lý xem lại trước khi gửi (tạo mới hoặc gửi phiếu DRAFT đang chỉnh sửa)
    const handleReviewAndSend = async () => {
        if (isSubmittingRequest) return
        if (!canEditRequests) {
            message.error('Bạn không có quyền lập hoặc gửi phiếu yêu cầu máu.')
            return
        }

        try {
            const values = await form.validateFields()
            if (!selectedPatient) {
                message.warning('Vui lòng chọn người bệnh từ HIS.')
                return
            }

            const comp = componentTypes.find((c) => c.componentTypeId === values.componentTypeId)

            setConfirmContext(
                editingDraft
                    ? {
                          mode: 'edit-and-submit',
                          draftId: editingDraft.requestId,
                          requestCode: editingDraft.requestCode,
                      }
                    : { mode: 'create' },
            )
            setConfirmData({
                requestCode: editingDraft?.requestCode,
                patientName: selectedPatient.patientName,
                patientCode: selectedPatient.patientCode,
                bloodGroup: values.bloodGroup,
                componentName: comp?.typeName || 'Chế phẩm máu',
                units: values.requestedUnits,
                urgencyLevel: values.urgencyLevel,
                doctorName: editingDraft?.doctorName || displayName || 'Bác sĩ điều trị',
                departmentName:
                    editingDraft?.departmentName || (user as any)?.departmentName || 'Khoa Điều trị',
            })
            setIsConfirmModalOpen(true)
        } catch {
            // Validation error
        }
    }

    // Xác nhận gửi chính thức phiếu yêu cầu
    const handleConfirmSubmit = async () => {
        if (isSubmittingRequest || !confirmContext) return

        try {
            if (confirmContext.mode === 'submit-draft') {
                await submitDraftMutation.mutateAsync(confirmContext.draftId)
                if (editingDraft?.requestId === confirmContext.draftId) {
                    setEditingDraft(null)
                    setSelectedPatient(null)
                    setShouldResetCreateForm(true)
                }
                setIsConfirmModalOpen(false)
                setConfirmContext(null)
                setConfirmData(null)
                return
            }

            if (!selectedPatient) return
            const values = form.getFieldsValue()

            if (confirmContext.mode === 'edit-and-submit') {
                if (!editingDraft || editingDraft.requestId !== confirmContext.draftId) return
                const updated = await updateDraftMutation.mutateAsync({
                    id: confirmContext.draftId,
                    dto: buildDraftUpdateDto(values, selectedPatient, editingDraft),
                })
                setEditingDraft(updated)
                await submitDraftMutation.mutateAsync(confirmContext.draftId)
            } else {
                const { bloodType, rhFactor } = parseBloodGroup(values.bloodGroup)
                await createMutation.mutateAsync({
                    patientCode: selectedPatient.patientCode,
                    patientName: selectedPatient.patientName,
                    patientBloodType: bloodType,
                    patientRh: rhFactor,
                    urgencyLevel: values.urgencyLevel,
                    diagnosis: values.diagnosis,
                    isDraft: false,
                    items: [
                        {
                            componentTypeId: values.componentTypeId,
                            requestedUnits: values.requestedUnits,
                        },
                    ],
                })
            }

            setIsConfirmModalOpen(false)
            setConfirmContext(null)
            setConfirmData(null)
            resetCreateFormNow()
            setActiveTab('list')
        } catch {
            // Handled in mutation onError
        }
    }

    // Xử lý khi chọn "Tiếp tục chỉnh sửa" từ chi tiết phiếu DRAFT
    const handleEditDraft = (draft: BloodRequestDetailDto) => {
        if (draft.status !== 'DRAFT') return
        setIsDrawerOpen(false)
        setSelectedRequestId(null)
        setShouldResetCreateForm(false)
        setEditingDraft(draft)
        setDraftToLoad(draft)
        setActiveTab('create')
    }

    // Hủy chỉnh sửa phiếu nháp để quay về chế độ lập phiếu mới
    const handleCancelEditDraft = () => {
        resetCreateFormNow()
    }

    // Xử lý khi chọn "Gửi phiếu ngay" từ chi tiết phiếu DRAFT (không đụng tới form đang unmount)
    const handleSubmitDraft = (draft: BloodRequestDetailDto) => {
        if (isSubmittingRequest || draft.status !== 'DRAFT') return
        setIsDrawerOpen(false)
        setSelectedRequestId(null)
        const bloodGroupStr = formatBloodGroup(draft.patientBloodType, draft.patientRh)
        const item = draft.items[0]

        setConfirmContext({
            mode: 'submit-draft',
            draftId: draft.requestId,
            requestCode: draft.requestCode,
        })
        setConfirmData({
            requestCode: draft.requestCode,
            patientName: draft.patientName,
            patientCode: draft.patientCode,
            bloodGroup: bloodGroupStr,
            componentName: item?.componentName || 'Chế phẩm máu',
            units: draft.totalRequestedUnits || item?.requestedUnits || 1,
            urgencyLevel: draft.urgencyLevel,
            doctorName: draft.doctorName || displayName || 'Bác sĩ điều trị',
            departmentName: draft.departmentName || (user as any)?.departmentName || 'Khoa Điều trị',
        })
        setIsConfirmModalOpen(true)
    }

    // Reset toàn bộ bộ lọc
    const handleResetAllFilters = () => {
        setSearchKeyword('')
        setUrgencyFilter(undefined)
        setStatusFilter(undefined)
        setPageNumber(1)
        setFilterDropdownOpen(false)
    }

    const hasActiveFilter = Boolean(urgencyFilter || statusFilter || searchKeyword.trim())

    // Bảng danh sách phiếu yêu cầu (Tab Danh sách phiếu)
    const requestColumns: ColumnsType<ClinicalBloodRequestDto> = useMemo(
        () => [
            {
                title: 'MÃ PHIẾU / KHOA',
                key: 'requestCode',
                width: 170,
                render: (_, record) => (
                    <div className="req-col-code">
                        <div className="req-code-text">{record.requestCode}</div>
                        <div className="req-dept-text">Khoa Điều trị</div>
                    </div>
                ),
            },
            {
                title: 'NGƯỜI BỆNH',
                dataIndex: 'patientName',
                key: 'patientName',
                render: (name, record) => (
                    <div>
                        <div className="req-patient-name">{name}</div>
                        <div className="req-patient-code">{record.patientCode}</div>
                    </div>
                ),
            },
            {
                title: 'NHÓM MÁU',
                key: 'bloodType',
                align: 'center',
                width: 110,
                render: (_, record) => {
                    const groupStr = formatBloodGroup(record.patientBloodType, record.patientRh)
                    return <BloodGroupTag value={groupStr} />
                },
            },
            {
                title: 'SỐ LƯỢNG',
                dataIndex: 'totalRequestedUnits',
                key: 'units',
                width: 100,
                render: (units) => <span className="req-units-text">{formatUnits(units)}</span>,
            },
            {
                title: 'MỨC ĐỘ',
                dataIndex: 'urgencyLevel',
                key: 'urgency',
                width: 120,
                render: (level: UrgencyLevel) => <UrgencyTag level={level} />,
            },
            {
                title: 'TIẾN ĐỘ XỬ LÝ',
                key: 'progress',
                render: (_, record) => (
                    <RequestStatusStepper currentStep={record.currentStep} status={record.status} />
                ),
            },
            {
                title: '',
                key: 'actions',
                width: 40,
                align: 'center',
                render: (_, record) => (
                    <RightOutlined
                        className="req-arrow-icon"
                        onClick={(e) => {
                            e.stopPropagation()
                            setSelectedRequestId(record.requestId)
                            setIsDrawerOpen(true)
                        }}
                    />
                ),
            },
        ],
        [],
    )

    return (
        <PageContainer>
            <PageHeader
                breadcrumb={[
                    { title: 'Yêu cầu máu' },
                    { title: 'Lâm sàng' },
                ]}
                title="Yêu cầu máu lâm sàng"
                subtitle="Theo dõi tiến độ xử lý và thực hiện bàn giao máu an toàn hai chiều."
                extra={
                    <button
                        type="button"
                        className="btn-sync-his"
                        onClick={() => {
                            queryClient.invalidateQueries({ queryKey: ['clinical'] })
                            if (canViewRequests) {
                                if (activeTab === 'list') refetchRequests()
                                void refetchPendingSessions()
                            }
                            message.success('Đã làm mới dữ liệu đồng bộ với hệ thống!')
                        }}
                    >
                        <span className="sync-dot" />
                        <SyncOutlined />
                        <span>Đồng bộ HIS/LIS</span>
                    </button>
                }
                bottom={
                    <div className="clinical-tabbar">
                        <button
                            type="button"
                            className={`clinical-tab-btn ${activeTab === 'list' ? 'is-active' : ''}`}
                            onClick={() => setActiveTab('list')}
                        >
                            <span>Danh sách phiếu</span>
                        </button>
                        <button
                            type="button"
                            className={`clinical-tab-btn ${activeTab === 'create' ? 'is-active' : ''}`}
                            onClick={() => setActiveTab('create')}
                        >
                            <span>Lập phiếu yêu cầu mới</span>
                        </button>
                        <button
                            type="button"
                            className="clinical-tab-btn"
                            onClick={() => navigate('/clinical/handovers')}
                        >
                            <span>Nhận bàn giao</span>
                            {pendingCount > 0 && <span className="tab-badge">{pendingCount}</span>}
                        </button>
                        <button
                            type="button"
                            className={`clinical-tab-btn ${activeTab === 'returns' ? 'is-active' : ''}`}
                            onClick={() => setActiveTab('returns')}
                        >
                            <span>Lập phiếu hoàn trả</span>
                        </button>
                    </div>
                }
            />

            {/* Tab: Danh sách phiếu */}
            {activeTab === 'list' && (
                <SectionCard
                    title="Tiến độ yêu cầu máu"
                    subtitle="Cập nhật trạng thái theo thời gian thực từ kho máu"
                    extra={
                        <div className="clinical-filters">
                            <Input
                                placeholder="Tìm mã phiếu, người bệnh..."
                                prefix={<SearchOutlined style={{ color: 'var(--c-text-subtle)' }} />}
                                value={searchKeyword}
                                onChange={(e) => {
                                    setSearchKeyword(e.target.value)
                                    setPageNumber(1)
                                }}
                                allowClear
                                className="search-input"
                            />
                            <Dropdown
                                open={filterDropdownOpen}
                                onOpenChange={setFilterDropdownOpen}
                                trigger={['click']}
                                dropdownRender={() => (
                                    <div className="filter-dropdown-menu">
                                        <div className="filter-dropdown-menu__head">
                                            <span className="filter-dropdown-menu__title">Bộ lọc tìm kiếm</span>
                                            {(urgencyFilter || statusFilter) && (
                                                <Button
                                                    type="link"
                                                    danger
                                                    size="small"
                                                    style={{ padding: 0, height: 'auto', fontSize: 'var(--text-xs)' }}
                                                    onClick={() => {
                                                        setUrgencyFilter(undefined)
                                                        setStatusFilter(undefined)
                                                        setPageNumber(1)
                                                    }}
                                                >
                                                    Đặt lại
                                                </Button>
                                            )}
                                        </div>

                                        <div className="filter-item-title">Mức độ ưu tiên</div>
                                        <Select
                                            placeholder="Tất cả mức độ"
                                            value={urgencyFilter}
                                            onChange={(val) => {
                                                setUrgencyFilter(val)
                                                setPageNumber(1)
                                            }}
                                            allowClear
                                            style={{ width: '100%', marginBottom: 14 }}
                                            options={[
                                                { label: 'Cấp cứu', value: 'EMERGENCY' },
                                                { label: 'Khẩn', value: 'URGENT' },
                                                { label: 'Kế hoạch', value: 'ROUTINE' },
                                            ]}
                                        />

                                        <div className="filter-item-title">Trạng thái phiếu</div>
                                        <Select
                                            placeholder="Tất cả trạng thái"
                                            value={statusFilter}
                                            onChange={(val) => {
                                                setStatusFilter(val)
                                                setPageNumber(1)
                                            }}
                                            allowClear
                                            style={{ width: '100%' }}
                                            options={[
                                                { label: 'Bản nháp', value: 'DRAFT' },
                                                { label: 'Đã gửi (Chờ duyệt)', value: 'PENDING' },
                                                { label: 'Sẵn sàng bàn giao', value: 'READY' },
                                                { label: 'Đã hoàn thành', value: 'COMPLETED' },
                                                { label: 'Đã từ chối', value: 'REJECTED' },
                                            ]}
                                        />

                                        <div className="filter-dropdown-menu__foot">
                                            <Button
                                                size="small"
                                                onClick={() => {
                                                    setUrgencyFilter(undefined)
                                                    setStatusFilter(undefined)
                                                    setPageNumber(1)
                                                }}
                                            >
                                                Xóa lọc
                                            </Button>
                                            <Button
                                                size="small"
                                                type="primary"
                                                onClick={() => setFilterDropdownOpen(false)}
                                            >
                                                Áp dụng
                                            </Button>
                                        </div>
                                    </div>
                                )}
                            >
                                <Button icon={<FilterOutlined />}>
                                    Bộ lọc {urgencyFilter || statusFilter ? '•' : ''}
                                </Button>
                            </Dropdown>
                        </div>
                    }
                >
                    {/* Thanh hiển thị các tag lọc đang kích hoạt */}
                    {hasActiveFilter && (
                        <div className="active-filters-bar">
                            <span style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)' }}>Đang lọc:</span>
                            {searchKeyword.trim() && (
                                <Tag
                                    closable
                                    onClose={() => {
                                        setSearchKeyword('')
                                        setPageNumber(1)
                                    }}
                                    color="default"
                                >
                                    Từ khóa: &quot;{searchKeyword}&quot;
                                </Tag>
                            )}
                            {urgencyFilter && (
                                <Tag
                                    closable
                                    onClose={() => {
                                        setUrgencyFilter(undefined)
                                        setPageNumber(1)
                                    }}
                                    color="orange"
                                >
                                    Mức độ: {getUrgencyMeta(urgencyFilter as any).label}
                                </Tag>
                            )}
                            {statusFilter && (
                                <Tag
                                    closable
                                    onClose={() => {
                                        setStatusFilter(undefined)
                                        setPageNumber(1)
                                    }}
                                    color="blue"
                                >
                                    Trạng thái: {getStatusMeta(statusFilter as any).label}
                                </Tag>
                            )}
                            <Button
                                type="link"
                                danger
                                size="small"
                                icon={<CloseCircleOutlined />}
                                onClick={handleResetAllFilters}
                                style={{ padding: 0, height: 'auto', fontSize: 'var(--text-xs)' }}
                            >
                                Xóa tất cả bộ lọc
                            </Button>
                        </div>
                    )}

                    <Table
                        columns={requestColumns}
                        dataSource={requestPage?.items || []}
                        rowKey="requestId"
                        loading={isRequestsLoading || isRequestsFetching}
                        pagination={{
                            current: pageNumber,
                            pageSize,
                            total: requestPage?.totalCount || 0,
                            onChange: (p) => setPageNumber(p),
                            showTotal: (total, range) => `Hiển thị ${range[0]}–${range[1]} trong ${total} phiếu yêu cầu`,
                        }}
                        onRow={(record) => ({
                            onClick: () => {
                                setSelectedRequestId(record.requestId)
                                setIsDrawerOpen(true)
                            },
                            className: 'clickable-row',
                        })}
                        className="clinical-request-table"
                    />

                    {/* Nút nổi quét nhận bàn giao góc dưới -> mở Màn hình 3 */}
                    {canEditRequests && (
                        <div className="floating-action-bar">
                            <Button
                                type="primary"
                                icon={<ScanOutlined />}
                                size="large"
                                onClick={() => navigate('/clinical/handovers')}
                            >
                                Quét nhận bàn giao
                            </Button>
                        </div>
                    )}
                </SectionCard>
            )}

            {/* Tab: Lập phiếu yêu cầu mới / Chỉnh sửa phiếu nháp */}
            {activeTab === 'create' && (
                <SectionCard
                    title={
                        editingDraft
                            ? `Chỉnh sửa phiếu nháp (${editingDraft.requestCode})`
                            : 'Lập phiếu yêu cầu máu mới'
                    }
                >
                    {editingDraft && (
                        <Alert
                            type="info"
                            showIcon
                            style={{ marginBottom: 20 }}
                            message={`Đang chỉnh sửa bản nháp ${editingDraft.requestCode}`}
                            description="Mọi thay đổi khi bấm Lưu nháp hoặc Gửi phiếu sẽ cập nhật trực tiếp trên phiếu nháp gốc này, không tạo phiếu mới."
                            action={
                                <Button
                                    size="small"
                                    onClick={handleCancelEditDraft}
                                    disabled={isSubmittingRequest}
                                >
                                    Hủy chỉnh sửa &amp; Tạo mới
                                </Button>
                            }
                        />
                    )}

                    <Form
                        form={form}
                        layout="vertical"
                        className="create-request-form"
                        initialValues={{
                            urgencyLevel: 'URGENT',
                            requestedUnits: 2,
                            componentTypeId: componentTypes[0]?.componentTypeId || 2,
                        }}
                    >
                        {/* Hàng 1: Bệnh nhân + nút Chọn từ HIS */}
                        <Form.Item
                            label={<span className="form-label-bold">Bệnh nhân <span className="req-star">*</span></span>}
                            required
                            style={{ marginBottom: 20 }}
                        >
                            <div className="patient-picker-row">
                                <Form.Item
                                    name="patientDisplay"
                                    noStyle
                                    rules={[{ required: true, message: 'Vui lòng chọn người bệnh từ HIS' }]}
                                >
                                    <Input
                                        readOnly
                                        value={
                                            selectedPatient
                                                ? `${selectedPatient.patientName} - ${selectedPatient.patientCode}`
                                                : form.getFieldValue('patientDisplay') || ''
                                        }
                                        placeholder="Chưa chọn người bệnh — Bấm 'Chọn từ HIS' bên cạnh để chọn dữ liệu HIS/EMR"
                                        className="patient-display-input"
                                    />
                                </Form.Item>
                                <Button
                                    type="default"
                                    onClick={() => setIsHisModalOpen(true)}
                                    disabled={!canEditRequests || isSubmittingRequest}
                                    className="btn-choose-his"
                                >
                                    Chọn từ HIS
                                </Button>
                            </div>
                            {selectedPatient && (
                                <div className="patient-selected-hint">
                                    ✓ Đã chọn: <strong>{selectedPatient.patientName}</strong> ({selectedPatient.patientCode} - {selectedPatient.room} - {selectedPatient.bed})
                                </div>
                            )}
                        </Form.Item>

                        {/* Hàng 2: Nhóm máu & Sản phẩm máu */}
                        <div className="form-two-cols">
                            <Form.Item
                                name="bloodGroup"
                                label={<span className="form-label-bold">Nhóm máu <span className="req-star">*</span></span>}
                                rules={[{ required: true, message: 'Vui lòng chọn nhóm máu' }]}
                            >
                                <Select
                                    placeholder="Chọn nhóm máu (ABO/Rh)"
                                    options={BLOOD_GROUPS_DISPLAY.map((g) => ({ label: g, value: g }))}
                                    size="large"
                                />
                            </Form.Item>

                            <Form.Item
                                name="componentTypeId"
                                label={<span className="form-label-bold">Sản phẩm máu <span className="req-star">*</span></span>}
                                rules={[{ required: true, message: 'Vui lòng chọn loại chế phẩm' }]}
                            >
                                <Select
                                    placeholder="Chọn loại chế phẩm máu"
                                    options={componentTypes.map((c) => ({
                                        label: c.typeName,
                                        value: c.componentTypeId,
                                    }))}
                                    size="large"
                                />
                            </Form.Item>
                        </div>

                        {/* Hàng 3: Số lượng (túi) & Mức độ ưu tiên */}
                        <div className="form-two-cols">
                            <Form.Item
                                name="requestedUnits"
                                label={<span className="form-label-bold">Số lượng (túi) <span className="req-star">*</span></span>}
                                rules={[{ required: true, message: 'Vui lòng nhập số túi' }]}
                            >
                                <InputNumber min={1} max={100} style={{ width: '100%' }} size="large" />
                            </Form.Item>

                            <Form.Item
                                name="urgencyLevel"
                                label={<span className="form-label-bold">Mức độ ưu tiên <span className="req-star">*</span></span>}
                                rules={[{ required: true, message: 'Vui lòng chọn mức độ ưu tiên' }]}
                            >
                                <Select
                                    options={[
                                        { label: 'Cấp cứu (Khẩn cấp nguy hiểm tính mạng)', value: 'EMERGENCY' },
                                        { label: 'Khẩn (Cần cấp trong vòng 1-2 giờ)', value: 'URGENT' },
                                        { label: 'Kế hoạch (Phẫu thuật / điều trị thường quy)', value: 'ROUTINE' },
                                    ]}
                                    size="large"
                                />
                            </Form.Item>
                        </div>

                        {/* Hàng 4: Chỉ định lâm sàng */}
                        <Form.Item
                            name="diagnosis"
                            label={<span className="form-label-bold">Chỉ định lâm sàng</span>}
                        >
                            <Input.TextArea
                                rows={4}
                                maxLength={255}
                                showCount
                                placeholder="Mô tả chỉ định truyền máu và tình trạng lâm sàng của người bệnh..."
                            />
                        </Form.Item>

                        {/* Nút hành động */}
                        <div className="form-actions-row">
                            <Button
                                size="large"
                                onClick={handleSaveDraft}
                                loading={isSubmittingRequest}
                                disabled={!canEditRequests || isSubmittingRequest}
                            >
                                {editingDraft ? 'Lưu thay đổi nháp' : 'Lưu nháp'}
                            </Button>
                            <Button
                                type="primary"
                                size="large"
                                onClick={handleReviewAndSend}
                                loading={isSubmittingRequest}
                                disabled={!canEditRequests || isSubmittingRequest}
                            >
                                Xem lại &amp; Gửi phiếu
                            </Button>
                        </div>
                    </Form>
                </SectionCard>
            )}

            {/* Tab: Lập phiếu hoàn trả (Màn hình 5) */}
            {activeTab === 'returns' && <ClinicalReturnCreatePanel />}

            {/* Modal Chọn bệnh nhân từ HIS */}
            <SelectPatientHisModal
                open={isHisModalOpen}
                onClose={() => setIsHisModalOpen(false)}
                onSelectPatient={handleSelectHisPatient}
                currentSelectedCode={selectedPatient?.patientCode}
            />

            {/* Modal Xác nhận gửi phiếu */}
            <ConfirmSendRequestModal
                open={isConfirmModalOpen}
                loading={isSubmittingRequest}
                data={confirmData}
                onClose={() => {
                    if (isSubmittingRequest) return
                    setIsConfirmModalOpen(false)
                    setConfirmContext(null)
                    setConfirmData(null)
                }}
                onConfirm={handleConfirmSubmit}
            />

            {/* Drawer xem chi tiết phiếu */}
            <BloodRequestDetailDrawer
                requestId={selectedRequestId}
                open={isDrawerOpen}
                onClose={() => {
                    setIsDrawerOpen(false)
                    setSelectedRequestId(null)
                }}
                onEditDraft={handleEditDraft}
                onSubmitDraft={handleSubmitDraft}
            />
        </PageContainer>
    )
}
