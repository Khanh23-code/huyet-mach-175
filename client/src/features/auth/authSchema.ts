import { z } from 'zod'

export const loginSchema = z.object({
    username: z
        .string()
        .trim()
        .min(1, 'Vui lòng nhập mã nhân viên'),
    password: z.string().min(1, 'Vui lòng nhập mật khẩu'),
    remember: z.boolean().optional(),
})
export type LoginForm = z.infer<typeof loginSchema>