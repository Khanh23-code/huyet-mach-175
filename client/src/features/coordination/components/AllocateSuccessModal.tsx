import { Modal, Tag } from 'antd'
import { CheckCircleFilled } from '@ant-design/icons'
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
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, color: '#16a34a' }}>
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
                <div style={{ fontSize: 13, color: '#334155', lineHeight: 1.6 }}>
                    Hệ thống đã xác nhận giữ chỗ thành công <strong>{result.allocatedCount} túi máu</strong> cho phiếu{' '}
                    <strong>{result.requestCode}</strong>.
                </div>

                <div
                    style={{
                        padding: '12px 16px',
                        backgroundColor: '#f8fafc',
                        borderRadius: 8,
                        border: '1px solid #e2e8f0',
                        fontSize: 13,
                        display: 'flex',
                        flexDirection: 'column',
                        gap: 8,
                    }}
                >
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                        <span style={{ color: '#64748b' }}>Tiến độ dòng chế phẩm:</span>
                        <strong style={{ color: '#0f172a' }}>
                            {result.itemAllocatedUnits} / {result.itemRequestedUnits} đơn vị
                        </strong>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: '#64748b' }}>Trạng thái phiếu:</span>
                        <Tag color={isFullyAllocated ? 'green' : 'orange'}>
                            {isFullyAllocated ? 'READY - ĐÃ ĐỦ TÚI (SẴN SÀNG XUẤT KHO)' : 'PENDING - ĐANG ĐIỀU PHỐI (CÒN THIẾU)'}
                        </Tag>
                    </div>
                </div>

                {result.allocatedBags && result.allocatedBags.length > 0 && (
                    <div>
                        <div style={{ fontSize: 13, fontWeight: 600, color: '#1e293b', marginBottom: 8 }}>
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
                                        background: '#ffffff',
                                        border: '1px solid #e2e8f0',
                                        borderRadius: 6,
                                        fontSize: 13,
                                    }}
                                >
                                    <span style={{ fontFamily: 'monospace', fontWeight: 600, color: '#0f172a' }}>
                                        {bag.barcode}
                                    </span>
                                    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                                        <Tag color="red">{bag.bloodType}</Tag>
                                        <span style={{ color: '#64748b' }}>{bag.volumeMl} ml</span>
                                        {bag.isExactMatch ? (
                                            <Tag color="blue">Trùng nhóm</Tag>
                                        ) : (
                                            <Tag color="purple">Hòa hợp</Tag>
                                        )}
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
