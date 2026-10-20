import { z } from 'zod'

// Giới hạn khớp entity Role ở BE: role_code <= 50, role_name <= 50, description <= 255.
// Mã vai trò giới hạn 20 cho gọn; nâng lên 50 nếu cần.
export const createRoleSchema = z.object({
    roleCode: z.string().trim()
        .min(2, 'Tối thiểu 2 ký tự')
        .max(20, 'Tối đa 20 ký tự')
        .regex(/^[A-Za-z][A-Za-z0-9_]*$/, 'Chỉ gồm chữ, số, dấu gạch dưới; bắt đầu bằng chữ'),
    roleName: z.string().trim().min(1, 'Vui lòng nhập tên vai trò').max(50, 'Tối đa 50 ký tự'),
    description: z.string().trim().max(255, 'Tối đa 255 ký tự'),
})

export type CreateRoleForm = z.infer<typeof createRoleSchema>