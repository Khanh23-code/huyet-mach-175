export type RoleCode = 'DON' | 'BBNK' | 'CLIN' | 'MGT' | 'SYS'

export interface UserItem {
    id: number
    staffCode: string
    username: string
    fullName: string
    departmentName: string
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
