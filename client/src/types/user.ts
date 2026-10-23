/** Mã vai trò do BE trả về. Admin có thể tạo vai trò mới nên không giới hạn bằng union. */
export type RoleCode = string

export interface UserItem {
    id: number
    staffCode: string
    username: string
    fullName: string
    departmentId: number | null
    departmentName: string
    email: string | null
    phoneNumber: string | null
    roles: RoleCode[]
    isActive: boolean
}

export interface PagedResult<T> {
    items: T[]
    total: number
}

export interface UserQuery {
    keyword?: string
    departmentId?: number
    roleCode?: RoleCode
    roleCodes?: RoleCode[]
    isActive?: boolean
    pageNumber: number
    pageSize: number
}

export interface CreateUserRequest {
    staffCode: string
    username: string
    fullName: string
    password: string
    email: string | null
    phoneNumber: string | null
    departmentId: number
    roleIds: number[]
}

/** Khớp UpdateUserRequest của BE (PUT /api/users/{id}). */
export interface UpdateUserRequest {
    fullName: string
    email: string | null
    phoneNumber: string | null
    departmentId: number
}

export interface Department {
    id: number
    name: string
}

export interface RoleOption {
    id: number
    code: RoleCode
    name: string
    isActive: boolean
}