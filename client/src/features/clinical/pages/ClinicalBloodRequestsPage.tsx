import { useState, useMemo, useEffect } from 'react'
import {
    Alert,
    Card,
    Table,
    Input,
    Button,
    Select,
    Form,
    InputNumber,
    Tag,
    message,
    Dropdown,
    Space,
    Empty,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
    SearchOutlined,
    FilterOutlined,
    ScanOutlined,
    RightOutlined,
    SyncOutlined,
    CheckCircleOutlined,
    RollbackOutlined,
    CloseCircleOutlined,
    ReloadOutlined,
} from '@ant-design/icons'
import {
    useClinicalRequests,
    useCreateBloodRequest,
    useUpdateDraftRequest,
    useSubmitDraftRequest,
    usePendingReceipts,
    usePendingBloodReturns,
    useComponentTypes,
} from '../hooks'
import { RequestStatusStepper } from '../components/RequestStatusStepper'
import { SelectPatientHisModal } from '../components/SelectPatientHisModal'
import { ConfirmSendRequestModal, type ConfirmSendRequestData } from '../components/ConfirmSendRequestModal'
import { BloodRequestDetailDrawer } from '../components/BloodRequestDetailDrawer'
import { ScanHandoverModal } from '../components/ScanHandoverModal'
import { CreateReturnModal } from '../components/CreateReturnModal'
import { getUrgencyMeta, getStatusMeta, formatUnits } from '../format'
import { formatBloodGroup, parseBloodGroup } from '@/shared/formatters/medical'
import { formatDateTime } from '@/shared/formatters/date'
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
    PendingBloodReturnDto,
    PendingReceiptBagDto,
    UpdateBloodRequestDto,
    UrgencyLevel,
} from '../types'
import './ClinicalBloodRequestsPage.css'

type ConfirmSubmitContext =
    | { mode: 'create' }
    | { mode: 'edit-and-submit'; draftId: number; requestCode: string }
    | { mode: 'submit-draft'; draftId: number; requestCode: string }

