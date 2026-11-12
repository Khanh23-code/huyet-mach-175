import { z } from 'zod'

export const storageLocationSchema = z.object({
    refrigeratorName: z
        .string({ message: 'Vui lòng nhập tên tủ' })
        .trim()
        .min(1, 'Vui lòng nhập tên tủ')
        .max(50, 'Tên tủ tối đa 50 ký tự'),
    shelfNumber: z
        .string({ message: 'Vui lòng nhập mã ngăn/kệ' })
        .trim()
        .min(1, 'Vui lòng nhập mã ngăn/kệ')
        .max(20, 'Mã ngăn/kệ tối đa 20 ký tự'),
    targetTemperature: z
        .number({ message: 'Vui lòng nhập nhiệt độ mục tiêu' })
        .min(-100, 'Nhiệt độ tối thiểu -100°C')
        .max(50, 'Nhiệt độ tối đa 50°C'),
    isFull: z.boolean(),
    isActive: z.boolean(),
})

export type StorageLocationFormValues = z.infer<typeof storageLocationSchema>