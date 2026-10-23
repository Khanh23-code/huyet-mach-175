/**
 * MẪU PHÂN QUYỀN MẶC ĐỊNH THEO ROLE  (chỉ là gợi ý cho nút "Áp dụng mẫu mặc định")
 *
 * ĐÂY LÀ CHỖ DUY NHẤT CẦN SỬA khi nghiệp vụ thay đổi.
 * Mỗi phần tử là 1 pattern khớp với `permissionCode` (dạng MODULE_ACTION):
 *
 *   'DONORS_*'          mọi thao tác của module DONORS
 *   '*_VIEW'            thao tác VIEW của mọi module
 *   'CAMPAIGN_APPROVE'  đúng 1 quyền
 *   '*'                 tất cả
 *   '!*_CANCEL'         loại trừ (tiền tố !), được áp dụng sau cùng
 *
 * Module/quyền mới thêm vào DB sẽ tự khớp nếu pattern phù hợp,
 * nên không phải sửa chỗ nào khác ở FE.
 *
 * Mẫu bên dưới là GIẢ ĐỊNH, hãy chỉnh theo nghiệp vụ thật của nhóm.
 */
export const ROLE_PRESETS: Record<string, string[]> = {
    SYS: ['*'],
    MGT: ['*_VIEW', 'CAMPAIGN_APPROVE', 'CAMPAIGN_CANCEL', '!USERS_*', '!ROLES_*'],
    CLIN: ['DONORS_VIEW', 'SCREENING_*'],
    BBNK: ['CAMPAIGN_VIEW', 'DONORS_VIEW'],
    DON: ['CAMPAIGN_VIEW', 'DONORS_*', 'APPOINTMENTS_*', 'SURVEY_*', 'SCREENING_*'],
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
    permissions: { permissionId: number; permissionCode: string }[],
): Set<number> | null {
    const patterns = ROLE_PRESETS[roleCode]
    if (!patterns) return null

    const include = patterns.filter((p) => !p.startsWith('!')).map(toRegExp)
    const exclude = patterns
        .filter((p) => p.startsWith('!'))
        .map((p) => toRegExp(p.slice(1)))

    const ids = new Set<number>()
    for (const p of permissions) {
        const matched = include.some((r) => r.test(p.permissionCode))
        const excluded = exclude.some((r) => r.test(p.permissionCode))
        if (matched && !excluded) ids.add(p.permissionId)
    }
    return ids
}