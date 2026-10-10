import type { ReactNode } from 'react'
import {
    HeartOutlined,
    SettingOutlined,
    TeamOutlined,
} from '@ant-design/icons'

/* ------------------------------------------------------------------ */
/* Types                                                               */
/* ------------------------------------------------------------------ */

export type RoleCode = 'SYS' | 'MGT' | 'CLIN' | 'BBNK' | 'DON'

export interface NavChild {
    key: string
    label: string
    path: string
}

export interface NavGroup {
    key: string
    /** Mã phân hệ hiển thị nhỏ bên trái (W0, W1, W6...) */
    code: string
    label: string
    icon: ReactNode
    children: NavChild[]
}

export interface RoleNav {
    home: { label: string; path: string }
    groups: NavGroup[]
}

/* ------------------------------------------------------------------ */
/* Role metadata                                                       */
/* ------------------------------------------------------------------ */

/** Thứ tự ưu tiên khi 1 user có nhiều role: role đứng trước thắng. */
export const ROLE_PRIORITY: RoleCode[] = [ 'SYS', 'MGT', 'CLIN', 'BBNK', 'DON', ]
export const ROLE_META: Record< RoleCode, { prefix: string; label: string } > = {
    SYS: { prefix: 'SYS', label: 'Quản trị hệ thống' },
    MGT: { prefix: 'MGT', label: 'Ban giám đốc, Quản lý' },
    CLIN: { prefix: 'CLIN', label: 'Bác sĩ, Nhân viên lâm sàng' },
    BBNK: { prefix: 'BBNK', label: 'Nhân viên ngân hàng máu' },
    DON: { prefix: 'DON', label: 'Nhân viên tiếp nhận và lấy máu' },
}

export function resolveRole( roles: string[] | undefined, ): RoleCode | undefined { if (!roles?.length) return undefined
    const normalizedRoles = roles.map((role) => role.trim().toUpperCase())
    return ROLE_PRIORITY.find((role) => normalizedRoles.includes(role), ) }

/* ------------------------------------------------------------------ */
/* Nhóm dùng chung cho mọi role                                        */
/* ------------------------------------------------------------------ */

export const ACCOUNT_GROUP: NavGroup = {
    key: 'account',
    code: 'W0',
    label: 'Tài khoản',
    icon: <TeamOutlined />,
    children: [
        { key: 'profile', label: 'Hồ sơ cá nhân', path: '/account/profile' },
        { key: 'notifications', label: 'Trung tâm thông báo', path: '/account/notifications' },
    ],
}

/* ------------------------------------------------------------------ */
/* Menu riêng theo role (làm AD trước, các role khác thêm dần)         */
/* ------------------------------------------------------------------ */

const ROLE_NAV: Partial<Record<RoleCode, RoleNav>> = {
    SYS: {
        home: { label: 'Trang chủ AD', path: '/admin' },
        groups: [
            {
                key: 'system',
                code: 'W6',
                label: 'Quản trị hệ thống',
                icon: <SettingOutlined />,
                children: [
                    { key: 'users', label: 'Người dùng & Phân quyền', path: '/admin/users' },
                    { key: 'audit-logs', label: 'Nhật ký hệ thống', path: '/admin/audit-logs' },
                    { key: 'parameters', label: 'Danh mục tham số', path: '/admin/parameters' },
                    { key: 'donation-sites', label: 'Điểm hiến máu & Lịch slot', path: '/admin/donation-sites' },
                    { key: 'integrations', label: 'Giám sát HIS/LIS/EMR', path: '/admin/integrations' },
                ],
            },
        ],
    },

    DON: {
        home: { label: 'Trang chủ BTD', path: '/btd' },
        groups: [
            {
                key: 'reception',
                code: 'W1',
                label: 'Tiếp nhận & Sàng lọc',
                icon: <HeartOutlined />,
                children: [
                    { key: 'queue', label: 'Hàng chờ tiếp nhận', path: '/btd/queue' },
                    { key: 'screening', label: 'Khám sàng lọc', path: '/btd/screening' },
                    { key: 'sessions', label: 'Phiên lấy máu', path: '/btd/sessions' },
                ],
            },
        ],
    },

    // TODO: QL, BBNK, LS — copy cấu trúc của AD/BTD rồi đổi nội dung.
}

/* ------------------------------------------------------------------ */
/* Public API                                                          */
/* ------------------------------------------------------------------ */

export interface ResolvedNav extends RoleNav {
    role: RoleCode
}

/** Trả về menu cho user: [Tài khoản, ...nhóm riêng của role]. */
export function getNavForRoles(roles: string[] | undefined): ResolvedNav | null {
    const role = resolveRole(roles)
    if (!role) return null

    const roleNav = ROLE_NAV[role]
    if (!roleNav) {
        return { role, home: { label: `Trang chủ ${role}`, path: '/' }, groups: [ACCOUNT_GROUP] }
    }
    return { role, home: roleNav.home, groups: [ACCOUNT_GROUP, ...roleNav.groups] }
}
