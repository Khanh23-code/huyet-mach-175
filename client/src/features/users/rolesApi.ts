import { axiosClient } from '@/services/axiosClient'

export interface CreateRoleRequest {
    roleCode: string
    roleName: string
    description: string | null
}

/** Khớp RoleDto của BE (POST /api/roles trả về dạng này). */
export interface RoleDto {
    roleId: number
    roleCode: string
    roleName: string
    description: string | null
    isActive: boolean
    usersCount: number
}

export const rolesApi = {
    create: async (body: CreateRoleRequest): Promise<RoleDto> => {
        const { data } = await axiosClient.post('/api/roles', body)
        return data
    },
}