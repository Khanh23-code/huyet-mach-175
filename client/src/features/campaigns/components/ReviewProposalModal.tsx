import { useState } from 'react'
import { App, Button, Descriptions, Input, Modal, Skeleton } from 'antd'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import ConfigCard from './ConfigCard'
import Can from '@/features/auth/components/Can'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { proposalsApi } from '../proposalsApi'
import { useProposal } from '../hooks'
import { SITE_TYPE_LABEL, fmtDateTime, proposalKindText } from '../format'
import "./ReviewProposalModal.css";

interface Props { proposalId: number | null; onClose: () => void }

export default function ReviewProposalModal({ proposalId, onClose }: Props) {
    const { message, modal } = App.useApp()
    const qc = useQueryClient()
    const { data: p, isLoading } = useProposal(proposalId)

    const [rejecting, setRejecting] = useState(false)
    const [reason, setReason] = useState('')
    const [reasonError, setReasonError] = useState('')

    const close = () => {
        setRejecting(false)
        setReason('')
        setReasonError('')
        onClose()
    }

    const refresh = async () => {
        await qc.invalidateQueries({ queryKey: ['proposals'] })
        await qc.invalidateQueries({ queryKey: ['sites'] })
    }

    const approve = useMutation({
        mutationFn: () => proposalsApi.approve(proposalId as number),
        onSuccess: async (r) => {
            await refresh()
            close()
            modal.success({
                title: 'Phê duyệt & kích hoạt thành công',
                content: `Đề xuất ${r.proposalCode} đã được kích hoạt và đồng bộ sang ứng dụng người hiến.`,
                okText: 'Đóng',
            })
        },
        onError: (e) => message.error(parseApiError(e).message),
    })

    const reject = useMutation({
        mutationFn: (r: string) => proposalsApi.reject(proposalId as number, r),
        onSuccess: async (r) => {
            await refresh()
            close()
            modal.info({
                title: 'Đã từ chối đề xuất',
                content: `Đề xuất ${r.proposalCode} đã bị từ chối. Lý do được gửi tới người đề xuất.`,
                okText: 'Đóng',
            })
        },
        onError: (e) => message.error(parseApiError(e).message),
    })

    const submitReject = () => {
        const r = reason.trim()
        if (r.length < 5) {
            setReasonError('Vui lòng nhập lý do từ chối (tối thiểu 5 ký tự)')
            return
        }
        if (r.length > 255) {
            setReasonError('Lý do tối đa 255 ký tự')
            return
        }
        reject.mutate(r)
    }

    const busy = approve.isPending || reject.isPending

    return (
        <Modal
            className="review-proposal-modal"
            title="Xem & duyệt đề xuất"
            open={proposalId !== null}
            onCancel={close}
            width={760}
            destroyOnHidden
            maskClosable={!busy}
            footer={
                <div className="ds-modal-foot">
                    <Button onClick={close} disabled={busy}>Đóng</Button>
                    {p && !rejecting && (
                        <>
                            <Can do={PERMISSIONS.CAMPAIGN_CANCEL}>
                                <Button danger onClick={() => setRejecting(true)} disabled={busy}>Từ chối</Button>
                            </Can>
                            <Can do={PERMISSIONS.CAMPAIGN_APPROVE}>
                                <Button type="primary" loading={approve.isPending} onClick={() => approve.mutate()}>
                                    Phê duyệt &amp; kích hoạt
                                </Button>
                            </Can>
                        </>
                    )}
                    {p && rejecting && (
                        <>
                            <Button onClick={() => { setRejecting(false); setReasonError('') }} disabled={busy}>Quay lại</Button>
                            <Button danger type="primary" loading={reject.isPending} onClick={submitReject}>Xác nhận từ chối</Button>
                        </>
                    )}
                </div>
            }
        >
            {isLoading || !p ? (
                <Skeleton active paragraph={{ rows: 6 }} />
            ) : (
                <>
                    <Descriptions column={2} size="small" bordered style={{ marginBottom: 16 }}>
                        <Descriptions.Item label="Mã đề xuất">{p.proposalCode}</Descriptions.Item>
                        <Descriptions.Item label="Loại đề xuất">{proposalKindText(p)}</Descriptions.Item>
                        <Descriptions.Item label="Điểm hiến" span={2}>
                            {p.siteName} <span className={`ds-tag ds-tag--${p.siteType}`}>{SITE_TYPE_LABEL[p.siteType]}</span>
                        </Descriptions.Item>
                        <Descriptions.Item label="Địa chỉ" span={2}>{p.address}</Descriptions.Item>
                        <Descriptions.Item label="Số điện thoại">{p.phoneNumber}</Descriptions.Item>
                        <Descriptions.Item label="Thời gian gửi">{fmtDateTime(p.createdAt)}</Descriptions.Item>
                        <Descriptions.Item label="Người gửi" span={2}>
                            {p.createdByName}{p.createdByRoleCode ? ` · ${p.createdByRoleCode}` : ''}
                        </Descriptions.Item>
                    </Descriptions>

                    <div className="ds-compare">
                        <ConfigCard
                            label="Cấu hình hiện tại"
                            title="Trước khi thay đổi"
                            subtitle={p.proposalType === 'NEW_SITE'
                                ? 'Chưa có điểm hiến trong hệ thống'
                                : `${p.currentSlots.reduce((n, x) => n + x.capacity, 0)} lượt/ngày`}
                            slots={p.proposalType === 'NEW_SITE' ? undefined : p.currentSlots}
                        />
                        <span className="ds-compare__arrow" aria-hidden>→</span>
                        <ConfigCard
                            label="Cấu hình đề xuất"
                            title="Sau khi kích hoạt"
                            subtitle={p.proposalType === 'NEW_SITE'
                                ? `${p.siteType === 'MOBILE' ? 'Điểm lưu động mới' : 'Điểm cố định mới'} · ${p.totalCapacityPerDay} lượt/ngày`
                                : `${p.totalCapacityPerDay} lượt/ngày`}
                            slots={p.slots}
                            highlight
                        />
                    </div>

                    {rejecting && (
                        <div className="ds-field" style={{ marginTop: 16 }}>
                            <label>Lý do từ chối <span className="ds-req">*</span></label>
                            <Input.TextArea
                                className="reject-reason-input"
                                rows={3}
                                maxLength={255}
                                showCount
                                value={reason}
                                status={reasonError ? 'error' : ''}
                                placeholder="Nêu rõ lý do để BBNK chỉnh sửa và gửi lại"
                                onChange={(e) => { setReason(e.target.value); if (reasonError) setReasonError('') }}
                            />
                            {reasonError && <div className="ds-field__error">{reasonError}</div>}
                        </div>
                    )}
                </>
            )}
        </Modal>
    )
}
