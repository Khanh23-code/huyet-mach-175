import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { Alert, App, AutoComplete, Button, Checkbox, Input, Modal } from 'antd'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'

import { ACTIONS } from '@/config/rbac'
import { MODULE_LABEL_OVERRIDES } from '@/config/permissionDefaults'
import { applyServerErrors, parseApiError } from '@/services/apiError'
import {
    createPermissionSchema,
    isModuleFull,
    type CreatePermissionForm,
    type ExistingModule,
} from '../permissionSchema'
import { permissionsApi, type PermissionDto } from '../permissionsApi'

interface Props {
    open: boolean
    onClose: () => void
    /** Toàn bộ permission hiện có (lấy từ dữ liệu ma trận) để gợi ý module. */
    permissions: PermissionDto[]
    /** Mã vai trò đang chọn trong ma trận. Bỏ trống (vd. SYS) thì không hiện ô "gán luôn". */
    roleLabel?: string
    /** Gọi MỘT lần với toàn bộ quyền vừa tạo nếu người dùng chọn gán luôn cho vai trò đang chọn. */
    onCreated?: (permissions: PermissionDto[]) => void
}

const Field = ({ label, error, children }: { label: string; error?: string; children: ReactNode }) => (
    <div className="um-field">
        <label>{label}</label>
        {children}
        {error && <div className="um-field__error">{error}</div>}
    </div>
)

const actionLabel = (key: string) => ACTIONS.find((a) => a.key === key)?.label ?? key

