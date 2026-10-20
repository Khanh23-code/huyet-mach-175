import { z } from 'zod'

export const createUserSchema = z.object({
    staffCode: z.string().trim().min(1, 'Vui lòng nhập mã nhân viên')
        .regex(/^NV\d{5}$/, 'Mã nhân viên có dạng NVxxxxx'),
    username: z.string().trim().min(3, 'Tối thiểu 3 ký tự').max(50, 'Tối đa 50 ký tự')
        .regex(/^[a-zA-Z0-9._-]+$/, 'Chỉ gồm chữ, số, dấu . _ -'),
    fullName: z.string().trim().min(1, 'Vui lòng nhập họ tên').max(100, 'Tối đa 100 ký tự'),

    password: z
        .string()
        .min(8, 'Mật khẩu tối thiểu 8 ký tự')
        .max(100, 'Mật khẩu tối đa 100 ký tự')
        .regex(/[a-z]/, 'Mật khẩu cần có ít nhất 1 chữ thường')
        .regex(/[A-Z]/, 'Mật khẩu cần có ít nhất 1 chữ in hoa')
        .regex(/\d/, 'Mật khẩu cần có ít nhất 1 chữ số')
        .regex(
            /[^A-Za-z0-9\s]/,
            'Mật khẩu cần có ít nhất 1 ký tự đặc biệt'
        )
        .regex(/^\S+$/, 'Mật khẩu không được chứa khoảng trắng'),

    email: z.string().trim().max(100, 'Tối đa 100 ký tự').email('Email không hợp lệ').or(z.literal('')),
    phoneNumber: z.string().trim().regex(/^(0\d{9})?$/, 'Số điện thoại gồm 10 số, bắt đầu bằng 0'),
    departmentId: z.number({ message: 'Vui lòng chọn khoa/phòng' }),
    roleIds: z.array(z.number()).min(1, 'Chọn ít nhất 1 vai trò'),
})
export type CreateUserForm = z.infer<typeof createUserSchema>

/** Form chỉnh sửa: tái sử dụng rule của form tạo, bỏ mã NV / username / mật khẩu (không sửa được). */
export const editUserSchema = createUserSchema.pick({
    fullName: true,
    email: true,
    phoneNumber: true,
    departmentId: true,
    roleIds: true,
})
export type EditUserForm = z.infer<typeof editUserSchema>