import { useEffect } from 'react'
import { App, Button, Input, InputNumber, Modal, Switch } from 'antd'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { applyServerErrors, parseApiError } from '@/services/apiError'
import { useCreateLocation, useUpdateLocation } from '../stockHooks'
import { storageLocationSchema, type StorageLocationFormValues } from '../storageLocationSchema'
import type { StorageLocation } from '../types'

interface Props {
    open: boolean
    /** null = tạo mới */
    location: StorageLocation | null
    onClose: () => void
}

const EMPTY: StorageLocationFormValues = {
    refrigeratorName: '', shelfNumber: '', targetTemperature: 4, isFull: false, isActive: true,
}
const FIELDS = ['refrigeratorName', 'shelfNumber', 'targetTemperature']

export default function StorageLocationFormModal({ open, location, onClose }: Props) {
    const { message } = App.useApp()
    const create = useCreateLocation()
    const update = useUpdateLocation()
    const pending = create.isPending || update.isPending

    const { control, handleSubmit, reset, setError, formState: { errors } } = useForm<StorageLocationFormValues>({
        resolver: zodResolver(storageLocationSchema),
        defaultValues: EMPTY,
    })

    useEffect(() => {
        if (!open) return
        reset(
            location
                ? {
                    refrigeratorName: location.refrigeratorName,
                    shelfNumber: location.shelfNumber,
                    targetTemperature: location.targetTemperature,
                    isFull: location.isFull,
                    isActive: location.isActive,
                }
                : EMPTY,
        )
    }, [open, location, reset])

    const onSubmit = async (v: StorageLocationFormValues) => {
        try {
            if (location) {
                await update.mutateAsync({ id: location.locationId, body: v })
                message.success('Đã cập nhật tủ lưu trữ')
            } else {
                await create.mutateAsync(v)
                message.success('Đã thêm tủ lưu trữ')
            }
            onClose()
        } catch (e) {
            const { message: msg, fieldErrors } = parseApiError(e)
            const applied = applyServerErrors(fieldErrors, setError, FIELDS)
            message.error(applied ? 'Vui lòng kiểm tra lại các trường được đánh dấu đỏ' : msg)
        }
    }

    return (
        <Modal
            open={open}
            onCancel={onClose}
            title={location ? 'Sửa tủ / ngăn lưu trữ' : 'Thêm tủ / ngăn lưu trữ'}
            width={520}
            destroyOnHidden
            maskClosable={!pending}
            footer={
                <div className="inv-actions" style={{ justifyContent: 'flex-end' }}>
                    <Button onClick={onClose} disabled={pending}>Hủy</Button>
                    <Button type="primary" loading={pending} onClick={handleSubmit(onSubmit)}>
                        {location ? 'Lưu thay đổi' : 'Thêm tủ'}
                    </Button>
                </div>
            }
        >
            <form className="stk-form" noValidate onSubmit={handleSubmit(onSubmit)}>
                <div className="stk-field">
                    <label>Tên tủ <span className="stk-req">*</span></label>
                    <Controller name="refrigeratorName" control={control} render={({ field }) => (
                        <Input {...field} placeholder="Ví dụ: Tủ A" maxLength={50} status={errors.refrigeratorName ? 'error' : ''} />
                    )} />
                    {errors.refrigeratorName && <div className="stk-err">{errors.refrigeratorName.message}</div>}
                </div>

                <div className="stk-field">
                    <label>Mã ngăn / kệ <span className="stk-req">*</span></label>
                    <Controller name="shelfNumber" control={control} render={({ field }) => (
                        <Input {...field} placeholder="Ví dụ: A-2" maxLength={20} status={errors.shelfNumber ? 'error' : ''} />
                    )} />
                    {errors.shelfNumber && <div className="stk-err">{errors.shelfNumber.message}</div>}
                </div>

                <div className="stk-field">
                    <label>Nhiệt độ mục tiêu (°C) <span className="stk-req">*</span></label>
                    <Controller name="targetTemperature" control={control} render={({ field }) => (
                        <InputNumber
                            style={{ width: '100%' }}
                            min={-100}
                            max={50}
                            step={0.5}
                            value={field.value}
                            onChange={(v) => field.onChange(v ?? undefined)}
                            status={errors.targetTemperature ? 'error' : ''}
                        />
                    )} />
                    <div className="stk-hint">Gợi ý: máu toàn phần / hồng cầu 1–10°C · huyết tương ≤ -18°C · tiểu cầu 20–24°C</div>
                    {errors.targetTemperature && <div className="stk-err">{errors.targetTemperature.message}</div>}
                </div>

                <div className="stk-switches">
                    <label>
                        <Controller name="isActive" control={control} render={({ field }) => (
                            <Switch checked={field.value} onChange={field.onChange} />
                        )} />
                        Đang sử dụng
                    </label>
                    <label>
                        <Controller name="isFull" control={control} render={({ field }) => (
                            <Switch checked={field.value} onChange={field.onChange} />
                        )} />
                        Đã đầy
                    </label>
                </div>
            </form>
        </Modal>
    )
}