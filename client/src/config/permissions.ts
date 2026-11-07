export const PERMISSIONS = {
    // Đợt hiến máu
    CAMPAIGN_VIEW: 'CAMPAIGN_VIEW',
    CAMPAIGN_EDIT: 'CAMPAIGN_EDIT',
    CAMPAIGN_APPROVE: 'CAMPAIGN_APPROVE',
    CAMPAIGN_CANCEL: 'CAMPAIGN_CANCEL',
    // W1: Tiếp đón & Sàng lọc
    DONORS_VIEW: 'DONORS_VIEW',
    DONORS_EDIT: 'DONORS_EDIT',
    APPOINTMENTS_VIEW: 'APPOINTMENTS_VIEW',
    APPOINTMENTS_EDIT: 'APPOINTMENTS_EDIT',
    APPOINTMENTS_CANCEL: 'APPOINTMENTS_CANCEL',
    SURVEY_VIEW: 'SURVEY_VIEW',
    SURVEY_EDIT: 'SURVEY_EDIT',
    SCREENING_VIEW: 'SCREENING_VIEW',
    SCREENING_EDIT: 'SCREENING_EDIT',
    // W1: Phiên lấy máu
    SESSION_VIEW: 'SESSION_VIEW',
    SESSION_EDIT: 'SESSION_EDIT',
<<<<<<< HEAD
=======
    // W2: Túi máu & Kho
    INVENTORY_VIEW: 'INVENTORY_VIEW',
    INVENTORY_EDIT: 'INVENTORY_EDIT',
    LAB_VIEW: 'LAB_VIEW',
    LAB_EDIT: 'LAB_EDIT',
    PROCESSING_VIEW: 'PROCESSING_VIEW',
    PROCESSING_EDIT: 'PROCESSING_EDIT',
    DISPOSAL_VIEW: 'DISPOSAL_VIEW',
    DISPOSAL_EDIT: 'DISPOSAL_EDIT',
    DISPOSAL_APPROVE: 'DISPOSAL_APPROVE',
>>>>>>> 18fd47d92a9274d24c20e3913cd951c12967d593
    // Quản trị hệ thống
    USERS_VIEW: 'USERS_VIEW',
    USERS_EDIT: 'USERS_EDIT',
    ROLES_VIEW: 'ROLES_VIEW',
    ROLES_EDIT: 'ROLES_EDIT',
} as const

/**
 * Điều kiện quyền:
 *  - `undefined`        -> không yêu cầu quyền
 *  - `'X_VIEW'`         -> phải có quyền đó
 *  - `['A', 'B']`       -> có ÍT NHẤT 1 quyền trong danh sách
 */
export type PermissionRequirement = string | readonly string[]