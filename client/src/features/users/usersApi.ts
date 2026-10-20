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
            Object.entries(q).filter(([, v]) => v !== undefined && v !== ''),
        )
        const { data } = await axiosClient.get('/api/users', { params })
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

    updateStatus: (id: number, isActive: boolean) =>
        axiosClient.put(`/api/users/${id}/status`, { isActive }).then((r) => r.data),
}

/* ===== MOCK: BE chưa có API roles / departments. Thay bằng axios khi có. ===== */
// Phiên bản chưa dùng API roles
//TODO cập nhật API role
export const referenceApi = {
    roles: async (): Promise<RoleOption[]> => [
        { id: 1, code: 'SYS', name: 'Quản trị hệ thống' },
        { id: 2, code: 'MGT', name: 'Ban Giám đốc / Quản lý' },
        { id: 3, code: 'CLIN', name: 'Bác sĩ / Nhân viên Lâm sàng' },
        { id: 4, code: 'BBNK', name: 'Nhân viên Ngân hàng máu' },
        { id: 5, code: 'DON', name: 'Nhân viên Tiếp nhận & Lấy máu' },
    ],
    departments: async (): Promise<Department[]> => [
        { id: 1, name: 'Khoa Huyết học' },
        { id: 2, name: 'Khoa Cấp cứu' },
        { id: 3, name: 'Phòng CNTT' },
        { id: 4, name: 'Trung tâm Truyền máu' },
        { id: 5, name: 'Phòng Kế hoạch' },
        { id: 6, name: 'Khoa Hồi sức' },
    ],
}
