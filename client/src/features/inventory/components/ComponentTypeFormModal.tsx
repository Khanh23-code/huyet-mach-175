import type { ClipboardEvent, KeyboardEvent } from 'react'
import { Alert, App, Button, Input, InputNumber, Modal, Select, Switch } from 'antd'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { applyServerErrors, parseApiError } from '@/services/apiError'
import { componentTypeSchema, type ComponentTypeFormValues } from '../componentTypeSchema'
import { COMPATIBILITY_RULE_META } from '../stockFormat'
import { useUpdateBloodComponentType } from '../stockHooks'
import type { BloodComponentTypeConfig } from '../stockTypes'
import type { CompatibilityRuleType } from '../types'

interface Props {
    open: boolean
    item: BloodComponentTypeConfig | null
    onClose: () => void
}

interface FormProps {
    item: BloodComponentTypeConfig
    onClose: () => void
}

const FIELDS: (keyof ComponentTypeFormValues)[] = [
    'typeName',
    'lifeDays',
    'storageTemperature',
    'description',
    'minTemp',
    'maxTemp',
    'allowRestock',
    'maxOutsideMinutesForRestock',
    'maxRestockCount',
    'compatibilityRuleType',
]

const COMPAT_OPTIONS = (Object.keys(COMPATIBILITY_RULE_META) as CompatibilityRuleType[]).map((k) => ({
    value: k,
    label: COMPATIBILITY_RULE_META[k].label,
}))

const CONTROL_KEYS = new Set([
    'Backspace',
    'Delete',
    'Tab',
    'Escape',
    'Enter',
    'ArrowLeft',
    'ArrowRight',
    'ArrowUp',
    'ArrowDown',
    'Home',
    'End',
])

const blockNonDigitKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.ctrlKey || e.metaKey || e.altKey || CONTROL_KEYS.has(e.key)) return
    if (!/^\d$/.test(e.key)) {
        e.preventDefault()
    }
}

const blockNonDigitPaste = (e: ClipboardEvent<HTMLInputElement>) => {
    const pasted = e.clipboardData.getData('text')
    if (/\D/.test(pasted)) {
        e.preventDefault()
    }
}

const digitsOnlyFormatter = (val?: number | string) => {
    if (val === undefined || val === null) return ''
    return String(val).replace(/\D+/g, '')
}

const digitsOnlyParser = (displayValue?: string) => {
    const digits = (displayValue ?? '').replace(/\D+/g, '')
    return digits ? Number(digits) : 0
}

const nullableDigitsOnlyParser = (displayValue?: string) => {
    const digits = (displayValue ?? '').replace(/\D+/g, '')
    return digits ? Number(digits) : (undefined as unknown as number)
}

