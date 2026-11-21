import { Modal } from 'antd'
import { CheckCircleFilled } from '@ant-design/icons'
import { BloodGroupTag, StatusPill } from '@/shared/components'
import type { AllocateBloodBagsResponse } from '../types'

interface Props {
    visible: boolean
    result: AllocateBloodBagsResponse | null
    onClose: () => void
}

export default function AllocateSuccessModal({ visible, result, onClose }: Props) {
    if (!result) return null

    const isFullyAllocated = result.requestStatus === 'READY'

    return (
        <Modal
            title={
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, color: 'var(--c-success)' }}>
                    <CheckCircleFilled style={{ fontSize: 20 }} />
                    <span>Giữ chỗ túi máu thành công</span>
                </div>
            }
            open={visible}
            onOk={onClose}
            onCancel={onClose}
            cancelButtonProps={{ style: { display: 'none' } }}
            okText="Đóng"
            width={580}
            destroyOnClose
        >
            <div style={{ display: 'flex', flexDirection: 'column', gap: 14, paddingTop: 10 }}>
                <div style={{ fontSize: 'var(--text-sm)', color: 'var(--c-text-secondary)', lineHeight: 1.6 }}>
                    Hệ thống đã xác nhận giữ chỗ thành công <strong>{result.allocatedCount} túi máu</strong> cho phiếu{' '}
                    <strong>{result.requestCode}</strong>.
                </div>

                <div
                    style={{
                        padding: '12px 16px',
                        backgroundColor: 'var(--c-surface-muted)',
                        borderRadius: 'var(--radius-sm)',
                        border: '1px solid var(--c-border)',
                        fontSize: 'var(--text-sm)',
                        display: 'flex',
                        flexDirection: 'column',
                        gap: 8,
                    }}
                >
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                        <span style={{ color: 'var(--c-text-muted)' }}>Tiến độ dòng chế phẩm:</span>
                        <strong style={{ color: 'var(--c-text)' }}>
                            {result.itemAllocatedUnits} / {result.itemRequestedUnits} đơn vị
                        </strong>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--c-text-muted)' }}>Trạng thái phiếu:</span>
                        <StatusPill tone={isFullyAllocated ? 'success' : 'warning'}>
                            {isFullyAllocated
                                ? 'READY - ĐÃ ĐỦ TÚI (SẴN SÀNG XUẤT KHO)'
                                : 'PENDING - ĐANG ĐIỀU PHỐI (CÒN THIẾU)'}
                        </StatusPill>
                    </div>
                </div>

                {result.allocatedBags && result.allocatedBags.length > 0 && (
                    <div>
                        <div
                            style={{
                                fontSize: 'var(--text-sm)',
                                fontWeight: 600,
                                color: 'var(--c-text)',
                                marginBottom: 8,
                            }}
                        >
                            Danh sách túi máu đã khóa giữ chỗ:
                        </div>
                        <div style={{ display: 'flex', flexDirection: 'column', gap: 6, maxHeight: 220, overflowY: 'auto' }}>
                            {result.allocatedBags.map((bag) => (
                                <div
                                    key={bag.bagId}
                                    style={{
                                        display: 'flex',
                                        justifyContent: 'space-between',
                                        alignItems: 'center',
                                        padding: '8px 12px',
                                        background: 'var(--c-surface)',
                                        border: '1px solid var(--c-border)',
                                        borderRadius: 'var(--radius-sm)',
                                        fontSize: 'var(--text-sm)',
                                    }}
                                >
                                    <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600, color: 'var(--c-text)' }}>
                                        {bag.barcode}
                                    </span>
                                    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                                        <BloodGroupTag value={bag.bloodType} size="sm" />
                                        <span style={{ color: 'var(--c-text-muted)' }}>{bag.volumeMl} ml</span>
                                        <StatusPill tone={bag.isExactMatch ? 'info' : 'purple'} dot={false}>
                                            {bag.isExactMatch ? 'Trùng nhóm' : 'Hòa hợp'}
                                        </StatusPill>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                )}
            </div>
        </Modal>
    )
}
