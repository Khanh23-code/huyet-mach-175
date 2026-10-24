import { z } from 'zod'
import type { ProposalType } from './types'

const TIME_RE = /^([01]\d|2[0-3]):[0-5]\d$/
const PHONE_RE = /^0\d{9}$/

export const slotSchema = z.object({
    startTime: z.string().regex(TIME_RE, 'Giờ không hợp lệ'),
    endTime: z.string().regex(TIME_RE, 'Giờ không hợp lệ'),
    capacity: z
        .number({ message: 'Nhập sức chứa' })
        .int('Phải là số nguyên')
        .min(1, 'Tối thiểu 1')
        .max(500, 'Tối đa 500'),
})

const baseShape = z.object({
    siteId: z.number().optional(),
    siteName: z.string().trim(),
    siteType: z.enum(['FIXED', 'MOBILE']),
    address: z.string().trim(),
    phoneNumber: z.string().trim(),
    slots: z.array(slotSchema),
})

export type ProposalFormValues = z.infer<typeof baseShape>

const toMinutes = (t: string) => {
    if (!TIME_RE.test(t)) return null
    const [h, m] = t.split(':').map(Number)
    return h * 60 + m
}

/** Bỏ khoảng trắng, dấu chấm, gạch ngang trước khi kiểm tra / gửi lên BE. */
export const normalizePhone = (v: string) => v.replace(/[\s.-]/g, '')

export const makeProposalSchema = (mode: ProposalType) =>
    baseShape.superRefine((v, ctx) => {
        if (mode === 'NEW_SITE') {
            if (!v.siteName) ctx.addIssue({ code: 'custom', path: ['siteName'], message: 'Vui lòng nhập tên điểm hiến' })
            else if (v.siteName.length > 150) ctx.addIssue({ code: 'custom', path: ['siteName'], message: 'Tối đa 150 ký tự' })

            if (!v.address) ctx.addIssue({ code: 'custom', path: ['address'], message: 'Vui lòng nhập địa chỉ đầy đủ' })
            else if (v.address.length > 255) ctx.addIssue({ code: 'custom', path: ['address'], message: 'Tối đa 255 ký tự' })

            if (!v.phoneNumber) ctx.addIssue({ code: 'custom', path: ['phoneNumber'], message: 'Vui lòng nhập số điện thoại' })
            else if (!PHONE_RE.test(normalizePhone(v.phoneNumber)))
                ctx.addIssue({ code: 'custom', path: ['phoneNumber'], message: 'Số điện thoại gồm 10 số, bắt đầu bằng 0' })
        } else if (v.siteId === undefined) {
            ctx.addIssue({ code: 'custom', path: ['siteId'], message: 'Vui lòng chọn điểm hiến' })
        }

        if (v.slots.length === 0) {
            ctx.addIssue({ code: 'custom', path: ['slots'], message: 'Cần ít nhất 1 khung giờ' })
            return
        }
        if (v.slots.length > 12) ctx.addIssue({ code: 'custom', path: ['slots'], message: 'Tối đa 12 khung giờ' })

        const valid: { i: number; s: number; e: number }[] = []
        v.slots.forEach((slot, i) => {
            const s = toMinutes(slot.startTime)
            const e = toMinutes(slot.endTime)
            if (s === null || e === null) return
            if (e <= s) ctx.addIssue({ code: 'custom', path: ['slots', i, 'endTime'], message: 'Phải sau giờ bắt đầu' })
            else valid.push({ i, s, e })
        })

        valid.sort((a, b) => a.s - b.s)
        for (let k = 1; k < valid.length; k++) {
            if (valid[k].s < valid[k - 1].e)
                ctx.addIssue({ code: 'custom', path: ['slots', valid[k].i, 'startTime'], message: 'Chồng lấn với khung giờ khác' })
        }
    })
