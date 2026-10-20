import { z } from 'zod'

export const loginSchema = z.object({
    username: z
        .string()
        .trim()
        .min(1, 'Vui lòng nhập tên đăng nhập hoặc mã nhân viên'),
    password: z.string().min(1, 'Vui lòng nhập mật khẩu'),
    remember: z.boolean().optional(),
})
export type LoginForm = z.infer<typeof loginSchema>

export const changePasswordSchema = z
    .object({
        currentPassword: z.string().min(1, 'Vui lòng nhập mật khẩu hiện tại'),
        newPassword: z
            .string()
            .min(8, 'Mật khẩu mới phải có ít nhất 8 ký tự')
            .regex(/[A-Z]/, 'Mật khẩu mới phải có ít nhất 1 chữ hoa')
            .regex(/\d/, 'Mật khẩu mới phải có ít nhất 1 chữ số'),
        confirmNewPassword: z.string().min(1, 'Vui lòng xác nhận mật khẩu mới'),
    })
    .refine((d) => d.newPassword === d.confirmNewPassword, {
        path: ['confirmNewPassword'],
        message: 'Mật khẩu xác nhận không khớp',
    })
    .refine((d) => !d.newPassword || d.newPassword !== d.currentPassword, {
        path: ['newPassword'],
        message: 'Mật khẩu mới phải khác mật khẩu hiện tại',
    })
export type ChangePasswordForm = z.infer<typeof changePasswordSchema>