function ComponentTypeForm({ item, onClose }: FormProps) {
    const { message } = App.useApp()
    const update = useUpdateBloodComponentType()

    const initialAllowRestock = item.maxRestockCount > 0 ? item.allowRestock : false

    const {
        control,
        handleSubmit,
        setError,
        setValue,
        formState: { errors },
    } = useForm<ComponentTypeFormValues>({
        resolver: zodResolver(componentTypeSchema),
        defaultValues: {
            typeName: item.typeName,
            lifeDays: item.lifeDays,
            storageTemperature: item.storageTemperature,
            description: item.description ?? '',
            minTemp: item.minTemp,
            maxTemp: item.maxTemp,
            allowRestock: initialAllowRestock,
            maxOutsideMinutesForRestock: item.maxOutsideMinutesForRestock ?? null,
            maxRestockCount: item.maxRestockCount,
            compatibilityRuleType: item.compatibilityRuleType,
        },
    })

    const allowRestock = useWatch({ control, name: 'allowRestock' })
    const maxRestockCount = useWatch({ control, name: 'maxRestockCount' })

    const onSubmit = async (v: ComponentTypeFormValues) => {
        try {
            await update.mutateAsync({
                id: item.componentTypeId,
                body: {
                    typeName: v.typeName.trim(),
                    lifeDays: v.lifeDays,
                    storageTemperature: v.storageTemperature.trim(),
                    description: v.description?.trim() ? v.description.trim() : null,
                    minTemp: v.minTemp,
                    maxTemp: v.maxTemp,
                    allowRestock: v.allowRestock,
                    maxOutsideMinutesForRestock: v.maxOutsideMinutesForRestock ?? null,
                    maxRestockCount: v.maxRestockCount,
                    compatibilityRuleType: v.compatibilityRuleType,
                },
            })
            message.success(`Đã cập nhật cấu hình chế phẩm ${item.typeCode} (${v.typeName.trim()})`)
            onClose()
        } catch (e) {
            const { message: msg, fieldErrors } = parseApiError(e)
            const applied = applyServerErrors(fieldErrors, setError, FIELDS)
            message.error(applied ? 'Vui lòng kiểm tra lại các trường được đánh dấu đỏ' : msg)
        }
    }

    return (
        <form className="stk-form" noValidate onSubmit={handleSubmit(onSubmit)}>
            <div style={{ display: 'grid', gridTemplateColumns: '140px 1fr', gap: 12 }}>
                <div className="stk-field">
                    <label>Mã chế phẩm</label>
                    <Input value={item.typeCode} disabled className="inv-mono" />
                </div>
                <div className="stk-field">
                    <label>
                        Tên chế phẩm máu <span className="stk-req">*</span>
                    </label>
                    <Controller
                        name="typeName"
                        control={control}
                        render={({ field }) => (
                            <Input
                                {...field}
                                maxLength={100}
                                placeholder="Ví dụ: Khối hồng cầu"
                                status={errors.typeName ? 'error' : ''}
                            />
                        )}
                    />
                    {errors.typeName && <div className="stk-err">{errors.typeName.message}</div>}
                </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                <div className="stk-field">
                    <label>
                        Hạn sử dụng chuẩn (ngày) <span className="stk-req">*</span>
                    </label>
                    <Controller
                        name="lifeDays"
                        control={control}
                        render={({ field }) => (
                            <InputNumber
                                style={{ width: '100%' }}
                                min={1}
                                max={3650}
                                step={1}
                                precision={0}
                                value={field.value}
                                onChange={(v) => field.onChange(v ?? undefined)}
                                status={errors.lifeDays ? 'error' : ''}
                            />
                        )}
                    />
                    {errors.lifeDays && <div className="stk-err">{errors.lifeDays.message}</div>}
                </div>

                <div className="stk-field">
                    <label>
                        Nhóm quy tắc hòa hợp miễn dịch <span className="stk-req">*</span>
                    </label>
                    <Controller
                        name="compatibilityRuleType"
                        control={control}
                        render={({ field }) => (
                            <>
                                <Select
                                    value={field.value}
                                    onChange={field.onChange}
                                    options={COMPAT_OPTIONS}
                                    status={errors.compatibilityRuleType ? 'error' : ''}
                                    style={{ width: '100%' }}
                                />
                                <div className="stk-hint">{COMPATIBILITY_RULE_META[field.value]?.desc}</div>
                            </>
                        )}
                    />
                    {errors.compatibilityRuleType && (
                        <div className="stk-err">{errors.compatibilityRuleType.message}</div>
                    )}
                </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12 }}>
                <div className="stk-field">
                    <label>
                        Nhãn nhiệt độ hiển thị <span className="stk-req">*</span>
                    </label>
                    <Controller
                        name="storageTemperature"
                        control={control}
                        render={({ field }) => (
                            <Input
                                {...field}
                                maxLength={50}
                                placeholder="VD: 2°C - 6°C"
                                status={errors.storageTemperature ? 'error' : ''}
                            />
                        )}
                    />
                    {errors.storageTemperature && (
                        <div className="stk-err">{errors.storageTemperature.message}</div>
                    )}
                </div>

                <div className="stk-field">
                    <label>
                        Nhiệt độ tối thiểu (°C) <span className="stk-req">*</span>
                    </label>
                    <Controller
                        name="minTemp"
                        control={control}
                        render={({ field }) => (
                            <InputNumber
                                style={{ width: '100%' }}
                                min={-100}
                                max={50}
                                step={0.5}
                                value={field.value}
                                onChange={(v) => field.onChange(v ?? undefined)}
                                status={errors.minTemp ? 'error' : ''}
                            />
                        )}
                    />
                    {errors.minTemp && <div className="stk-err">{errors.minTemp.message}</div>}
                </div>

                <div className="stk-field">
                    <label>
                        Nhiệt độ tối đa (°C) <span className="stk-req">*</span>
                    </label>
                    <Controller
                        name="maxTemp"
                        control={control}
                        render={({ field }) => (
                            <InputNumber
                                style={{ width: '100%' }}
                                min={-100}
                                max={50}
                                step={0.5}
                                value={field.value}
                                onChange={(v) => field.onChange(v ?? undefined)}
                                status={errors.maxTemp ? 'error' : ''}
                            />
                        )}
                    />
                    {errors.maxTemp && <div className="stk-err">{errors.maxTemp.message}</div>}
                </div>
            </div>

            <div className="stk-field">
                <label>Mô tả quy chuẩn</label>
                <Controller
                    name="description"
                    control={control}
                    render={({ field }) => (
                        <Input.TextArea
                            rows={2}
                            maxLength={255}
                            showCount
                            value={field.value ?? ''}
                            onChange={(e) => field.onChange(e.target.value)}
                            placeholder="Ghi chú đặc tính bảo quản hoặc hướng dẫn chuyên môn..."
                            status={errors.description ? 'error' : ''}
                        />
                    )}
                />
                {errors.description && <div className="stk-err">{errors.description.message}</div>}
            </div>

            <div
                style={{
                    padding: 14,
                    borderRadius: 'var(--radius-md)',
                    border: '1px solid var(--c-border)',
                    background: 'var(--c-surface-muted, #fafafa)',
                    display: 'grid',
                    gap: 12,
                }}
            >
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12 }}>
                    <div>
                        <div style={{ fontWeight: 600, fontSize: 'var(--text-sm)' }}>
                            Cho phép đánh giá tái nhập kho khi hoàn trả
                        </div>
                        <div style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)' }}>
                            Nếu tắt, mọi túi thuộc loại chế phẩm này khi hoàn trả về Ngân hàng máu đều bắt buộc tiêu hủy.
                        </div>
                    </div>
                    <Controller
                        name="allowRestock"
                        control={control}
                        render={({ field }) => (
                            <Switch
                                checked={field.value}
                                onChange={(checked) => {
                                    field.onChange(checked)
                                    if (checked && maxRestockCount <= 0) {
                                        setValue('maxRestockCount', 1, {
                                            shouldValidate: true,
                                            shouldDirty: true,
                                        })
                                    }
                                }}
                            />
                        )}
                    />
                </div>

                {!allowRestock && (
                    <Alert
                        type="warning"
                        showIcon
                        message="Đang tắt quyền tái nhập kho"
                        description="Chế phẩm này sẽ luôn bị gắn cờ bắt buộc tiêu hủy trên màn hình Tiếp nhận hoàn trả tại Ngân hàng máu."
                    />
                )}

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                    <div className="stk-field">
                        <label>Số lần tái nhập kho tối đa</label>
                        <Controller
                            name="maxRestockCount"
                            control={control}
                            render={({ field }) => (
                                <InputNumber<number>
                                    style={{ width: '100%' }}
                                    inputMode="numeric"
                                    min={0}
                                    max={100}
                                    step={1}
                                    precision={0}
                                    formatter={digitsOnlyFormatter}
                                    parser={digitsOnlyParser}
                                    onKeyDown={blockNonDigitKeyDown}
                                    onPaste={blockNonDigitPaste}
                                    value={field.value}
                                    onChange={(v) => {
                                        const nextCount = typeof v === 'number' && !Number.isNaN(v) ? Math.trunc(v) : 0
                                        field.onChange(nextCount)
                                        setValue('allowRestock', nextCount > 0, {
                                            shouldValidate: true,
                                            shouldDirty: true,
                                        })
                                    }}
                                    status={errors.maxRestockCount ? 'error' : ''}
                                />
                            )}
                        />
                        {errors.maxRestockCount && <div className="stk-err">{errors.maxRestockCount.message}</div>}
                    </div>

                    <div className="stk-field">
                        <label>Thời gian ngoài tủ tối đa (phút)</label>
                        <Controller
                            name="maxOutsideMinutesForRestock"
                            control={control}
                            render={({ field }) => (
                                <InputNumber<number>
                                    style={{ width: '100%' }}
                                    inputMode="numeric"
                                    min={1}
                                    max={1440}
                                    step={5}
                                    precision={0}
                                    formatter={digitsOnlyFormatter}
                                    parser={nullableDigitsOnlyParser}
                                    onKeyDown={blockNonDigitKeyDown}
                                    onPaste={blockNonDigitPaste}
                                    placeholder="Để trống nếu không giới hạn"
                                    value={field.value ?? undefined}
                                    onChange={(v) =>
                                        field.onChange(
                                            typeof v === 'number' && !Number.isNaN(v) && v >= 1 ? Math.trunc(v) : null,
                                        )
                                    }
                                    status={errors.maxOutsideMinutesForRestock ? 'error' : ''}
                                />
                            )}
                        />
                        {errors.maxOutsideMinutesForRestock && (
                            <div className="stk-err">{errors.maxOutsideMinutesForRestock.message}</div>
                        )}
                    </div>
                </div>
            </div>

            <div className="inv-actions" style={{ justifyContent: 'flex-end', marginTop: 4 }}>
                <Button onClick={onClose} disabled={update.isPending}>
                    Hủy
                </Button>
                <Button type="primary" htmlType="submit" loading={update.isPending}>
                    Lưu cấu hình
                </Button>
            </div>
        </form>
    )
}

export default function ComponentTypeFormModal({ open, item, onClose }: Props) {
    return (
        <Modal
            open={open && Boolean(item)}
            onCancel={onClose}
            title={item ? `Cấu hình chế phẩm ${item.typeCode} - ${item.typeName}` : 'Cấu hình chế phẩm máu'}
            width={660}
            destroyOnHidden
            footer={null}
        >
            {item && <ComponentTypeForm key={item.componentTypeId} item={item} onClose={onClose} />}
        </Modal>
    )
}
