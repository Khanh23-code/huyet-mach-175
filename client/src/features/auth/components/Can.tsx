import type { ReactNode } from 'react'
import { usePermission } from '../usePermission'
import type { PermissionRequirement } from '@/config/permissions'

interface CanProps {
    /** Mã quyền cần có. Mảng = cần ÍT NHẤT 1 quyền (hoặc TẤT CẢ nếu bật `all`). */
    do: PermissionRequirement
    /** Với mảng quyền: yêu cầu có TOÀN BỘ thay vì chỉ 1. */
    all?: boolean
    /** Nội dung thay thế khi không có quyền. Mặc định: không render gì. */
    fallback?: ReactNode
    children: ReactNode
}

/**
 * <Can do="CAMPAIGN_APPROVE"><Button>Duyệt</Button></Can>
 * <Can do={['USERS_EDIT', 'ROLES_EDIT']} all fallback={<span>Chỉ xem</span>}>...</Can>
 */
export default function Can({ do: required, all = false, fallback = null, children }: CanProps) {
    const { canAccess, canAll } = usePermission()
    const allowed = all && typeof required !== 'string' ? canAll(required) : canAccess(required)

    return <>{allowed ? children : fallback}</>
}