import type { DeviceType, LoginSession } from '@/types/auth'

/** Đoán trình duyệt / HĐH của máy hiện tại từ User-Agent (IP và vị trí phải do backend trả). */
export function detectCurrentDevice(): Pick<LoginSession, 'browser' | 'os' | 'deviceType'> {
    const ua = navigator.userAgent

    const browser = /Edg\//.test(ua) ? 'Edge'
        : /OPR\//.test(ua) ? 'Opera'
        : /Firefox\//.test(ua) ? 'Firefox'
        : /Chrome\//.test(ua) ? 'Chrome'
        : /Safari\//.test(ua) ? 'Safari'
        : 'Trình duyệt'

    // Windows 10 và 11 đều báo "Windows NT 10.0" nên không phân biệt được
    const os = /Windows/.test(ua) ? 'Windows'
        : /iPhone|iPad|iPod/.test(ua) ? 'iOS'
        : /Android/.test(ua) ? 'Android'
        : /Mac OS X/.test(ua) ? 'macOS'
        : /Linux/.test(ua) ? 'Linux'
        : 'Không rõ'

    const deviceType: DeviceType = /iPad|Tablet/.test(ua) ? 'tablet'
        : /iPhone|Android.*Mobile|Mobile/.test(ua) ? 'mobile'
        : 'desktop'

    return { browser, os, deviceType }
}

const pad = (n: number) => String(n).padStart(2, '0')

/** "07:34 hôm nay" | "20:10 hôm qua" | "14:22, 01/06/2025" */
export function formatLoginTime(iso: string): string {
    const d = new Date(iso)
    const time = `${pad(d.getHours())}:${pad(d.getMinutes())}`
    const startOfDay = (x: Date) => new Date(x.getFullYear(), x.getMonth(), x.getDate()).getTime()
    const diffDays = Math.round((startOfDay(new Date()) - startOfDay(d)) / 86_400_000)

    if (diffDays === 0) return `${time} hôm nay`
    if (diffDays === 1) return `${time} hôm qua`
    return `${time}, ${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`
}

/** "2020-01-07" -> "07/01/2020" */
export function formatDate(iso?: string | null): string {
    if (!iso) return '—'
    const d = new Date(iso)
    if (Number.isNaN(d.getTime())) return '—'
    return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`
}
