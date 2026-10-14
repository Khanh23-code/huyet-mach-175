import { z } from 'zod'

export const createUserSchema = z.object({
    staffCode: z.string().trim().min(1, 'Vui lòng nhập mã nhân viên')
        .regex(/^\d{3}-\d{5}$/, 'Mã nhân viên có dạng 175-01245'),
    username: z.string().trim().min(3, 'Tối thiểu 3 ký tự').max(50, 'Tối đa 50 ký tự'),
    fullName: z.string().trim().min(1, 'Vui lòng nhập họ tên').max(100),
    password: z.string().min(8, 'Mật khẩu tối thiểu 8 ký tự'),
    email: z.string().trim().email('Email không hợp lệ').or(z.literal('')),
    phoneNumber: z.string().trim().regex(/^(0\d{9,10})?$/, 'Số điện thoại không hợp lệ'),
    departmentId: z.number({ message: 'Vui lòng chọn khoa/phòng' }),
    roleIds: z.array(z.number()).min(1, 'Chọn ít nhất 1 vai trò'),
})
export type CreateUserForm = z.infer<typeof createUserSchema>