export default function CreatePermissionModal({ open, onClose, permissions, roleLabel, onCreated }: Props) {
    const { message } = App.useApp()
    const qc = useQueryClient()
    const [assign, setAssign] = useState(true)

    const modules = useMemo<ExistingModule[]>(() => {
        const map = new Map<string, ExistingModule>()
        for (const p of permissions) {
            const code = p.moduleCode.toUpperCase()
            const m = map.get(code) ?? {
                code,
                name: MODULE_LABEL_OVERRIDES[p.moduleCode]?.name ?? p.moduleName,
                actions: [],
            }
            m.actions.push(p.actionType.toUpperCase())
            map.set(code, m)
        }
        return [...map.values()]
    }, [permissions])

    const schema = useMemo(() => createPermissionSchema(modules), [modules])

    const { control, handleSubmit, reset, watch, setValue, getValues, setError, formState: { errors } } =
        useForm<CreatePermissionForm>({
            resolver: zodResolver(schema),
            mode: 'onBlur',
            defaultValues: { moduleCode: '', moduleName: '', actionTypes: [], description: '' },
        })

    const codeInput = (watch('moduleCode') ?? '').trim().toUpperCase()
    const selected = watch('actionTypes') ?? []
    const found = modules.find((m) => m.code === codeInput)
    const full = isModuleFull(found)
    const missing = ACTIONS.filter((a) => !found?.actions.includes(a.key))

    // Đổi sang module khác -> bỏ các thao tác đã tồn tại khỏi lựa chọn
    useEffect(() => {
        if (!found) return
        const cur = getValues('actionTypes') ?? []
        const next = cur.filter((a) => !found.actions.includes(a))
        if (next.length !== cur.length) setValue('actionTypes', next)
    }, [found, getValues, setValue])

    const close = () => { reset(); onClose() }

    const create = useMutation({
        mutationFn: permissionsApi.createBatch,
        onSuccess: async ({ created, skipped }) => {
            // Mọi role đều cần thấy quyền/module mới trong ma trận.
            await qc.invalidateQueries({ queryKey: ['role-permissions'] })
            await qc.invalidateQueries({ queryKey: ['permissions'] })
            message.success(`Đã tạo ${created.length} quyền: ${created.map((p) => p.permissionCode).join(', ')}`)
            if (skipped.length) message.warning(`Bỏ qua ${skipped.length} thao tác đã tồn tại: ${skipped.join(', ')}`)
            if (assign && roleLabel && created.length) onCreated?.(created)
            close()
        },
        onError: (err) => {
            const { message: msg, fieldErrors } = parseApiError(err)
            const applied = applyServerErrors(fieldErrors, setError, ['moduleCode', 'moduleName', 'description'])
            message.error(applied ? 'Vui lòng kiểm tra lại các trường được đánh dấu' : msg)
        },
    })

    const submit = handleSubmit((v) => {
        const moduleName = found?.name ?? v.moduleName
        create.mutate({
            moduleCode: v.moduleCode.trim().toUpperCase(),
            moduleName,
            description: v.description || undefined,
            // Tên quyền tự sinh theo thao tác, ví dụ "Xem Quản lý chiến dịch"
            actions: v.actionTypes.map((a) => ({
                actionType: a,
                permissionName: `${actionLabel(a)} ${moduleName}`.slice(0, 100),
            })),
        })
    })

    return (
        <Modal
            title="Thêm quyền mới"
            open={open}
            onCancel={close}
            destroyOnHidden
            width={560}
            footer={[
                <Button key="c" onClick={close}>Hủy</Button>,
                <Button key="s" type="primary" loading={create.isPending} disabled={full} onClick={submit}>
                    {selected.length > 1 ? `Tạo ${selected.length} quyền` : 'Tạo quyền'}
                </Button>,
            ]}
        >
            <form onSubmit={submit} className="um-form">
                <Field label="Mã phân hệ (chọn có sẵn hoặc nhập mới)" error={errors.moduleCode?.message}>
                    <Controller name="moduleCode" control={control} render={({ field }) => (
                        <AutoComplete
                            {...field}
                            style={{ width: '100%' }}
                            placeholder="VD: INVENTORY"
                            status={errors.moduleCode ? 'error' : ''}
                            options={modules.map((m) => ({
                                value: m.code,
                                label: `${m.code} – ${m.name}${isModuleFull(m) ? ' (đã đủ thao tác)' : ''}`,
                            }))}
                            filterOption={(input, option) =>
                                String(option?.value ?? '').toUpperCase().includes(input.toUpperCase())
                            }
                        />
                    )} />
                </Field>

                {found && full && (
                    <Alert
                        type="warning"
                        showIcon
                        message={`Phân hệ ${found.code} đã có đủ ${ACTIONS.length} thao tác`}
                        description="Không thể thêm quyền mới cho phân hệ này. Hãy chọn phân hệ khác hoặc nhập mã phân hệ mới."
                        style={{ marginBottom: 12 }}
                    />
                )}
                {found && !full && (
                    <Alert
                        type="info"
                        showIcon
                        message={`Phân hệ ${found.code} đã có: ${ACTIONS.filter((a) => found.actions.includes(a.key)).map((a) => a.label).join(', ')}`}
                        description={`Có thể thêm: ${missing.map((a) => a.label).join(', ')}`}
                        style={{ marginBottom: 12 }}
                    />
                )}

                <Field
                    label={found ? 'Tên phân hệ (đã có, không đổi được)' : 'Tên phân hệ mới'}
                    error={errors.moduleName?.message}
                >
                    <Controller name="moduleName" control={control} render={({ field }) => (
                        <Input
                            {...field}
                            value={found ? found.name : field.value}
                            disabled={Boolean(found)}
                            status={errors.moduleName ? 'error' : ''}
                        />
                    )} />
                </Field>

                <Field label="Thao tác (chọn được nhiều)" error={errors.actionTypes?.message}>
                    <Controller name="actionTypes" control={control} render={({ field }) => (
                        <>
                            <Checkbox.Group
                                value={field.value}
                                onChange={(v) => field.onChange(v)}
                                disabled={full}
                                options={ACTIONS.map((a) => ({
                                    value: a.key,
                                    label: `${a.label}${found?.actions.includes(a.key) ? ' (đã có)' : ''}`,
                                    disabled: Boolean(found?.actions.includes(a.key)),
                                }))}
                            />
                            {!full && missing.length > 1 && (
                                <Button
                                    type="link"
                                    size="small"
                                    onClick={() => setValue('actionTypes', missing.map((a) => a.key), { shouldValidate: true })}
                                >
                                    Chọn tất cả thao tác còn thiếu
                                </Button>
                            )}
                        </>
                    )} />
                </Field>

                <Field label="Mô tả (bỏ trống sẽ dùng mô tả của phân hệ)" error={errors.description?.message}>
                    <Controller name="description" control={control} render={({ field }) => (
                        <Input.TextArea {...field} rows={2} status={errors.description ? 'error' : ''} />
                    )} />
                </Field>

                {roleLabel && (
                    <Checkbox checked={assign} onChange={(e) => setAssign(e.target.checked)}>
                        Gán luôn cho vai trò <strong>{roleLabel}</strong> (chỉ vào bản nháp, bấm "Lưu thay đổi" để lưu)
                    </Checkbox>
                )}

                <p className="um-muted">
                    Mã quyền sẽ tạo:{' '}
                    <strong>
                        {codeInput && selected.length
                            ? selected.map((a) => `${codeInput}_${a}`).join(', ')
                            : '—'}
                    </strong>
                    . Tên quyền tự sinh theo thao tác. Quyền mới chưa gán cho vai trò nào (trừ SYS).
                </p>
            </form>
        </Modal>
    )
}