import { type ReactNode, useEffect } from 'react'
import { App, Button, Checkbox, DatePicker, Input, InputNumber, Radio } from 'antd'
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import dayjs from 'dayjs'
import SlotEditor from './SlotEditor'
import { campaignSchema, type CampaignFormValues } from '../campaignSchema'
import { useCreateCampaign, useSubmitApproval, useUpdateCampaign } from '../hooks'
import { applyServerErrors, parseApiError } from '@/services/apiError'
import type { CampaignDetail, CampaignListItem, CampaignType } from '../types'
import '../pages/CampaignPage.css'

const DEFAULT_SLOTS = [
    { startTime: '07:30', endTime: '09:00', capacity: 20 },
    { startTime: '09:00', endTime: '11:00', capacity: 20 },
]

const FORM_FIELDS = [
    'campaignName',
    'campaignType',
    'locationName',
    'operatingDays',
    'description',
    'startDate',
    'endDate',
    'targetDonations',
    'maxCapacity',
    'slots',
]

const DAYS_OPTIONS = [
    { label: 'Thứ 2', value: '2' },
    { label: 'Thứ 3', value: '3' },
    { label: 'Thứ 4', value: '4' },
    { label: 'Thứ 5', value: '5' },
    { label: 'Thứ 6', value: '6' },
    { label: 'Thứ 7', value: '7' },
    { label: 'Chủ nhật', value: '8' },
]

const Field = ({
    label,
    required,
    error,
    full,
    children,
}: {
    label: string
    required?: boolean
    error?: string
    full?: boolean
    children: ReactNode
}) => (
    <div className={`ds-field${full ? ' ds-field--full' : ''}`}>
        <label>
            {label}
            {required && <span className="ds-req"> *</span>}
        </label>
        {children}
        {error && <div className="ds-field__error">{error}</div>}
    </div>
)

interface Props {
    campaign?: CampaignDetail | CampaignListItem | null
    defaultType?: CampaignType
    onSuccess?: () => void
    onCancel?: () => void
}

