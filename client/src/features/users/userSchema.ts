import { z } from 'zod'

export const createUserSchema = z.object({
    staffCode: z.string().trim().min(1, 'Vui lòng nhập mã nhân viên')
        .regex(/^NV\d{5}$/, 'Mã nhân viên có dạng NV12345'),
    username: z.string().trim().min(3, 'Tối thiểu 3 ký tự').max(50, 'Tối đa 50 ký tự')
        .regex(/^[a-zA-Z0-9._-]+$/, 'Chỉ gồm chữ, số, dấu . _ -'),
    fullName: z.string().trim().min(1, 'Vui lòng nhập họ tên').max(100, 'Tối đa 100 ký tự'),
    password: z.string().min(8, 'Mật khẩu tối thiểu 8 ký tự').max(100, 'Tối đa 100 ký tự')
        .regex(/[A-Za-z]/, 'Mật khẩu cần có chữ cái')
        .regex(/\d/, 'Mật khẩu cần có chữ số'),
    email: z.string().trim().max(100, 'Tối đa 100 ký tự').email('Email không hợp lệ').or(z.literal('')),
    phoneNumber: z.string().trim().regex(/^(0\d{9})?$/, 'Số điện thoại gồm 10 số, bắt đầu bằng 0'),
    departmentId: z.number({ message: 'Vui lòng chọn khoa/phòng' }),
    roleIds: z.array(z.number()).min(1, 'Chọn ít nhất 1 vai trò'),
})
export type CreateUserForm = z.infer<typeof createUserSchema>