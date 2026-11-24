import { useCallback, useEffect, useState } from 'react'
import { Alert, App, Button, Empty, Spin } from 'antd'
import { ClockCircleOutlined, MedicineBoxOutlined, ReloadOutlined, ScanOutlined } from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
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
import { usePendingHandoverSessions } from '../hooks'
import '@/shared/styles/HandoverLayout.css'
import './ClinicalHandoverPages.css'

export default function ClinicalHandoverScanPage() {
    const { message } = App.useApp()
    const navigate = useNavigate()
    const { displayName, roleLabel } = useCurrentUser()
    const { can } = usePermission()
    const canView = can(PERMISSIONS.BLOOD_REQUEST_VIEW)

    const {
        data: pendingSessions = [],
        isLoading: isPendingLoading,
        isFetching: isPendingFetching,
        refetch: refetchPending,
    } = usePendingHandoverSessions(undefined, { enabled: canView, refetchInterval: 5000 })

    const [verifying, setVerifying] = useState(false)
    const [errorMsg, setErrorMsg] = useState<string | null>(null)
    const [nowMs, setNowMs] = useState(() => Date.now())

    useEffect(() => {
        if (pendingSessions.length === 0) return
        const timer = window.setInterval(() => {
            setNowMs(Date.now())
        }, 1000)
        return () => window.clearInterval(timer)
    }, [pendingSessions.length])

    const handleScan = useCallback(
        async (rawBarcode: string) => {
            const barcode = rawBarcode.trim()
            if (!barcode) return

            if (!canView) {
                const msg = 'Bạn không có quyền BLOOD_REQUEST_VIEW để xác minh phiên bàn giao túi máu.'
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

    return (
        <PageContainer className="handover-page">
            <PageHeader
                breadcrumb={[
                    { title: 'Yêu cầu máu Lâm sàng', path: '/clinical/requests' },
                    { title: 'Nhận bàn giao túi máu' },
                ]}
                title="Nhận bàn giao túi máu tại quầy"
                subtitle="Quét đủ barcode các túi máu trong phiên bàn giao đang chờ (Pha 2) để đối chiếu và chính thức nhận máu về khoa."
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
                        <span className="handover-role-badge is-clin">CLIN</span>
                    </div>
                </div>

                <Alert
                    type="info"
                    showIcon
                    message="Quy trình nhận máu 2 pha tại quầy Ngân hàng máu"
                    description="Mỗi phiên bàn giao chỉ có hiệu lực 3–5 phút kể từ lúc BBNK khởi tạo. Quét barcode một túi bất kỳ hoặc chọn phiên bên dưới để quét đủ 100% số túi trong lô và xác nhận nhận máu."
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
            </section>
        </PageContainer>
    )
}
