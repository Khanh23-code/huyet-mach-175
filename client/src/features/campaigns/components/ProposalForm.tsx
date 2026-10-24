import type { ReactNode } from 'react'
import { App, Button, Input, Select } from 'antd'
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import SlotEditor from './SlotEditor'
import { makeProposalSchema, normalizePhone, type ProposalFormValues } from '../proposalSchema'
import { proposalsApi } from '../proposalsApi'
import { useSites } from '../hooks'
import { SITE_TYPE_LABEL } from '../format'
import type { ProposalType } from '../types'
import { applyServerErrors, parseApiError } from '@/services/apiError'

const FORM_FIELDS = ['siteId', 'siteName', 'siteType', 'address', 'phoneNumber']
const DEFAULT_SLOT = { startTime: '07:30', endTime: '09:00', capacity: 10 }

const Field = ({ label, required, error, full, children }: {
    label: string; required?: boolean; error?: string; full?: boolean; children: ReactNode
}) => (
    <div className={`ds-field${full ? ' ds-field--full' : ''}`}>
        <label>{label}{required && <span className="ds-req"> *</span>}</label>
        {children}
        {error && <div className="ds-field__error">{error}</div>}
    </div>
)

/** Remount (key={mode}) mỗi khi đổi tab để schema + giá trị mặc định luôn đúng. */
export default function ProposalForm({ mode }: { mode: ProposalType }) {
    const { modal, message } = App.useApp()
    const qc = useQueryClient()
    const sites = useSites()

    const { control, handleSubmit, reset, setError, formState: { errors } } = useForm<ProposalFormValues>({
        resolver: zodResolver(makeProposalSchema(mode)),
        mode: 'onBlur',
        defaultValues: {
            siteId: undefined,
            siteName: '',
            siteType: 'FIXED',
            address: '',
            phoneNumber: '',
            slots: [{ ...DEFAULT_SLOT }],
        },
    })
    const { fields, append, remove, replace } = useFieldArray({ control, name: 'slots' })
    const selectedId = useWatch({ control, name: 'siteId' })
    const selectedSite = sites.data?.find((s) => s.siteId === selectedId)

    const create = useMutation({
        mutationFn: proposalsApi.create,
        onSuccess: async (p) => {
            await qc.invalidateQueries({ queryKey: ['proposals'] })
            modal.success({
                title: 'Gửi đề xuất thành công',
                content: `Đề xuất ${p.proposalCode} đã được gửi, đang chờ Quản lý phê duyệt.`,
                okText: 'Đóng',
            })
            reset()
            replace([{ ...DEFAULT_SLOT }])
        },
        onError: (err) => {
            const { message: msg, fieldErrors } = parseApiError(err)
            const applied = applyServerErrors(fieldErrors, setError, FORM_FIELDS)
            message.error(applied ? 'Vui lòng kiểm tra lại các trường được đánh dấu' : msg)
        },
    })

    const submit = handleSubmit((v) =>
        create.mutate(
            mode === 'NEW_SITE'
                ? {
                    proposalType: mode,
                    siteName: v.siteName,
                    siteType: v.siteType,
                    address: v.address,
                    phoneNumber: normalizePhone(v.phoneNumber),
                    slots: v.slots,
                }
                : { proposalType: mode, siteId: v.siteId, slots: v.slots },
        ),
    )

    return (
        <form onSubmit={submit} className="ds-form" noValidate>
            {mode === 'NEW_SITE' ? (
                <>
                    <Field label="Tên điểm hiến" required error={errors.siteName?.message}>
                        <Controller name="siteName" control={control} render={({ field }) => (
                            <Input {...field} size="large" placeholder="Nhập tên điểm hiến" status={errors.siteName ? 'error' : ''} />
                        )} />
                    </Field>
                    <Field label="Loại điểm hiến" required error={errors.siteType?.message}>
                        <Controller name="siteType" control={control} render={({ field }) => (
                            <Select {...field} size="large" options={[
                                { value: 'FIXED', label: SITE_TYPE_LABEL.FIXED },
                                { value: 'MOBILE', label: SITE_TYPE_LABEL.MOBILE },
                            ]} />
                        )} />
                    </Field>
                    <Field label="Địa chỉ" required full error={errors.address?.message}>
                        <Controller name="address" control={control} render={({ field }) => (
                            <Input {...field} size="large" placeholder="Nhập địa chỉ đầy đủ" status={errors.address ? 'error' : ''} />
                        )} />
                    </Field>
                    <Field label="Số điện thoại" required error={errors.phoneNumber?.message}>
                        <Controller name="phoneNumber" control={control} render={({ field }) => (
                            <Input {...field} size="large" inputMode="tel" placeholder="Ví dụ: 028 3895 1234" status={errors.phoneNumber ? 'error' : ''} />
                        )} />
                    </Field>
                </>
            ) : (
                <>
                    <Field label="Điểm hiến đang hoạt động" required full error={errors.siteId?.message}>
                        <Controller name="siteId" control={control} render={({ field }) => (
                            <Select
                                size="large"
                                showSearch
                                optionFilterProp="label"
                                placeholder="Chọn điểm hiến cần đổi lịch & sức chứa"
                                loading={sites.isLoading}
                                status={errors.siteId ? 'error' : ''}
                                value={field.value}
                                onChange={(id: number) => {
                                    field.onChange(id)
                                    const s = sites.data?.find((x) => x.siteId === id)
                                    if (s) replace(s.slots.map((x) => ({ ...x })))
                                }}
                                options={sites.data?.map((s) => ({
                                    value: s.siteId,
                                    label: `${s.siteName} (${SITE_TYPE_LABEL[s.siteType]})`,
                                }))}
                            />
                        )} />
                    </Field>
                    {selectedSite && (
                        <div className="ds-field ds-field--full ds-hint">
                            {selectedSite.address} · {selectedSite.siteCode} · Cấu hình hiện tại: {selectedSite.totalCapacityPerDay} lượt/ngày.
                            Khung giờ bên dưới đã điền sẵn từ cấu hình hiện tại, hãy chỉnh sửa theo đề xuất.
                        </div>
                    )}
                </>
            )}

            <SlotEditor
                control={control}
                errors={errors}
                count={fields.length}
                onAdd={() => append({ ...DEFAULT_SLOT })}
                onRemove={remove}
            />

            <div className="ds-form__foot">
                <Button type="primary" size="large" htmlType="submit" loading={create.isPending}>Gửi đề xuất</Button>
            </div>
        </form>
    )
}
