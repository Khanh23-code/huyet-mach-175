import type { ReactNode } from 'react'
import {
    CalendarOutlined,
    HeartOutlined,
    SettingOutlined,
    TeamOutlined,
} from '@ant-design/icons'
import { PERMISSIONS, type PermissionRequirement } from './permissions'

export interface RouteItem {
    key: string
    label: string
    path: string
    requiredPermission?: PermissionRequirement
}

export interface RouteGroup {
    key: string
    label: string
    icon: ReactNode
    /** Mở sẵn khi vào trang. */
    defaultOpen?: boolean
    /** Điều kiện riêng cho cả nhóm (tùy chọn, ngoài điều kiện của từng mục con). */
    requiredPermission?: PermissionRequirement
    children: RouteItem[]
}

export const HOME_PATH = '/'

export const ROUTE_GROUPS: RouteGroup[] = [
    {
        key: 'account',
        label: 'Tài khoản',
        icon: <TeamOutlined />,
        children: [
            { key: 'profile', label: 'Hồ sơ cá nhân', path: '/account/profile' },
            { key: 'notifications', label: 'Trung tâm thông báo', path: '/account/notifications' },
        ],
    },
    {
        // W1: Tiếp đón & Sàng lọc
        key: 'reception',
        label: 'Tiếp nhận & Sàng lọc',
        icon: <HeartOutlined />,
        defaultOpen: true,
        children: [
            { key: 'queue', label: 'Hàng chờ tiếp nhận', path: '/btd/queue', requiredPermission: PERMISSIONS.APPOINTMENTS_VIEW },
            { key: 'screening', label: 'Khám sàng lọc', path: '/btd/screening', requiredPermission: PERMISSIONS.SCREENING_VIEW },
        ],
    },
    {
        key: 'campaigns',
        label: 'Điểm hiến & Lịch slot',
        icon: <CalendarOutlined />,
        defaultOpen: true,
        children: [
            // MGT / BBNK / DON: theo dõi điểm hiến; MGT thấy thêm khu vực duyệt đề xuất
            { key: 'site-list', label: 'Điểm hiến máu & Lịch slot', path: '/campaigns/sites', requiredPermission: PERMISSIONS.CAMPAIGN_VIEW },
            // BBNK: tạo / gửi đề xuất
            { key: 'site-proposals', label: 'Đề xuất lịch & điểm hiến', path: '/campaigns/proposals', requiredPermission: PERMISSIONS.CAMPAIGN_EDIT },
        ],
    },
    {
        key: 'system',
        label: 'Quản trị hệ thống',
        icon: <SettingOutlined />,
        defaultOpen: true,
        children: [
            { key: 'users', label: 'Người dùng & Phân quyền', path: '/admin/users', requiredPermission: PERMISSIONS.USERS_VIEW },
            // TODO: audit-logs, parameters, donation-sites, integrations: thêm khi có trang + mã quyền
        ],
    },
]

/** Lấy 1 mục theo key (dùng trong router để gắn `requiredPermission`). */
export function findRouteItem(key: string): RouteItem {
    for (const group of ROUTE_GROUPS) {
        const item = group.children.find((c) => c.key === key)
        if (item) return item
    }
    throw new Error(`routeConfig: không có route với key "${key}"`)
}

/** Lọc menu theo quyền; nhóm không còn mục con nào thì bị loại. */
export function filterRouteGroups(
    groups: RouteGroup[],
    canAccess: (required?: PermissionRequirement) => boolean,
): RouteGroup[] {
    return groups
        .filter((g) => canAccess(g.requiredPermission))
        .map((g) => ({ ...g, children: g.children.filter((c) => canAccess(c.requiredPermission)) }))
        .filter((g) => g.children.length > 0)
}