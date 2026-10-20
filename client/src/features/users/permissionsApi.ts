import { axiosClient } from '@/services/axiosClient'

export interface PermissionDto {
    permissionId: number
    permissionCode: string
    permissionName: string
    moduleCode: string
    moduleName: string
    actionType: string
    description: string | null
    isAssigned: boolean
}

export interface RolePermissionsDto {
    roleId: number
    roleCode: string
    roleName: string
    description: string | null
    isActive: boolean
    usersCount: number
    assignedPermissionIds: number[]
    permissions: PermissionDto[]
}

export interface UpdateRolePermissionsRequest {
    permissionIds: number[]
}

/** BE tự sinh permissionCode = {MODULE}_{ACTION}. */
export interface CreatePermissionRequest {
    moduleCode: string
    /** Bắt buộc khi module chưa tồn tại; module đã có thì BE dùng lại tên cũ. */
    moduleName?: string
    /** VIEW | EDIT | APPROVE | CANCEL (enum PermissionAction) */
    actionType: string
    permissionName: string
    description?: string
}

export const permissionsApi = {
    getAll: async (): Promise<PermissionDto[]> => {
        const { data } = await axiosClient.get('/api/permissions')
        return data
    },

    /** TODO BE: POST /api/permissions (xem file BE handoff) */
    create: async (body: CreatePermissionRequest): Promise<PermissionDto> => {
        const { data } = await axiosClient.post('/api/permissions', body)
        return data
    },

    getByRole: async (
        roleId: number,
    ): Promise<RolePermissionsDto> => {
        const { data } = await axiosClient.get(
            `/api/roles/${roleId}/permissions`,
        )
        return data
    },

    updateByRole: async (
        roleId: number,
        body: UpdateRolePermissionsRequest,
    ): Promise<RolePermissionsDto> => {
        const { data } = await axiosClient.put(
            `/api/roles/${roleId}/permissions`,
            body,
        )
        return data
    },
}