import { z } from 'zod'

const TIME_RE = /^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/

export const slotSchema = z.object({
    startTime: z.string().regex(TIME_RE, 'Giờ bắt đầu không hợp lệ (HH:mm)'),
    endTime: z.string().regex(TIME_RE, 'Giờ kết thúc không hợp lệ (HH:mm)'),
    capacity: z
        .number({ message: 'Vui lòng nhập sức chứa' })
        .int('Sức chứa phải là số nguyên')
        .min(1, 'Sức chứa tối thiểu là 1')
        .max(1000, 'Sức chứa tối đa 1.000'),
})

export const toMinutes = (t: string) => {
    if (!TIME_RE.test(t)) return null
    const parts = t.split(':').map(Number)
    return parts[0] * 60 + parts[1]
}

export const campaignSchema = z
    .object({
        campaignName: z
            .string({ message: 'Vui lòng nhập tên chiến dịch' })
            .trim()
            .min(1, 'Vui lòng nhập tên chiến dịch')
            .max(150, 'Tên chiến dịch tối đa 150 ký tự'),
        description: z.string().trim().optional().or(z.literal('')),
        locationName: z
            .string({ message: 'Vui lòng nhập địa điểm tổ chức' })
            .trim()
            .min(1, 'Vui lòng nhập địa điểm tổ chức')
            .max(255, 'Địa điểm tối đa 255 ký tự'),
        campaignType: z.enum(['EVENT', 'RECURRING']),
        operatingDays: z.string().trim().optional().or(z.literal('')),
        startDate: z.string().min(1, 'Vui lòng chọn ngày bắt đầu'),
        endDate: z.string().min(1, 'Vui lòng chọn ngày kết thúc'),
        targetDonations: z
            .number({ message: 'Vui lòng nhập chỉ tiêu thu gom' })
            .int('Chỉ tiêu phải là số nguyên')
            .min(1, 'Chỉ tiêu tối thiểu là 1 túi máu')
            .max(10000, 'Chỉ tiêu tối đa 10.000 túi máu'),
        maxCapacity: z
            .number({ message: 'Sức chứa phải là số' })
            .int('Sức chứa tối đa phải là số nguyên')
            .min(1, 'Sức chứa tối đa phải lớn hơn 0')
            .optional()
            .nullable(),
        slots: z.array(slotSchema),
    })
    .superRefine((v, ctx) => {
        // Validate RECURRING operatingDays
        if (v.campaignType === 'RECURRING' && (!v.operatingDays || v.operatingDays.trim().length === 0)) {
            ctx.addIssue({
                code: 'custom',
                path: ['operatingDays'],
                message: 'Vui lòng chọn các ngày hoạt động trong tuần cho điểm hiến định kỳ',
            })
        }

        // Validate date range
        if (v.startDate && v.endDate && v.startDate > v.endDate) {
            ctx.addIssue({
                code: 'custom',
                path: ['endDate'],
                message: 'Ngày kết thúc phải sau hoặc bằng ngày bắt đầu',
            })
        }

        // Validate maxCapacity >= targetDonations if present
        if (v.maxCapacity != null && v.maxCapacity < v.targetDonations) {
            ctx.addIssue({
                code: 'custom',
                path: ['maxCapacity'],
                message: 'Sức chứa tối đa phải lớn hơn hoặc bằng chỉ tiêu thu gom',
            })
        }

        // Validate slots
        if (!v.slots || v.slots.length === 0) {
            ctx.addIssue({
                code: 'custom',
                path: ['slots'],
                message: 'Cần thiết lập ít nhất 1 khung giờ hiến máu',
            })
            return
        }

        if (v.slots.length > 12) {
            ctx.addIssue({
                code: 'custom',
                path: ['slots'],
                message: 'Tối đa 12 khung giờ',
            })
        }

        // Validate start time < end time and no overlaps
        const validSlots: { i: number; s: number; e: number }[] = []
        v.slots.forEach((slot, i) => {
            const s = toMinutes(slot.startTime)
            const e = toMinutes(slot.endTime)
            if (s === null || e === null) return

            if (e <= s) {
                ctx.addIssue({
                    code: 'custom',
                    path: ['slots', i, 'endTime'],
                    message: 'Giờ kết thúc phải sau giờ bắt đầu',
                })
            } else {
                validSlots.push({ i, s, e })
            }
        })

        // Sort by start time to check overlap
        validSlots.sort((a, b) => a.s - b.s)
        for (let k = 1; k < validSlots.length; k++) {
            if (validSlots[k].s < validSlots[k - 1].e) {
                ctx.addIssue({
                    code: 'custom',
                    path: ['slots', validSlots[k].i, 'startTime'],
                    message: 'Khung giờ này bị chồng lấn với khung giờ khác',
                })
            }
        }
    })

export type CampaignFormValues = z.infer<typeof campaignSchema>

