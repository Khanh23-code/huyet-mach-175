import { useMemo, useState, type ReactNode } from 'react'
import { App, AutoComplete, Button, Checkbox, Input, Modal, Select } from 'antd'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'

import { ACTIONS } from '@/config/rbac'
import { MODULE_LABEL_OVERRIDES } from '@/config/permissionDefaults'
import { applyServerErrors, parseApiError } from '@/services/apiError'
import {
    createPermissionSchema,
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
    /** Gọi sau khi tạo xong nếu người dùng chọn gán luôn cho vai trò đang chọn. */
    onCreated?: (permission: PermissionDto) => void
}

const Field = ({ label, error, children }: { label: string; error?: string; children: ReactNode }) => (
    <div className="um-field">
        <label>{label}</label>
        {children}
        {error && <div className="um-field__error">{error}</div>}
    </div>
)

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

    const { control, handleSubmit, reset, watch, setError, formState: { errors } } = useForm<CreatePermissionForm>({
        resolver: zodResolver(schema),
        mode: 'onBlur',
        defaultValues: { moduleCode: '', moduleName: '', permissionName: '', description: '' },
    })

    const codeInput = (watch('moduleCode') ?? '').trim().toUpperCase()
    const actionInput = watch('actionType')
    const found = modules.find((m) => m.code === codeInput)

    const close = () => { reset(); onClose() }

    const create = useMutation({
        mutationFn: permissionsApi.create,
        onSuccess: async (p) => {
            // Mọi role đều cần thấy quyền/module mới trong ma trận.
            await qc.invalidateQueries({ queryKey: ['role-permissions'] })
            await qc.invalidateQueries({ queryKey: ['permissions'] })
            message.success(`Đã tạo quyền ${p.permissionCode}`)
            if (assign && roleLabel) onCreated?.(p)
            close()
        },
        onError: (err) => {
            const { message: msg, fieldErrors } = parseApiError(err)
            const applied = applyServerErrors(fieldErrors, setError,
                ['moduleCode', 'moduleName', 'actionType', 'permissionName', 'description'])
            message.error(applied ? 'Vui lòng kiểm tra lại các trường được đánh dấu' : msg)
        },
    })

    const submit = handleSubmit((v) =>
        create.mutate({
            moduleCode: v.moduleCode.trim().toUpperCase(),
            moduleName: found?.name ?? v.moduleName,
            actionType: v.actionType,
            permissionName: v.permissionName,
            description: v.description || undefined,
        }),
    )

    return (
        <Modal
            title="Thêm quyền mới"
            open={open}
            onCancel={close}
            destroyOnHidden
            width={560}
            footer={[
                <Button key="c" onClick={close}>Hủy</Button>,
                <Button key="s" type="primary" loading={create.isPending} onClick={submit}>Tạo quyền</Button>,
            ]}
        >
            <form onSubmit={submit} className="um-form">
                <Field label="Mã module (chọn có sẵn hoặc nhập mới)" error={errors.moduleCode?.message}>
                    <Controller name="moduleCode" control={control} render={({ field }) => (
                        <AutoComplete
                            {...field}
                            style={{ width: '100%' }}
                            placeholder="VD: INVENTORY"
                            status={errors.moduleCode ? 'error' : ''}
                            options={modules.map((m) => ({ value: m.code, label: `${m.code} – ${m.name}` }))}
                            filterOption={(input, option) =>
                                String(option?.value ?? '').toUpperCase().includes(input.toUpperCase())
                            }
                        />
                    )} />
                </Field>

                <Field
                    label={found ? 'Tên module (đã có, không đổi được)' : 'Tên module mới'}
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

                <Field label="Thao tác" error={errors.actionType?.message}>
                    <Controller name="actionType" control={control} render={({ field }) => (
                        <Select
                            {...field}
                            placeholder="Chọn thao tác"
                            status={errors.actionType ? 'error' : ''}
                            options={ACTIONS.map((a) => ({
                                value: a.key,
                                label: `${a.label} (${a.key})`,
                                disabled: Boolean(found?.actions.includes(a.key)),
                            }))}
                        />
                    )} />
                </Field>

                <Field label="Tên quyền" error={errors.permissionName?.message}>
                    <Controller name="permissionName" control={control} render={({ field }) => (
                        <Input {...field} status={errors.permissionName ? 'error' : ''} />
                    )} />
                </Field>

                <Field label="Mô tả (bỏ trống sẽ dùng mô tả của module)" error={errors.description?.message}>
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
                    Mã quyền sẽ là: <strong>{codeInput && actionInput ? `${codeInput}_${actionInput}` : '—'}</strong>.
                    Quyền mới chưa gán cho vai trò nào (trừ SYS).
                </p>
            </form>
        </Modal>
    )
}