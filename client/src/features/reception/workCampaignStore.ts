import { create } from 'zustand'
import { createJSONStorage, persist } from 'zustand/middleware'

/** Đợt hiến máu mà nhân viên tiếp nhận đang làm việc (lưu theo tab, mất khi đóng tab hoặc đăng xuất). */
export interface WorkCampaign {
    id: number
    name: string
    locationName: string
}

interface WorkCampaignState {
    campaign: WorkCampaign | null
    setCampaign: (campaign: WorkCampaign) => void
    clear: () => void
}

export const useWorkCampaignStore = create<WorkCampaignState>()(
    persist(
        (set) => ({
            campaign: null,
            setCampaign: (campaign) => set({ campaign }),
            clear: () => set({ campaign: null }),
        }),
        {
            name: 'work-campaign',
            storage: createJSONStorage(() => sessionStorage),
        },
    ),
)