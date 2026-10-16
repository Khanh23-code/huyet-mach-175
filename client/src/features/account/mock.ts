import type { LoginSession, UserProfile } from '@/types/auth'
import { detectCurrentDevice } from './utils/device'

/**
 * ======================= MOCK DATA =======================
 * Bật/tắt từng phần khi backend xong API tương ứng.
 *
 *  USE_MOCK_PROFILE         GET /api/Auth/me chưa trả staffCode, departmentName, email,
 *                           phoneNumber, hireDate, isActive -> bổ sung từ MOCK_PROFILE_EXTRA
 *  USE_MOCK_SESSIONS        chưa có GET /api/Auth/sessions, DELETE /api/Auth/sessions/{id}
 *  USE_MOCK_CHANGE_PASSWORD chưa có POST /api/Auth/change-password
 * =========================================================
 */
export const USE_MOCK_PROFILE = true
export const USE_MOCK_SESSIONS = true
export const USE_MOCK_CHANGE_PASSWORD = true

export const MOCK_PROFILE_EXTRA: Partial<UserProfile> = {
    staffCode: 'NV-175-0297',
    departmentName: 'Trung tâm CNTT',
    email: 'le.quoc.anh@bv175.vn',
    phoneNumber: '0918 202 297',
    hireDate: '2020-01-07',
    isActive: true,
}

const hoursAgo = (h: number) => new Date(Date.now() - h * 3_600_000).toISOString()

/** Danh sách "phiên khác" (mutable để mô phỏng thu hồi phiên). */
let otherSessions: LoginSession[] = [
    {
        id: 'mock-2', browser: 'Firefox', os: 'macOS Sonoma', deviceType: 'desktop',
        ipAddress: '10.175.4.85', location: 'Hà Nội', lastActiveAt: hoursAgo(20), isCurrent: false,
    },
    {
        id: 'mock-3', browser: 'Safari', os: 'iPhone 15', deviceType: 'mobile',
        ipAddress: '10.175.8.201', location: 'Hà Nội', lastActiveAt: '2025-06-01T14:22:00', isCurrent: false,
    },
]

export function mockGetSessions(): Promise<LoginSession[]> {
    const current: LoginSession = {
        id: 'mock-current',
        ...detectCurrentDevice(),
        ipAddress: '—', // chỉ backend biết IP
        location: '—',
        lastActiveAt: new Date().toISOString(),
        isCurrent: true,
    }
    return Promise.resolve([current, ...otherSessions])
}

export function mockRevokeSession(id: string): Promise<void> {
    otherSessions = otherSessions.filter((s) => s.id !== id)
    return new Promise((resolve) => setTimeout(resolve, 400))
}

export function mockChangePassword(): Promise<void> {
    otherSessions = [] // backend thật sẽ thu hồi mọi phiên khác
    return new Promise((resolve) => setTimeout(resolve, 700))
}
