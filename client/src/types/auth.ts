export interface LoginRequest {
    username?: string
    staffCode?: string
    password: string
}

export interface RefreshTokenRequest {
    refreshToken: string
}

export interface AuthUser {
    id: string
    username: string
    fullName?: string
    roles: string[]
    permissions?: string[]
}

export interface AuthTokens {
    accessToken: string
    refreshToken: string
}

export type LoginResponse = AuthTokens & { user?: AuthUser }

/* ------------------------------------------------------------------ */
/* Hồ sơ cá nhân                                                       */
/* ------------------------------------------------------------------ */

/**
 * GET /api/Auth/me hiện chỉ trả id, username, fullName, roles.
 * Các field còn lại là TODO backend -> tạm lấy từ mock (xem features/account/mock.ts).
 */
export interface UserProfile extends AuthUser {
    staffCode?: string
    departmentName?: string
    email?: string | null
    phoneNumber?: string | null
    hireDate?: string | null // ISO date
    isActive?: boolean
}

export interface UpdateProfileRequest {
    fullName: string
    email: string | null
    phoneNumber: string | null
}

export interface ChangePasswordRequest {
    currentPassword: string
    newPassword: string
    confirmNewPassword: string
}

export type DeviceType = 'desktop' | 'mobile' | 'tablet'

/** TODO backend: GET /api/Auth/sessions */
export interface LoginSession {
    id: string
    browser: string
    os: string
    deviceType: DeviceType
    ipAddress: string
    location: string
    lastActiveAt: string // ISO datetime
    isCurrent: boolean
}