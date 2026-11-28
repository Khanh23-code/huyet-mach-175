const MODULE_LABELS: Record<string, string> = {
    Clinical: 'Lâm sàng & Điều phối',
    Inventory: 'Kho & Xét nghiệm',
    Donation: 'Hiến máu',
    Auth: 'Tài khoản & Phân quyền',
    Notifications: 'Hệ thống',
}

export function getModuleLabelVi(sourceModule?: string | null): string {
    if (!sourceModule) return 'Hệ thống'
    return MODULE_LABELS[sourceModule] ?? 'Hệ thống'
}

export function formatFullDateTimeVi(iso?: string | null): string {
    if (!iso) return ''
    const date = new Date(iso)
    if (Number.isNaN(date.getTime())) return ''

    const pad = (n: number) => String(n).padStart(2, '0')
    return `${pad(date.getHours())}:${pad(date.getMinutes())}, ${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()}`
}

export function formatRelativeTimeVi(iso?: string | null, nowMs = Date.now()): string {
    if (!iso) return ''
    const date = new Date(iso)
    const timeMs = date.getTime()
    if (Number.isNaN(timeMs)) return ''

    const diffSec = Math.max(0, Math.floor((nowMs - timeMs) / 1000))
    if (diffSec < 60) return 'Vừa xong'

    const diffMin = Math.floor(diffSec / 60)
    if (diffMin < 60) return `${diffMin} phút trước`

    const diffHour = Math.floor(diffMin / 60)
    if (diffHour < 24) return `${diffHour} giờ trước`

    const diffDay = Math.floor(diffHour / 24)
    if (diffDay <= 7) return `${diffDay} ngày trước`

    return formatFullDateTimeVi(iso)
}
