import { z } from 'zod'
import { ACTIONS, type ActionKey } from '@/config/rbac'

const ACTION_KEYS = ACTIONS.map((a) => a.key) as [ActionKey, ...ActionKey[]]

export interface ExistingModule {
    code: string // viết hoa
    name: string
    /** Các action đã có của module, viết hoa */
    actions: string[]
}

/** Module đã có đủ mọi thao tác (VIEW/EDIT/APPROVE/CANCEL) -> không thể thêm nữa. */
export const isModuleFull = (m?: ExistingModule) =>
    !!m && ACTIONS.every((a) => m.actions.includes(a.key))

/**
 * Schema phụ thuộc danh sách module hiện có:
 *  - module mới  -> bắt buộc nhập tên module
 *  - module cũ   -> không cho tạo trùng action, và không cho thêm nếu đã đủ thao tác
 */
export const createPermissionSchema = (modules: ExistingModule[]) =>
    z
        .object({
            moduleCode: z.string().trim()
                .min(2, 'Tối thiểu 2 ký tự')
                .max(20, 'Tối đa 20 ký tự')
                .regex(/^[A-Za-z][A-Za-z0-9_]*$/, 'Chỉ gồm chữ, số, dấu gạch dưới; bắt đầu bằng chữ'),
            moduleName: z.string().trim().max(100, 'Tối đa 100 ký tự'),
            actionTypes: z.array(z.enum(ACTION_KEYS)).min(1, 'Chọn ít nhất 1 thao tác'),
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
            if (isModuleFull(found)) {
                ctx.addIssue({
                    code: 'custom',
                    path: ['moduleCode'],
                    message: 'Phân hệ này đã có đủ thao tác',
                })
            } else if (found && v.actionTypes.some((a) => found.actions.includes(a))) {
                ctx.addIssue({
                    code: 'custom',
                    path: ['actionTypes'],
                    message: 'Có thao tác đã tồn tại trong phân hệ này',
                })
            }
        })

export type CreatePermissionForm = z.infer<ReturnType<typeof createPermissionSchema>>