import { Descriptions, Modal } from 'antd'
import ConfigCard from './ConfigCard'
import { SITE_TYPE_LABEL, fmtDateTime } from '../format'
import type { Site } from '../types'

interface Props { site: Site | null; onClose: () => void }

export default function SiteDetailModal({ site, onClose }: Props) {
    return (
        <Modal
            title="Chi tiết điểm hiến máu"
            open={site !== null}
            onCancel={onClose}
            footer={null}
            width={640}
            destroyOnHidden
        >
            {site && (
                <>
                    <Descriptions column={1} size="small" bordered style={{ marginBottom: 16 }}>
                        <Descriptions.Item label="Tên điểm hiến">{site.siteName}</Descriptions.Item>
                        <Descriptions.Item label="Mã điểm">{site.siteCode}</Descriptions.Item>
                        <Descriptions.Item label="Loại">
                            <span className={`ds-tag ds-tag--${site.siteType}`}>{SITE_TYPE_LABEL[site.siteType]}</span>
                        </Descriptions.Item>
                        <Descriptions.Item label="Địa chỉ">{site.address}</Descriptions.Item>
                        <Descriptions.Item label="Số điện thoại">{site.phoneNumber}</Descriptions.Item>
                        <Descriptions.Item label="Sức chứa dự kiến">{site.totalCapacityPerDay} lượt/ngày</Descriptions.Item>
                        <Descriptions.Item label="Ngày kích hoạt">{fmtDateTime(site.createdAt)}</Descriptions.Item>
                    </Descriptions>
                    <ConfigCard
                        label="Cấu hình đang phát hành"
                        title="Khung giờ & sức chứa"
                        subtitle={`${site.totalCapacityPerDay} lượt/ngày`}
                        slots={site.slots}
                        highlight
                    />
                </>
            )}
        </Modal>
    )
}
