import { z } from 'zod'
import { ACTIONS, type ActionKey } from '@/config/rbac'

const ACTION_KEYS = ACTIONS.map((a) => a.key) as [ActionKey, ...ActionKey[]]

export interface ExistingModule {
    code: string // viết hoa
    name: string
    /** Các action đã có của module, viết hoa */
    actions: string[]
}

/**
 * Schema phụ thuộc danh sách module hiện có:
 *  - module mới  -> bắt buộc nhập tên module
 *  - module cũ   -> không cho tạo trùng action
 */
export const createPermissionSchema = (modules: ExistingModule[]) =>
    z
        .object({
            moduleCode: z.string().trim()
                .min(2, 'Tối thiểu 2 ký tự')
                .max(20, 'Tối đa 20 ký tự')
                .regex(/^[A-Za-z][A-Za-z0-9_]*$/, 'Chỉ gồm chữ, số, dấu gạch dưới; bắt đầu bằng chữ'),
            moduleName: z.string().trim().max(100, 'Tối đa 100 ký tự'),
            actionType: z.enum(ACTION_KEYS, { message: 'Vui lòng chọn thao tác' }),
            permissionName: z.string().trim().min(1, 'Vui lòng nhập tên quyền').max(100, 'Tối đa 100 ký tự'),
            description: z.string().trim().max(255, 'Tối đa 255 ký tự'),
        })
        .superRefine((v, ctx) => {
            const found = modules.find((m) => m.code === v.moduleCode.toUpperCase())

            if (!found && !v.moduleName) {
                ctx.addIssue({
                    code: 'custom',
                    path: ['moduleName'],
                    message: 'Module mới cần có tên hiển thị',
                })
            }
            if (found?.actions.includes(v.actionType)) {
                ctx.addIssue({
                    code: 'custom',
                    path: ['actionType'],
                    message: 'Module này đã có thao tác đó',
                })
            }
        })

export type CreatePermissionForm = z.infer<ReturnType<typeof createPermissionSchema>>