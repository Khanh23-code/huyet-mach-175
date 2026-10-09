export interface LoginRequest {
    username: string
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
}

export interface AuthTokens {
    accessToken: string
    refreshToken: string
}

export type LoginResponse = AuthTokens & { user?: AuthUser }