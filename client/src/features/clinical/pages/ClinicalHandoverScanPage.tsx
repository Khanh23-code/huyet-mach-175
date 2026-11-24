import { useCallback, useEffect, useState } from 'react'
import { Alert, App, Button, Empty, Spin, Tabs } from 'antd'
import {
    CheckCircleOutlined,
    ClockCircleOutlined,
    HeartOutlined,
    MedicineBoxOutlined,
    ReloadOutlined,
    ScanOutlined,
} from '@ant-design/icons'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useCurrentUser } from '@/features/auth/useCurrentUser'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { computeRemainingSeconds, formatCountdown, formatDateTime } from '@/shared/formatters/date'
import {
    BarcodeScanBox,
    BloodGroupTag,
    PageContainer,
    PageHeader,
    StatusPill,
    UrgencyTag,
} from '@/shared/components'
import { clinicalApi } from '../clinicalApi'
import { ConfirmTransfusionModal } from '../components/ConfirmTransfusionModal'
import { usePendingHandoverSessions } from '../hooks'
import type { ConfirmTransfusionResponse, TransfusionScanDto } from '../types'
import '@/shared/styles/HandoverLayout.css'
import './ClinicalHandoverPages.css'

type HandoverPageTab = 'receive' | 'transfuse'

export default function ClinicalHandoverScanPage() {
    const { message } = App.useApp()
    const navigate = useNavigate()
    const [searchParams, setSearchParams] = useSearchParams()
    const { displayName, roleLabel } = useCurrentUser()
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)

    const initialTab: HandoverPageTab = searchParams.get('tab') === 'transfuse' ? 'transfuse' : 'receive'
    const [activeTab, setActiveTab] = useState<HandoverPageTab>(initialTab)

    const {
        data: pendingSessions = [],
        isLoading: isPendingLoading,
        isFetching: isPendingFetching,
        refetch: refetchPending,
    } = usePendingHandoverSessions(undefined, { enabled: canView, refetchInterval: 5000 })

    const [verifying, setVerifying] = useState(false)
    const [errorMsg, setErrorMsg] = useState<string | null>(null)
    const [nowMs, setNowMs] = useState(() => Date.now())

    // State cho sub-tab Truyền máu tại giường (ISSUED -> TRANSFUSED)
    const [scanningTransfusion, setScanningTransfusion] = useState(false)
    const [transfusionErrorMsg, setTransfusionErrorMsg] = useState<string | null>(null)
    const [scannedTransfusionBag, setScannedTransfusionBag] = useState<TransfusionScanDto | null>(null)
    const [transfusionModalOpen, setTransfusionModalOpen] = useState(false)
    const [lastTransfusion, setLastTransfusion] = useState<ConfirmTransfusionResponse | null>(null)

    useEffect(() => {
        if (pendingSessions.length === 0) return
        const timer = window.setInterval(() => {
            setNowMs(Date.now())
        }, 1000)
        return () => window.clearInterval(timer)
    }, [pendingSessions.length])

    const handleTabChange = (key: string) => {
        const nextTab: HandoverPageTab = key === 'transfuse' ? 'transfuse' : 'receive'
        setActiveTab(nextTab)
        if (nextTab === 'transfuse') {
            setSearchParams({ tab: 'transfuse' }, { replace: true })
        } else {
            setSearchParams({}, { replace: true })
        }
    }

    const handleScan = useCallback(
        async (rawBarcode: string) => {
            const barcode = rawBarcode.trim()
            if (!barcode) return

            if (!canView) {
                const msg = 'Bạn chưa có quyền xem hoặc xác minh phiên bàn giao túi máu.'
                setErrorMsg(msg)
                message.error(msg)
                return
            }

            setVerifying(true)
            setErrorMsg(null)

            try {
                const latestSessions = await clinicalApi.getPendingHandoverSessions()
                const normalized = barcode.toLowerCase()
                const matchedSession = latestSessions.find((session) =>
                    session.items.some((item) => item.barcode.trim().toLowerCase() === normalized),
                )

                if (!matchedSession) {
                    const msg = `Không tìm thấy túi máu "${barcode}" trong bất kỳ phiên bàn giao nào đang chờ xác nhận tại khoa của bạn (BBNK chưa mở phiên, phiên đã hết hạn 3-5 phút, thuộc khoa khác hoặc đã xác nhận trước đó).`
                    setErrorMsg(msg)
                    message.error(msg)
                    return
                }

                const matchedItem = matchedSession.items.find(
                    (item) => item.barcode.trim().toLowerCase() === normalized,
                )

                navigate(`/clinical/handovers/${matchedSession.handoverSessionId}`, {
                    state: {
                        session: matchedSession,
                        initialScannedBarcode: matchedItem?.barcode ?? barcode,
                    },
                })
            } catch (err) {
                const msg = parseApiError(err).message
                setErrorMsg(msg)
                message.error(msg)
            } finally {
                setVerifying(false)
            }
        },
        [canView, message, navigate],
    )

    const handleTransfusionScan = useCallback(
        async (rawBarcode: string) => {
            const barcode = rawBarcode.trim()
            if (!barcode) return

            if (!canView) {
                const msg = 'Bạn chưa có quyền kiểm tra túi máu truyền tại giường.'
                setTransfusionErrorMsg(msg)
                message.error(msg)
                return
            }

            setScanningTransfusion(true)
            setTransfusionErrorMsg(null)

            try {
                const data = await clinicalApi.scanTransfusionBag(barcode)
                if (data.bagStatus !== 'ISSUED') {
                    const msg = `Túi máu "${data.barcode}" hiện không ở trạng thái đã xuất kho, không thể thực hiện truyền máu tại giường.`
                    setTransfusionErrorMsg(msg)
                    message.error(msg)
                    return
                }

                setScannedTransfusionBag(data)
                setTransfusionModalOpen(true)
            } catch (err) {
                const msg = parseApiError(err).message
                setTransfusionErrorMsg(msg)
                message.error(msg)
            } finally {
                setScanningTransfusion(false)
            }
        },
        [canView, message],
    )

    return (
        <PageContainer className="handover-page">
            <PageHeader
                breadcrumb={[
                    { title: 'Yêu cầu máu Lâm sàng', path: '/clinical/requests' },
                    { title: 'Nhận & truyền túi máu' },
                ]}
                title="Nhận bàn giao & Truyền máu tại giường"
                subtitle="Xác nhận nhận lô túi máu tại quầy Ngân hàng máu hoặc quét barcode túi máu đã xuất kho để bắt đầu truyền cho người bệnh."
                extra={
                    <StatusPill tone={pendingSessions.length > 0 ? 'warning' : 'neutral'} pulse={pendingSessions.length > 0}>
                        {pendingSessions.length > 0
                            ? `${pendingSessions.length} phiên chờ quét nhận`
                            : 'Chưa có phiên chờ'}
                    </StatusPill>
                }
            />

            <section className="handover-card">
                <div className="handover-role-bar">
                    <div className="handover-role-info">
                        <div className="handover-role-icon is-clin">
                            <MedicineBoxOutlined />
                        </div>
                        <div>
                            <div className="handover-role-title">Nhân viên / Điều dưỡng lâm sàng</div>
                            <div className="handover-role-sub">
                                {displayName} - {roleLabel || 'Khoa Lâm sàng'}
                            </div>
                        </div>
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                        {activeTab === 'receive' && (
                            <Button
                                size="small"
                                icon={<ReloadOutlined spin={isPendingFetching} />}
                                onClick={() => {
                                    setNowMs(Date.now())
                                    void refetchPending()
                                }}
                                disabled={isPendingLoading || isPendingFetching}
                            >
                                Làm mới danh sách chờ
                            </Button>
                        )}
                        <span className="handover-role-badge is-clin">CLIN</span>
                    </div>
                </div>

                <Tabs
                    activeKey={activeTab}
                    onChange={handleTabChange}
                    items={[
                        {
                            key: 'receive',
                            label: (
                                <span>
                                    <ScanOutlined /> Nhận bàn giao tại quầy
                                    {pendingSessions.length > 0 ? ` (${pendingSessions.length})` : ''}
                                </span>
                            ),
                            children: (
                                <div style={{ display: 'grid', gap: 18 }}>
                                    <Alert
                                        type="info"
                                        showIcon
                                        message="Quy trình nhận máu 2 pha tại quầy Ngân hàng máu"
                                        description="Mỗi phiên bàn giao chỉ có hiệu lực 3-5 phút kể từ lúc BBNK khởi tạo. Quét barcode một túi bất kỳ hoặc chọn phiên bên dưới để quét đủ 100% số túi trong lô và xác nhận nhận máu."
                                    />

                                    {isPendingLoading ? (
                                        <div style={{ padding: 36, textAlign: 'center' }}>
                                            <Spin tip="Đang tải danh sách phiên bàn giao chờ nhận tại khoa..." />
                                        </div>
                                    ) : pendingSessions.length > 0 ? (
                                        <div className="handover-session-list">
                                            {pendingSessions.map((session) => {
                                                const remaining = computeRemainingSeconds(session.expiresAt, nowMs)
                                                const isExpiring = remaining <= 60
                                                return (
                                                    <div key={session.handoverSessionId} className="clin-pending-item" style={{ display: 'grid', gap: 12 }}>
                                                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 16, flexWrap: 'wrap' }}>
                                                            <div>
                                                                <div className="clin-pending-item__eyebrow">
                                                                    Phiên {session.sessionCode} - {session.isOwnRequest ? 'Phiếu của bạn' : 'Nhận hộ trong khoa'}
                                                                </div>
                                                                <div className="clin-pending-item__title">
                                                                    {session.requestCode} - {session.patientName} ({session.patientCode})
                                                                </div>
                                                                <div className="clin-pending-item__sub">
                                                                    {session.departmentName || 'Khoa điều trị'} - BS {session.doctorName || '—'} - Mở lúc{' '}
                                                                    {formatDateTime(session.createdAt)} bởi {session.initiatedByName || 'KTV BBNK'}
                                                                </div>
                                                            </div>

                                                            <div className="clin-pending-item__right" style={{ flexWrap: 'wrap' }}>
                                                                <UrgencyTag level={session.urgencyLevel} />
                                                                <BloodGroupTag value={session.patientBloodType} size="lg" />
                                                                <StatusPill tone={isExpiring ? 'danger' : 'warning'} pulse={remaining > 0}>
                                                                    <ClockCircleOutlined />{' '}
                                                                    {remaining > 0 ? `Còn ${formatCountdown(remaining)}` : 'Hết giờ'}
                                                                </StatusPill>
                                                                <Button
                                                                    type="primary"
                                                                    icon={<ScanOutlined />}
                                                                    disabled={remaining <= 0}
                                                                    onClick={() =>
                                                                        navigate(`/clinical/handovers/${session.handoverSessionId}`, {
                                                                            state: { session },
                                                                        })
                                                                    }
                                                                >
                                                                    Đối soát &amp; Nhận ({session.items.length} túi)
                                                                </Button>
                                                            </div>
                                                        </div>

                                                        <div className="handover-session-items">
                                                            {session.items.map((item) => (
                                                                <div key={item.allocationId} className="handover-session-chip">
                                                                    <span className="text-mono" style={{ fontWeight: 700, color: 'var(--c-text)' }}>
                                                                        {item.barcode}
                                                                    </span>
                                                                    <span>
                                                                        {item.componentCode} - {item.bloodType} - {item.volumeMl}ml
                                                                    </span>
                                                                </div>
                                                            ))}
                                                        </div>
                                                    </div>
                                                )
                                            })}
                                        </div>
                                    ) : (
                                        <Empty
                                            image={Empty.PRESENTED_IMAGE_SIMPLE}
                                            description="Hiện không có phiên bàn giao nào đang chờ xác nhận tại khoa của bạn."
                                        />
                                    )}

                                    <BarcodeScanBox
                                        title="Quét nhanh barcode túi máu để mở phiên đối soát"
                                        description="Quét mã vạch của một túi máu trong lô được giao; hệ thống sẽ tự động mở đúng phiên bàn giao chứa túi đó."
                                        placeholder="Nhập hoặc quét mã barcode túi máu..."
                                        submitLabel="Quét & mở phiên"
                                        loading={verifying}
                                        errorMessage={errorMsg}
                                        onClearError={() => setErrorMsg(null)}
                                        onScan={handleScan}
                                    />
                                </div>
                            ),
                        },
                        {
                            key: 'transfuse',
                            label: (
                                <span>
                                    <HeartOutlined /> Truyền máu tại giường
                                </span>
                            ),
                            children: (
                                <div style={{ display: 'grid', gap: 18 }}>
                                    <Alert
                                        type="info"
                                        showIcon
                                        message="Đối chiếu an toàn trước khi bắt đầu truyền máu tại giường"
                                        description="Chỉ các túi máu đang ở trạng thái Đã xuất kho thuộc khoa của bạn mới được phép truyền. Quét mã vạch túi máu để mở hộp thoại xác nhận mã người bệnh trước khi chuyển sang trạng thái Đã truyền."
                                    />

                                    {lastTransfusion && (
                                        <Alert
                                            type="success"
                                            showIcon
                                            closable
                                            onClose={() => setLastTransfusion(null)}
                                            icon={<CheckCircleOutlined />}
                                            message={`Đã xác nhận truyền túi máu ${lastTransfusion.barcode}`}
                                            description={
                                                <div style={{ display: 'grid', gap: 4, marginTop: 4 }}>
                                                    <div>
                                                        Người bệnh: <strong>{lastTransfusion.patientName}</strong> ({lastTransfusion.patientCode}) - Nhóm máu:{' '}
                                                        <strong>{lastTransfusion.patientBloodType}</strong>
                                                    </div>
                                                    <div>
                                                        Phiếu yêu cầu: <strong>{lastTransfusion.requestCode}</strong> - Chế phẩm:{' '}
                                                        {lastTransfusion.componentName} ({lastTransfusion.bloodType})
                                                    </div>
                                                    <div>
                                                        Thời gian xác nhận: {formatDateTime(lastTransfusion.transfusedAt)}
                                                        {lastTransfusion.confirmedByName ? ` - Thực hiện bởi: ${lastTransfusion.confirmedByName}` : ''}
                                                    </div>
                                                </div>
                                            }
                                        />
                                    )}

                                    <BarcodeScanBox
                                        title="Quét barcode túi máu để xác nhận truyền cho người bệnh"
                                        description="Hệ thống kiểm tra trạng thái túi máu ngay trên giao diện và hiển thị hộp thoại nhập mã vòng tay người bệnh."
                                        placeholder="Nhập hoặc quét mã barcode túi máu đã xuất kho..."
                                        submitLabel="Kiểm tra & mở xác nhận"
                                        loading={scanningTransfusion}
                                        errorMessage={transfusionErrorMsg}
                                        onClearError={() => setTransfusionErrorMsg(null)}
                                        onScan={handleTransfusionScan}
                                    />
                                </div>
                            ),
                        },
                    ]}
                />
            </section>

            <ConfirmTransfusionModal
                open={transfusionModalOpen}
                bag={scannedTransfusionBag}
                onClose={() => setTransfusionModalOpen(false)}
                onConfirmed={(res) => {
                    setLastTransfusion(res)
                    setTransfusionErrorMsg(null)
                }}
            />
        </PageContainer>
    )
}
