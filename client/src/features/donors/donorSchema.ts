import { z } from 'zod'
import { ageFromIso } from './format'

export const walkInSchema = z.object({
    citizenId: z.string().regex(/^\d{12}$/, 'Số CCCD phải gồm đúng 12 chữ số'),
    fullName: z
        .string()
        .trim()
        .min(1, 'Vui lòng nhập họ tên')
        .max(100, 'Họ tên tối đa 100 ký tự'),
    dateOfBirth: z
        .string()
        .min(1, 'Vui lòng nhập ngày sinh')
        .refine((v) => {
            if (!v) return true
            const age = ageFromIso(v)
            return age >= 18 && age <= 60
        }, 'Người hiến phải từ 18 đến 60 tuổi'),
    gender: z.string().min(1, 'Vui lòng chọn giới tính').max(10, 'Giới tính tối đa 10 ký tự'),
    phoneNumber: z
        .string()
        .trim()
        .regex(/^0\d{9}$/, 'Số điện thoại gồm 10 số, bắt đầu bằng 0'),
})
export type WalkInForm = z.infer<typeof walkInSchema>