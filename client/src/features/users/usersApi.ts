import { axiosClient } from '@/services/axiosClient'
import type {
    CreateUserRequest, Department, PagedResult, RoleCode, RoleOption, UserItem, UserQuery,
} from '@/types/user'

/* Response GET /api/users chưa rõ shape nên chuẩn hóa ở đây.
   Khi có response thật, chỉ cần sửa normalizeUser và phần đọc items/total trong list(). */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const normalizeUser = (raw: any): UserItem => ({
    id: raw.id ?? raw.userId,
    staffCode: raw.staffCode ?? raw.username ?? '',
    username: raw.username ?? '',
    fullName: raw.fullName ?? '',
    departmentName: raw.departmentName ?? raw.department?.departmentName ?? raw.department?.name ?? '',
    isActive: raw.isActive ?? true,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    roles: ((raw.roles ?? raw.roleCodes ?? []) as any[]).map((r) =>
        typeof r === 'string' ? r : (r.roleCode ?? r.code),
    ) as RoleCode[],
})

export const usersApi = {
    list: async (q: UserQuery): Promise<PagedResult<UserItem>> => {
        const params = Object.fromEntries(
            Object.entries(q).filter(
                ([, v]) => v !== undefined && v !== '' && (!Array.isArray(v) || v.length > 0),
            ),
        )
        const { data } = await axiosClient.get('/api/users', {
            params,
            paramsSerializer: {
                indexes: null,
            },
        })
        const rawItems = Array.isArray(data) ? data : (data.items ?? data.data ?? data.results ?? [])
        return {
            items: rawItems.map(normalizeUser),
            total: Array.isArray(data)
                ? data.length
                : (data.totalCount ?? data.total ?? data.totalItems ?? rawItems.length),
        }
    },

    create: (body: CreateUserRequest) =>
        axiosClient.post('/api/users', body).then((r) => r.data),

    // BE là PATCH /api/users/{id}/status (bản cũ dùng PUT -> 405).
    updateStatus: (id: number, isActive: boolean) =>
        axiosClient.patch(`/api/users/${id}/status`, { isActive }).then((r) => r.data),
}

export const referenceApi = {
    roles: async (): Promise<RoleOption[]> => {
        const { data } = await axiosClient.get('/api/roles')

        return data.map((r: {
            roleId: number
            roleCode: RoleOption['code']
            roleName: string
            isActive: boolean
        }): RoleOption => ({
            id: r.roleId,
            code: r.roleCode,
            name: r.roleName,
            isActive: r.isActive,
        }))
    },

    /**
     * Cần BE có GET /api/departments (xem file BE handoff).
     * Bản mock cũ có id/tên KHÔNG khớp DB seed nên đã bỏ để tránh gán sai khoa/phòng.
     */
    departments: async (): Promise<Department[]> => {
        const { data } = await axiosClient.get('/api/departments')

        return data.map((d: { departmentId: number; departmentName: string }): Department => ({
            id: d.departmentId,
            name: d.departmentName,
        }))
    },
}
