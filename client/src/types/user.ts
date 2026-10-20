export type RoleCode = 'DON' | 'BBNK' | 'CLIN' | 'MGT' | 'SYS'

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