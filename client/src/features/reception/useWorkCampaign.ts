import { useEffect } from 'react'
import { App } from 'antd'
import type { AxiosError } from 'axios'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { useCampaignDetail } from '@/features/campaigns/hooks'
import { getCampaignBlockReason } from './campaignRules'
import { useWorkCampaignStore } from './workCampaignStore'

/**
 * Đợt hiến máu đang làm việc của nhân viên tiếp nhận.
 * - Chỉ bắt buộc với user có APPOINTMENTS_EDIT (DON); người chỉ xem (MGT, CLIN) thấy toàn bộ.
 * - Tự xác thực lại đợt đã lưu với server (F5, đợt bị hủy / hết hạn / không còn tồn tại).
 */
export function useWorkCampaign() {
    const { message } = App.useApp()
    const { can } = usePermission()
    const requireCampaign = can(PERMISSIONS.APPOINTMENTS_EDIT)
    const campaign = useWorkCampaignStore((s) => s.campaign)
    const clear = useWorkCampaignStore((s) => s.clear)

    const { data, error } = useCampaignDetail(requireCampaign ? (campaign?.id ?? null) : null)

    useEffect(() => {
        if (!data) return
        const reason = getCampaignBlockReason(data)
        if (reason) {
            clear()
            message.warning(reason)
        }
    }, [data, clear, message])

    useEffect(() => {
        if ((error as AxiosError | null)?.response?.status === 404) {
            clear()
            message.warning('Không còn tìm thấy đợt hiến máu đã chọn, vui lòng nhập lại mã.')
        }
    }, [error, clear, message])

    return {
        requireCampaign,
        campaign,
        campaignId: requireCampaign ? campaign?.id : undefined,
        /** true = phải nhập mã đợt trước khi xem dữ liệu */
        blocked: requireCampaign && !campaign,
        clear,
    }
}