export default function CampaignForm({ campaign, defaultType = 'EVENT', onSuccess, onCancel }: Props) {
    const { modal, message } = App.useApp()
    const isEditing = Boolean(campaign && campaign.campaignId > 0)

    const createMutation = useCreateCampaign()
    const updateMutation = useUpdateCampaign()
    const submitApprovalMutation = useSubmitApproval()

    const {
        control,
        handleSubmit,
        reset,
        setError,
        setValue,
        formState: { errors },
    } = useForm<CampaignFormValues>({
        resolver: zodResolver(campaignSchema),
        mode: 'onBlur',
        defaultValues: {
            campaignName: campaign?.campaignName ?? '',
            campaignType: campaign?.campaignType ?? defaultType,
            locationName: campaign?.locationName ?? '',
            description: campaign?.description ?? '',
            operatingDays: campaign?.operatingDays ?? '',
            startDate: campaign?.startDate ? campaign.startDate.split('T')[0] : dayjs().format('YYYY-MM-DD'),
            endDate: campaign?.endDate ? campaign.endDate.split('T')[0] : dayjs().add(7, 'day').format('YYYY-MM-DD'),
            targetDonations: campaign?.targetDonations ?? 100,
            maxCapacity: campaign?.maxCapacity ?? undefined,
            slots: campaign?.slots && campaign.slots.length > 0
                ? campaign.slots.map((s) => ({
                    startTime: s.startTime.slice(0, 5),
                    endTime: s.endTime.slice(0, 5),
                    capacity: s.capacity,
                }))
                : [...DEFAULT_SLOTS],
        },
    })

    const { fields, append, remove } = useFieldArray({ control, name: 'slots' })
    const campaignType = useWatch({ control, name: 'campaignType' })
    const operatingDays = useWatch({ control, name: 'operatingDays' })

    useEffect(() => {
        if (campaign) {
            reset({
                campaignName: campaign.campaignName,
                campaignType: campaign.campaignType,
                locationName: campaign.locationName,
                description: campaign.description ?? '',
                operatingDays: campaign.operatingDays ?? '',
                startDate: campaign.startDate ? campaign.startDate.split('T')[0] : '',
                endDate: campaign.endDate ? campaign.endDate.split('T')[0] : '',
                targetDonations: campaign.targetDonations,
                maxCapacity: campaign.maxCapacity ?? undefined,
                slots: campaign.slots && campaign.slots.length > 0
                    ? campaign.slots.map((s) => ({
                        startTime: s.startTime.slice(0, 5),
                        endTime: s.endTime.slice(0, 5),
                        capacity: s.capacity,
                    }))
                    : [...DEFAULT_SLOTS],
            })
        }
    }, [campaign, reset])

    const handleFormSubmit = async (values: CampaignFormValues, andSubmitApproval = false) => {
        const payload = {
            campaignName: values.campaignName.trim(),
            description: values.description ? values.description.trim() : undefined,
            locationName: values.locationName.trim(),
            campaignType: values.campaignType,
            operatingDays: values.campaignType === 'RECURRING' ? values.operatingDays : undefined,
            startDate: values.startDate,
            endDate: values.endDate,
            targetDonations: values.targetDonations,
            maxCapacity: values.maxCapacity ?? undefined,
            slots: values.slots.map((s) => ({
                startTime: s.startTime.length === 5 ? `${s.startTime}:00` : s.startTime,
                endTime: s.endTime.length === 5 ? `${s.endTime}:00` : s.endTime,
                capacity: s.capacity,
            })),
        }

        try {
            if (isEditing && campaign) {
                await updateMutation.mutateAsync({ id: campaign.campaignId, body: payload })
                if (andSubmitApproval) {
                    await submitApprovalMutation.mutateAsync(campaign.campaignId)
                    message.success('Đã cập nhật và gửi đợt hiến máu lên phê duyệt')
                } else {
                    message.success('Cập nhật đợt hiến máu thành công')
                }
            } else {
                const created = await createMutation.mutateAsync(payload)
                if (andSubmitApproval && created.campaignId) {
                    await submitApprovalMutation.mutateAsync(created.campaignId)
                    modal.success({
                        title: 'Tạo & gửi duyệt thành công',
                        content: `Chiến dịch "${created.campaignName}" đã được gửi lên Lãnh đạo phê duyệt.`,
                        okText: 'Đóng',
                    })
                } else {
                    modal.success({
                        title: 'Lưu bản nháp thành công',
                        content: `Đợt hiến máu "${created.campaignName}" đã được tạo ở trạng thái Bản nháp (DRAFT). Bạn có thể kiểm tra và gửi duyệt sau.`,
                        okText: 'Đóng',
                    })
                }
                reset()
            }
            onSuccess?.()
        } catch (err) {
            const { message: msg, fieldErrors } = parseApiError(err)
            const applied = applyServerErrors(fieldErrors, setError, FORM_FIELDS)
            message.error(applied ? 'Vui lòng kiểm tra lại các trường được đánh dấu đỏ' : msg)
        }
    }

    const isPending = createMutation.isPending || updateMutation.isPending || submitApprovalMutation.isPending

    const selectedDays = (operatingDays || '').split(',').map((s) => s.trim()).filter(Boolean)

    return (
        <form
            onSubmit={handleSubmit((data) => handleFormSubmit(data, false))}
            className="ds-form"
            noValidate
        >
            <Field label="Hình thức tổ chức" required full error={errors.campaignType?.message}>
                <Controller
                    name="campaignType"
                    control={control}
                    render={({ field }) => (
                        <Radio.Group
                            {...field}
                            buttonStyle="solid"
                            className="campaign-type-radio"
                            onChange={(e) => {
                                field.onChange(e)
                                if (e.target.value === 'EVENT') {
                                    setValue('operatingDays', '')
                                }
                            }}
                        >
                            <Radio.Button value="EVENT">Chiến dịch sự kiện (Một lần / Ngắn hạn)</Radio.Button>
                            <Radio.Button value="RECURRING">Điểm hiến định kỳ (Lịch cố định hàng tuần)</Radio.Button>
                        </Radio.Group>
                    )}
                />
            </Field>

            <Field label="Tên đợt / chiến dịch hiến máu" required error={errors.campaignName?.message}>
                <Controller
                    name="campaignName"
                    control={control}
                    render={({ field }) => (
                        <Input
                            {...field}
                            size="large"
                            placeholder="Ví dụ: Giọt Hồng Blouse Trắng 175 - Đợt 1"
                            status={errors.campaignName ? 'error' : ''}
                        />
                    )}
                />
            </Field>

            <Field label="Địa điểm tổ chức" required error={errors.locationName?.message}>
                <Controller
                    name="locationName"
                    control={control}
                    render={({ field }) => (
                        <Input
                            {...field}
                            size="large"
                            placeholder="Ví dụ: Hội trường Bệnh viện Quân y 175"
                            status={errors.locationName ? 'error' : ''}
                        />
                    )}
                />
            </Field>

            {campaignType === 'RECURRING' && (
                <Field label="Các ngày hoạt động trong tuần" required full error={errors.operatingDays?.message}>
                    <Checkbox.Group
                        options={DAYS_OPTIONS}
                        value={selectedDays}
                        onChange={(checkedValues) => {
                            setValue('operatingDays', checkedValues.sort().join(','), { shouldValidate: true })
                        }}
                    />
                    <div className="ds-hint" style={{ marginTop: 8 }}>
                        Chọn các ngày điểm hiến tiếp nhận người hiến máu định kỳ mỗi tuần (Ví dụ: Thứ 2, Thứ 4, Thứ 6).
                    </div>
                </Field>
            )}

            <Field label="Ngày bắt đầu" required error={errors.startDate?.message}>
                <Controller
                    name="startDate"
                    control={control}
                    render={({ field }) => (
                        <DatePicker
                            style={{ width: '100%' }}
                            size="large"
                            format="DD/MM/YYYY"
                            value={field.value ? dayjs(field.value) : null}
                            onChange={(date) => field.onChange(date ? date.format('YYYY-MM-DD') : '')}
                            status={errors.startDate ? 'error' : ''}
                        />
                    )}
                />
            </Field>

            <Field label="Ngày kết thúc" required error={errors.endDate?.message}>
                <Controller
                    name="endDate"
                    control={control}
                    render={({ field }) => (
                        <DatePicker
                            style={{ width: '100%' }}
                            size="large"
                            format="DD/MM/YYYY"
                            value={field.value ? dayjs(field.value) : null}
                            onChange={(date) => field.onChange(date ? date.format('YYYY-MM-DD') : '')}
                            status={errors.endDate ? 'error' : ''}
                        />
                    )}
                />
            </Field>

            <Field label="Chỉ tiêu thu gom (túi máu)" required error={errors.targetDonations?.message}>
                <Controller
                    name="targetDonations"
                    control={control}
                    render={({ field }) => (
                        <InputNumber
                            style={{ width: '100%' }}
                            size="large"
                            min={1}
                            max={10000}
                            value={field.value}
                            onChange={(val) => field.onChange(val ?? 0)}
                            placeholder="Ví dụ: 200"
                            status={errors.targetDonations ? 'error' : ''}
                        />
                    )}
                />
            </Field>

            <Field label="Sức chứa tối đa (tùy chọn)" error={errors.maxCapacity?.message}>
                <Controller
                    name="maxCapacity"
                    control={control}
                    render={({ field }) => (
                        <InputNumber
                            style={{ width: '100%' }}
                            size="large"
                            min={1}
                            max={20000}
                            value={field.value ?? undefined}
                            onChange={(val) => field.onChange(val ?? null)}
                            placeholder="Để trống nếu không giới hạn"
                            status={errors.maxCapacity ? 'error' : ''}
                        />
                    )}
                />
            </Field>

            <Field label="Mô tả / Ghi chú thêm" full error={errors.description?.message}>
                <Controller
                    name="description"
                    control={control}
                    render={({ field }) => (
                        <Input.TextArea
                            {...field}
                            rows={3}
                            placeholder="Thông tin đối tượng tiếp nhận, hướng dẫn gửi xe hoặc lưu ý y tế..."
                        />
                    )}
                />
            </Field>

            <SlotEditor
                control={control}
                errors={errors}
                count={fields.length}
                onAdd={() => append({ startTime: '13:30', endTime: '15:00', capacity: 20 })}
                onRemove={remove}
            />

            <div className="ds-form__foot">
                {onCancel && (
                    <Button onClick={onCancel} disabled={isPending}>
                        Hủy
                    </Button>
                )}
                <Button
                    type="default"
                    size="large"
                    htmlType="submit"
                    loading={isPending}
                >
                    {isEditing ? 'Lưu cập nhật' : 'Lưu bản nháp (DRAFT)'}
                </Button>
                <Button
                    type="primary"
                    size="large"
                    loading={isPending}
                    onClick={handleSubmit((data) => handleFormSubmit(data, true))}
                >
                    {isEditing ? 'Lưu & Gửi phê duyệt' : 'Tạo & Gửi phê duyệt'}
                </Button>
            </div>
        </form>
    )
}

