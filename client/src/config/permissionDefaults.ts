export const ROLE_PRESETS: Record<string, string[]> = {
    SYS: ['*'],
    MGT: ['*_VIEW', 'CAMPAIGN_APPROVE', 'CAMPAIGN_CANCEL', 'DISPOSAL_APPROVE', '!USERS_*', '!ROLES_*'],
    CLIN: ['DONORS_VIEW', 'SCREENING_*', 'BLOOD_REQUEST_*', 'BLOOD_RETURN_*', '!*_APPROVE'],
    BBNK: [
        'CAMPAIGN_VIEW',
        'CAMPAIGN_EDIT',
        'DONORS_VIEW',
        'INVENTORY_*',
        'LAB_*',
        'PROCESSING_*',
        'DISPOSAL_VIEW',
        'DISPOSAL_EDIT',
        'BLOOD_REQUEST_VIEW',
        'BLOOD_REQUEST_APPROVE',
        'BLOOD_RETURN_VIEW',
        'BLOOD_RETURN_APPROVE',
    ],
    DON: ['CAMPAIGN_VIEW', 'DONORS_*', 'APPOINTMENTS_*', 'SURVEY_*', 'SCREENING_*', 'SESSION_*'],
}

/**
 * VÁ TẠM nhãn hiển thị do seed BE đang ghi sai.
 * Xóa entry tương ứng khi BE sửa DbInitializer.
 */
export const MODULE_LABEL_OVERRIDES: Record<string, { name?: string; description?: string }> = {
    SCREENING: { name: 'Khám sàng lọc', description: 'Quản lý khám sàng lọc lâm sàng' },
}

const toRegExp = (pattern: string): RegExp =>
    new RegExp(
        '^' + pattern.replace(/[.+?^${}()|[\]\\]/g, '\\$&').replace(/\*/g, '.*') + '$',
        'i',
    )

/**
 * Trả về tập permissionId theo mẫu của role, hoặc null nếu role chưa có mẫu.
 */
export function resolvePreset(
    roleCode: string,
    permissions: { permissionId: string; permissionCode: string }[],
): Set<string> | null {
    const patterns = ROLE_PRESETS[roleCode]
    if (!patterns) return null

    const include = patterns.filter((p) => !p.startsWith('!')).map(toRegExp)
    const exclude = patterns
        .filter((p) => p.startsWith('!'))
        .map((p) => toRegExp(p.slice(1)))

    const ids = new Set<string>()
    for (const p of permissions) {
        const matched = include.some((r) => r.test(p.permissionCode))
        const excluded = exclude.some((r) => r.test(p.permissionCode))
        if (matched && !excluded) ids.add(p.permissionId)
    }
    return ids
}