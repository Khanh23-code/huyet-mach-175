import { z } from 'zod'

export const componentTypeSchema = z
    .object({
        typeName: z
            .string({ message: 'Vui lòng nhập tên chế phẩm máu' })
            .trim()
            .min(1, 'Tên chế phẩm máu không được để trống')
            .max(100, 'Tên chế phẩm máu tối đa 100 ký tự'),
        lifeDays: z
            .number({ message: 'Vui lòng nhập hạn sử dụng (ngày)' })
            .int('Hạn sử dụng phải là số nguyên ngày')
            .min(1, 'Hạn sử dụng tối thiểu 1 ngày')
            .max(3650, 'Hạn sử dụng tối đa 3650 ngày'),
        storageTemperature: z
            .string({ message: 'Vui lòng nhập nhãn nhiệt độ bảo quản' })
            .trim()
            .min(1, 'Nhãn nhiệt độ bảo quản không được để trống')
            .max(50, 'Nhãn nhiệt độ bảo quản tối đa 50 ký tự'),
        description: z.string().max(255, 'Mô tả tối đa 255 ký tự').optional().nullable(),
        minTemp: z
            .number({ message: 'Vui lòng nhập nhiệt độ tối thiểu' })
            .min(-100, 'Nhiệt độ tối thiểu từ -100°C')
            .max(50, 'Nhiệt độ tối thiểu không vượt quá 50°C'),
        maxTemp: z
            .number({ message: 'Vui lòng nhập nhiệt độ tối đa' })
            .min(-100, 'Nhiệt độ tối đa từ -100°C')
            .max(50, 'Nhiệt độ tối đa không vượt quá 50°C'),
        allowRestock: z.boolean(),
        maxOutsideMinutesForRestock: z
            .number()
            .int('Thời gian ngoài tủ phải là số nguyên phút')
            .min(1, 'Thời gian ngoài tủ tối đa phải lớn hơn 0 phút')
            .nullable()
            .optional(),
        maxRestockCount: z
            .number({ message: 'Vui lòng nhập số lần tái nhập kho tối đa' })
            .int('Số lần tái nhập kho phải là số nguyên')
            .min(0, 'Số lần tái nhập kho tối thiểu là 0')
            .max(100, 'Số lần tái nhập kho tối đa là 100'),
        compatibilityRuleType: z.enum(['RBC', 'PLASMA', 'PLATELET'], {
            message: 'Vui lòng chọn quy tắc hòa hợp miễn dịch',
        }),
    })
    .refine((data) => data.minTemp <= data.maxTemp, {
        message: 'Nhiệt độ tối thiểu không được lớn hơn nhiệt độ tối đa',
        path: ['minTemp'],
    })

export type ComponentTypeFormValues = z.infer<typeof componentTypeSchema>