export default function ClinicalBloodRequestsPage() {
    const queryClient = useQueryClient()
    const { displayName, user } = useCurrentUser()
    const { can } = usePermission()

    const canViewRequests = can(PERMISSIONS.BLOOD_REQUEST_VIEW)
    const canEditRequests = can(PERMISSIONS.BLOOD_REQUEST_EDIT)
    const canViewReturns = can(PERMISSIONS.BLOOD_RETURN_VIEW)
    const canEditReturns = can(PERMISSIONS.BLOOD_RETURN_EDIT)

    // Tab active: 'list' | 'create' | 'handovers' | 'returns'
    const [activeTab, setActiveTab] = useState<'list' | 'create' | 'handovers' | 'returns'>('list')

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

    // Query túi máu chờ nhận (W3.2)
    const {
        data: pendingBags,
        isLoading: isPendingLoading,
        isFetching: isPendingFetching,
        refetch: refetchPendingReceipts,
    } = usePendingReceipts(undefined, { enabled: canViewRequests })
    const pendingCount = pendingBags?.length || 0

    // Query phiếu hoàn trả đang chờ kiểm định (W3.3 - chỉ gọi lần đầu khi mở tab Hoàn trả)
    const {
        data: pendingReturnsPage,
        isLoading: isPendingReturnsLoading,
        isFetching: isPendingReturnsFetching,
        refetch: refetchReturns,
    } = usePendingBloodReturns(
        {
            pageNumber: 1,
            pageSize: 20,
        },
        { enabled: canViewReturns && activeTab === 'returns' },
    )

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
                patientDisplay: `${draftToLoad.patientName} · ${draftToLoad.patientCode}`,
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

    // Handover modal state
    const [isScanModalOpen, setIsScanModalOpen] = useState(false)
    const [targetHandoverBag, setTargetHandoverBag] = useState<PendingReceiptBagDto | null>(null)

    // Return modal state
    const [isReturnModalOpen, setIsReturnModalOpen] = useState(false)
    const [targetReturnBag, setTargetReturnBag] = useState<PendingReceiptBagDto | null>(null)

    // Xử lý khi chọn bệnh nhân từ popup HIS
    const handleSelectHisPatient = (patient: HisPatientDto) => {
        setSelectedPatient(patient)
        const displayVal = `${patient.patientName} · ${patient.patientCode}`
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
                    return <Tag className="req-blood-tag">{groupStr}</Tag>
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
                width: 110,
                render: (level: UrgencyLevel) => {
                    const meta = getUrgencyMeta(level)
                    return (
                        <span
                            className="req-urgency-badge"
                            style={{
                                color: meta.color,
                                backgroundColor: meta.bg,
                                borderColor: meta.border,
                            }}
                        >
                            {meta.label}
                        </span>
                    )
                },
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

    // Bảng nhận bàn giao (Tab Nhận bàn giao)
    const handoverColumns: ColumnsType<PendingReceiptBagDto> = useMemo(
        () => [
            {
                title: 'MÃ TÚI MÁU',
                dataIndex: 'barcode',
                key: 'barcode',
                render: (barcode) => <code className="handover-barcode">{barcode}</code>,
            },
            {
                title: 'NGƯỜI BỆNH',
                key: 'patient',
                render: (_, row) => (
                    <div>
                        <strong>{row.patientName}</strong>
                        <div style={{ color: '#64748b', fontSize: 12 }}>{row.patientCode}</div>
                    </div>
                ),
            },
            {
                title: 'CHẾ PHẨM / NHÓM MÁU',
                key: 'product',
                render: (_, row) => (
                    <div>
                        <div>{row.componentName} ({row.volumeMl} ml)</div>
                        <Tag color="red">{row.bloodType}</Tag>
                    </div>
                ),
            },
            {
                title: 'BÁC SĨ LẬP PHIẾU',
                key: 'doctor',
                render: (_, row) => (
                    <div>
                        <div>{row.doctorName || 'Bác sĩ điều trị'}</div>
                        {row.isOwnRequest ? (
                            <Tag color="green">Phiếu của bạn</Tag>
                        ) : (
                            <Tag color="gold">Nhận hộ trong khoa</Tag>
                        )}
                    </div>
                ),
            },
            {
                title: 'THỜI GIAN XUẤT KHO',
                dataIndex: 'issuedAt',
                key: 'issuedAt',
                render: (val) => (val ? formatDateTime(val) : '—'),
            },
            {
                title: 'HÀNH ĐỘNG',
                key: 'actions',
                align: 'center',
                render: (_, row) => (
                    <Space>
                        {canEditRequests && (
                            <Button
                                type="primary"
                                size="small"
                                icon={<CheckCircleOutlined />}
                                style={{ backgroundColor: '#1A7534', borderColor: '#1A7534' }}
                                onClick={() => {
                                    setTargetHandoverBag(row)
                                    setIsScanModalOpen(true)
                                }}
                            >
                                Quét nhận
                            </Button>
                        )}
                        {canEditReturns && (
                            <Button
                                size="small"
                                danger
                                icon={<RollbackOutlined />}
                                onClick={() => {
                                    setTargetReturnBag(row)
                                    setIsReturnModalOpen(true)
                                }}
                            >
                                Hoàn trả
                            </Button>
                        )}
                    </Space>
                ),
            },
        ],
        [canEditRequests, canEditReturns],
    )

    // Bảng danh sách phiếu hoàn trả đang kiểm định (Tab Lập phiếu hoàn trả)
    const returnColumns: ColumnsType<PendingBloodReturnDto> = useMemo(
        () => [
            {
                title: 'MÃ PHIẾU / TÚI MÁU',
                key: 'info',
                render: (_, r) => (
                    <div>
                        <strong>Mã phiếu #{r.returnId}</strong>
                        <div><code className="handover-barcode">{r.barcode}</code></div>
                    </div>
                ),
            },
            {
                title: 'NGƯỜI BỆNH',
                key: 'patient',
                render: (_, r) => (
                    <div>
                        <div><strong>{r.patientName}</strong> ({r.patientCode})</div>
                        <div style={{ color: '#64748b', fontSize: 12 }}>Phiếu yêu cầu: {r.requestCode}</div>
                    </div>
                ),
            },
            {
                title: 'CHẾ PHẨM',
                key: 'product',
                render: (_, r) => (
                    <div>
                        <div>{r.componentName} ({r.volumeMl} ml)</div>
                        <Tag color="red">{r.bloodType}</Tag>
                    </div>
                ),
            },
            {
                title: 'LÝ DO HOÀN TRẢ',
                dataIndex: 'returnReason',
                key: 'returnReason',
                render: (text) => <span style={{ color: '#475569' }}>{text}</span>,
            },
            {
                title: 'TRẠNG THÁI KIỂM ĐỊNH',
                key: 'status',
                align: 'center',
                render: () => <Tag color="processing">Chờ Ngân hàng máu kiểm định</Tag>,
            },
            {
                title: 'THỜI GIAN LẬP',
                dataIndex: 'processedAt',
                key: 'processedAt',
                render: (val) => formatDateTime(val),
            },
        ],
        [],
    )

    return (
        <div className="clinical-page-container">
            {/* Breadcrumb & Header */}
            <div className="clinical-header">
                <div>
                    <div className="clinical-breadcrumb">Yêu cầu máu / Lâm sàng</div>
                    <h1 className="clinical-title">Yêu cầu máu lâm sàng</h1>
                    <p className="clinical-subtitle">
                        Theo dõi tiến độ xử lý và thực hiện bàn giao máu an toàn hai chiều.
                    </p>
                </div>
                <div className="clinical-header__actions">
                    <button
                        type="button"
                        className="btn-sync-his"
                        onClick={() => {
                            queryClient.invalidateQueries({ queryKey: ['clinical'] })
                            if (canViewRequests) {
                                if (activeTab === 'list') refetchRequests()
                                refetchPendingReceipts()
                            }
                            if (canViewReturns && activeTab === 'returns') {
                                refetchReturns()
                            }
                            message.success('Đã làm mới dữ liệu đồng bộ với hệ thống!')
                        }}
                    >
                        <span className="sync-dot" />
                        <SyncOutlined />
                        <span>Đồng bộ HIS/LIS</span>
                    </button>
                </div>
            </div>

            {/* Custom Navigation Tab Bar (đã bỏ tiền tố Tổng quan, W3.1, W3.2, W3.3) */}
            <div className="clinical-tabbar">
                <button
                    type="button"
                    className={`clinical-tab-btn ${activeTab === 'list' ? 'is-active' : ''}`}
                    onClick={() => setActiveTab('list')}
                >
                    <span className="tab-label">Danh sách phiếu</span>
                </button>
                <button
                    type="button"
                    className={`clinical-tab-btn ${activeTab === 'create' ? 'is-active' : ''}`}
                    onClick={() => setActiveTab('create')}
                >
                    <span className="tab-label">Lập phiếu yêu cầu mới</span>
                </button>
                <button
                    type="button"
                    className={`clinical-tab-btn ${activeTab === 'handovers' ? 'is-active' : ''}`}
                    onClick={() => setActiveTab('handovers')}
                >
                    <span className="tab-label">Nhận bàn giao</span>
                    {pendingCount > 0 && <span className="tab-badge">{pendingCount}</span>}
                </button>
                <button
                    type="button"
                    className={`clinical-tab-btn ${activeTab === 'returns' ? 'is-active' : ''}`}
                    onClick={() => setActiveTab('returns')}
                >
                    <span className="tab-label">Lập phiếu hoàn trả</span>
                </button>
            </div>

            {/* Tab: Danh sách phiếu */}
            {activeTab === 'list' && (
                <Card className="clinical-main-card" bordered={false}>
                    <div className="clinical-card-header">
                        <div>
                            <h2 className="card-inner-title">Tiến độ yêu cầu máu</h2>
                            <p className="card-inner-sub">Cập nhật trạng thái theo thời gian thực từ kho máu</p>
                        </div>
                        <div className="clinical-filters">
                            <Input
                                placeholder="Tìm mã phiếu, người bệnh..."
                                prefix={<SearchOutlined style={{ color: '#94a3b8' }} />}
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
                                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
                                            <span style={{ fontWeight: 700, fontSize: 13, color: '#0f172a' }}>Bộ lọc tìm kiếm</span>
                                            {(urgencyFilter || statusFilter) && (
                                                <Button
                                                    type="link"
                                                    size="small"
                                                    style={{ padding: 0, height: 'auto', fontSize: 12, color: '#dc2626' }}
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

                                        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 14, paddingTop: 10, borderTop: '1px solid #f1f5f9' }}>
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
                                                style={{ backgroundColor: '#1A7534', borderColor: '#1A7534' }}
                                            >
                                                Áp dụng
                                            </Button>
                                        </div>
                                    </div>
                                )}
                            >
                                <Button icon={<FilterOutlined />} className="btn-filter">
                                    Bộ lọc {urgencyFilter || statusFilter ? '•' : ''}
                                </Button>
                            </Dropdown>
                        </div>
                    </div>

                    {/* Thanh hiển thị các tag lọc đang kích hoạt */}
                    {hasActiveFilter && (
                        <div className="active-filters-bar" style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 14, flexWrap: 'wrap' }}>
                            <span style={{ fontSize: 12, color: '#64748b' }}>Đang lọc:</span>
                            {searchKeyword.trim() && (
                                <Tag
                                    closable
                                    onClose={() => {
                                        setSearchKeyword('')
                                        setPageNumber(1)
                                    }}
                                    color="default"
                                >
                                    Từ khóa: "{searchKeyword}"
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
                                size="small"
                                icon={<CloseCircleOutlined />}
                                onClick={handleResetAllFilters}
                                style={{ padding: 0, height: 'auto', fontSize: 12, color: '#dc2626' }}
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

                    {/* Nút nổi quét nhận bàn giao góc dưới */}
                    {canEditRequests && (
                        <div className="floating-action-bar">
                            <Button
                                type="primary"
                                icon={<ScanOutlined />}
                                size="large"
                                className="btn-scan-floating"
                                onClick={() => {
                                    setTargetHandoverBag(null)
                                    setIsScanModalOpen(true)
                                }}
                            >
                                Quét nhận bàn giao
                            </Button>
                        </div>
                    )}
                </Card>
            )}

            {/* Tab: Lập phiếu yêu cầu mới / Chỉnh sửa phiếu nháp */}
            {activeTab === 'create' && (
                <Card className="clinical-main-card" bordered={false}>
                    <h2 className="card-inner-title">
                        {editingDraft
                            ? `Chỉnh sửa phiếu nháp (${editingDraft.requestCode})`
                            : 'Lập phiếu yêu cầu máu mới'}
                    </h2>

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
                                    Hủy chỉnh sửa & Tạo mới
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
                        {/* Hàng 1: Bệnh nhân + nút Chọn từ HIS (Đã sửa lỗi hiển thị tên bệnh nhân) */}
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
                                                ? `${selectedPatient.patientName} · ${selectedPatient.patientCode}`
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
                                <div style={{ marginTop: 6, fontSize: 12, color: '#166534', fontWeight: 500 }}>
                                    ✓ Đã chọn: <strong>{selectedPatient.patientName}</strong> ({selectedPatient.patientCode} · {selectedPatient.room} · {selectedPatient.bed})
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
                                onClick={handleSaveDraft}
                                loading={isSubmittingRequest}
                                disabled={!canEditRequests || isSubmittingRequest}
                                className="btn-draft"
                            >
                                {editingDraft ? 'Lưu thay đổi nháp' : 'Lưu nháp'}
                            </Button>
                            <Button
                                type="primary"
                                onClick={handleReviewAndSend}
                                loading={isSubmittingRequest}
                                disabled={!canEditRequests || isSubmittingRequest}
                                className="btn-review-send"
                            >
                                Xem lại & Gửi phiếu
                            </Button>
                        </div>
                    </Form>
                </Card>
            )}

            {/* Tab: Nhận bàn giao */}
            {activeTab === 'handovers' && (
                <Card className="clinical-main-card" bordered={false}>
                    <div className="clinical-card-header">
                        <div>
                            <h2 className="card-inner-title">Nhận bàn giao túi máu tại Lâm sàng</h2>
                            <p className="card-inner-sub">
                                Danh sách các túi máu đã được Ngân hàng máu xuất kho (trạng thái ISSUED) và đang chờ khoa điều trị xác nhận thực nhận.
                            </p>
                        </div>
                        <Button
                            icon={<ReloadOutlined />}
                            onClick={() => refetchPendingReceipts()}
                            loading={isPendingLoading || isPendingFetching}
                        >
                            Làm mới
                        </Button>
                    </div>

                    <Table
                        columns={handoverColumns}
                        dataSource={pendingBags || []}
                        rowKey="allocationId"
                        loading={isPendingLoading || isPendingFetching}
                        pagination={{ pageSize: 10 }}
                        locale={{
                            emptyText: (
                                <Empty
                                    description={
                                        <div>
                                            <p>Hiện không có túi máu nào đang chờ nhận tại khoa của bạn.</p>
                                            <small style={{ color: '#94a3b8' }}>
                                                Túi máu sẽ xuất hiện tại đây sau khi được Ngân hàng máu thẩm định (Allocate) và xuất kho (Dispatch).
                                            </small>
                                        </div>
                                    }
                                />
                            ),
                        }}
                    />
                </Card>
            )}

            {/* Tab: Lập phiếu hoàn trả */}
            {activeTab === 'returns' && (
                <Card className="clinical-main-card" bordered={false}>
                    <div className="clinical-card-header">
                        <div>
                            <h2 className="card-inner-title">Hoàn trả túi máu chưa sử dụng</h2>
                            <p className="card-inner-sub">
                                Lập biên bản hoàn trả túi máu về Ngân hàng máu khi bệnh nhân hoãn chỉ định hoặc thay đổi phác đồ điều trị.
                            </p>
                        </div>
                        <Button
                            icon={<ReloadOutlined />}
                            onClick={() => {
                                refetchReturns()
                                refetchPendingReceipts()
                            }}
                            loading={isPendingReturnsLoading || isPendingReturnsFetching || isPendingFetching}
                        >
                            Làm mới
                        </Button>
                    </div>

                    {/* Phần 1: Túi máu có thể hoàn trả tại khoa */}
                    <div style={{ marginBottom: 28 }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12 }}>
                            <h3 style={{ fontSize: 15, fontWeight: 700, margin: 0, color: '#1e293b' }}>
                                1. Túi máu tại khoa có thể lập phiếu hoàn trả
                            </h3>
                            <Tag color="blue">{pendingBags?.length || 0} túi khả dụng</Tag>
                        </div>
                        <p style={{ fontSize: 13, color: '#64748b', marginTop: 0, marginBottom: 12 }}>
                            Các túi máu đã xuất kho cho khoa của bạn và chưa hết hạn. Bấm "Hoàn trả" để kiểm tra 4 điều kiện an toàn sinh học.
                        </p>

                        <Table
                            columns={handoverColumns}
                            dataSource={pendingBags || []}
                            rowKey="allocationId"
                            loading={isPendingLoading || isPendingFetching}
                            pagination={false}
                            size="small"
                            locale={{
                                emptyText: (
                                    <Empty
                                        description="Hiện tại khoa không có túi máu nào đủ điều kiện hoàn trả."
                                        style={{ padding: 20 }}
                                    />
                                ),
                            }}
                        />
                    </div>

                    {/* Phần 2: Phiếu hoàn trả đang chờ Ngân hàng máu kiểm định */}
                    <div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12 }}>
                            <h3 style={{ fontSize: 15, fontWeight: 700, margin: 0, color: '#1e293b' }}>
                                2. Phiếu hoàn trả đang chờ Ngân hàng máu kiểm định
                            </h3>
                            <Tag color="orange">{pendingReturnsPage?.totalCount || 0} phiếu đang chờ</Tag>
                        </div>
                        <p style={{ fontSize: 13, color: '#64748b', marginTop: 0, marginBottom: 12 }}>
                            Danh sách các túi máu đã lập phiếu hoàn trả và phong tỏa (LOCKED), đang chờ KTV Ngân hàng máu quét kiểm định tiếp nhận.
                        </p>

                        <Table
                            columns={returnColumns}
                            dataSource={pendingReturnsPage?.items || []}
                            rowKey="returnId"
                            loading={isPendingReturnsLoading || isPendingReturnsFetching}
                            pagination={{
                                pageSize: 10,
                                total: pendingReturnsPage?.totalCount || 0,
                            }}
                            locale={{
                                emptyText: (
                                    <Empty
                                        description="Chưa có phiếu hoàn trả nào của khoa đang chờ kiểm định."
                                        style={{ padding: 20 }}
                                    />
                                ),
                            }}
                        />
                    </div>
                </Card>
            )}

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

            {/* Drawer xem chi tiết phiếu (Có hỗ trợ xử lý phiếu DRAFT: Tiếp tục chỉnh sửa & Gửi phiếu ngay) */}
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

            {/* Modal Quét đối soát nhận bàn giao */}
            <ScanHandoverModal
                open={isScanModalOpen}
                onClose={() => {
                    setIsScanModalOpen(false)
                    setTargetHandoverBag(null)
                }}
                targetBag={targetHandoverBag}
            />

            {/* Modal Lập phiếu hoàn trả */}
            <CreateReturnModal
                open={isReturnModalOpen}
                onClose={() => {
                    setIsReturnModalOpen(false)
                    setTargetReturnBag(null)
                }}
                bag={targetReturnBag}
            />
        </div>
    )
}
