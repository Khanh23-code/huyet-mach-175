import { Breadcrumb, Button, Result, Space } from 'antd'
import {
    ArrowLeftOutlined,
    HomeOutlined,
    TeamOutlined,
    ToolOutlined,
} from '@ant-design/icons'
import { useLocation, useNavigate } from 'react-router-dom'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import './NotFoundPage.css'

interface KnownModuleInfo {
    name: string
    group: string
    description: string
}

const KNOWN_MODULES: Record<string, KnownModuleInfo> = {
    '/admin/audit-logs': {
        name: 'Nhật ký hệ thống',
        group: 'Quản trị hệ thống',
        description: 'Phân hệ kiểm toán biến động, lưu vết thao tác người dùng và nhật ký an toàn thông tin.',
    },
    '/admin/parameters': {
        name: 'Danh mục tham số',
        group: 'Quản trị hệ thống',
        description: 'Cấu hình tham số hệ thống, ngưỡng cảnh báo kho máu và quy tắc nghiệp vụ.',
    },
    '/admin/donation-sites': {
        name: 'Điểm hiến máu & Lịch slot',
        group: 'Quản trị hệ thống',
        description: 'Quản lý danh sách các điểm hiến máu lưu động và kế hoạch khung giờ tiếp nhận.',
    },
    '/admin/integrations': {
        name: 'Giám sát HIS/LIS/EMR',
        group: 'Quản trị hệ thống',
        description: 'Giám sát tích hợp kết nối đồng bộ dữ liệu với hệ sinh thái Bệnh viện Quân y 175.',
    },
    '/account/notifications': {
        name: 'Trung tâm thông báo',
        group: 'Tài khoản',
        description: 'Hộp thư thông báo biến động y lệnh, phân quyền và cảnh báo nội bộ.',
    },
    '/btd/queue': {
        name: 'Hàng chờ tiếp nhận',
        group: 'Tiếp nhận & Sàng lọc',
        description: 'Điều phối luồng người hiến máu và tiếp nhận hồ sơ ban đầu.',
    },
    '/btd/screening': {
        name: 'Khám sàng lọc',
        group: 'Tiếp nhận & Sàng lọc',
        description: 'Khám lâm sàng, xét nghiệm nhanh và đánh giá tiêu chuẩn người hiến máu.',
    },
    '/btd/sessions': {
        name: 'Phiên lấy máu',
        group: 'Tiếp nhận & Sàng lọc',
        description: 'Theo dõi quy trình lấy máu, dán mã barcode và bàn giao túi máu.',
    },
}

export default function NotFoundPage() {
    const location = useLocation()
    const navigate = useNavigate()
    const { can } = usePermission()

    const pathname = location.pathname.replace(/\/+$/, '')
    const known = KNOWN_MODULES[pathname]

    const isUnderDevelopment = !!known || pathname.startsWith('/admin') || pathname.startsWith('/btd')

    return (
        <div className="not-found-page">
            <Breadcrumb
                items={[
                    { title: 'Hệ thống' },
                    { title: known ? known.group : 'Điều hướng' },
                    { title: known ? known.name : isUnderDevelopment ? 'Tính năng đang phát triển' : '404' },
                ]}
            />

            <div className="not-found-card">
                <Result
                    icon={
                        isUnderDevelopment ? (
                            <div className="not-found-icon not-found-icon--dev">
                                <ToolOutlined />
                            </div>
                        ) : undefined
                    }
                    status={isUnderDevelopment ? 'info' : '404'}
                    title={
                        isUnderDevelopment
                            ? `${known?.name ?? 'Tính năng'} đang được phát triển`
                            : '404 - Không tìm thấy trang'
                    }
                    subTitle={
                        isUnderDevelopment
                            ? 'Màn hình hiện đang trong quá trình phát triển hoàn thiện giao diện.'
                            : 'Đường dẫn bạn yêu cầu không tồn tại.'
                    }
                    extra={
                        <Space wrap size="middle">
                            <Button icon={<ArrowLeftOutlined />} onClick={() => navigate(-1)}>
                                Quay lại trang trước
                            </Button>
                            {can(PERMISSIONS.USERS_VIEW) && (
                                <Button
                                    type="primary"
                                    icon={<TeamOutlined />}
                                    onClick={() => navigate('/admin/users')}
                                >
                                    Quản lý Người dùng & Phân quyền
                                </Button>
                            )}
                            <Button icon={<HomeOutlined />} onClick={() => navigate('/')}>
                                Về trang chủ
                            </Button>
                        </Space>
                    }
                >
                </Result>
            </div>
        </div>
    )
}

