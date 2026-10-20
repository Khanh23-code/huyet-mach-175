import type { ReactNode } from 'react'
import { App, Button, Input, Modal } from 'antd'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createRoleSchema, type CreateRoleForm } from '../roleSchema'
import { rolesApi, type RoleDto } from '../rolesApi'
import { applyServerErrors, parseApiError } from '@/services/apiError'

interface Props {
    open: boolean
    onClose: () => void
    onCreated?: (role: RoleDto) => void
}

const Field = ({ label, error, children }: { label: string; error?: string; children: ReactNode }) => (
    <div className="um-field">
        <label>{label}</label>
        {children}
        {error && <div className="um-field__error">{error}</div>}
    </div>
)

export default function CreateRoleModal({ open, onClose, onCreated }: Props) {
    const { message } = App.useApp()
    const qc = useQueryClient()

    const { control, handleSubmit, reset, setError, formState: { errors } } = useForm<CreateRoleForm>({
        resolver: zodResolver(createRoleSchema),
        mode: 'onBlur',
        defaultValues: { roleCode: '', roleName: '', description: '' },
    })

    const close = () => { reset(); onClose() }

    const create = useMutation({
        mutationFn: rolesApi.create,
        onSuccess: async (role) => {
            // Role mới phải có trong danh sách trước khi onCreated chọn nó
            await qc.invalidateQueries({ queryKey: ['roles'] })
            message.success(`Đã tạo vai trò ${role.roleCode}`)
            onCreated?.(role)
            close()
        },
        onError: (err) => {
            const { message: msg, fieldErrors } = parseApiError(err)
            const applied = applyServerErrors(fieldErrors, setError, ['roleCode', 'roleName', 'description'])
            message.error(applied ? 'Vui lòng kiểm tra lại các trường được đánh dấu' : msg)
        },
    })

    const submit = handleSubmit((v) =>
        create.mutate({
            roleCode: v.roleCode.toUpperCase(),
            roleName: v.roleName,
            description: v.description || null,
        }),
    )

    return (
        <Modal
            title="Thêm vai trò mới"
            open={open}
            onCancel={close}
            destroyOnHidden
            width={520}
            footer={[
                <Button key="c" onClick={close}>Hủy</Button>,
                <Button key="s" type="primary" loading={create.isPending} onClick={submit}>Tạo vai trò</Button>,
            ]}
        >
            <form onSubmit={submit} className="um-form">
                <Field label="Mã vai trò" error={errors.roleCode?.message}>
                    <Controller name="roleCode" control={control} render={({ field }) => (
                        <Input
                            {...field}
                            placeholder="VD: LAB"
                            onChange={(e) => field.onChange(e.target.value.toUpperCase())}
                            status={errors.roleCode ? 'error' : ''}
                        />
                    )} />
                </Field>

                <Field label="Tên vai trò" error={errors.roleName?.message}>
                    <Controller name="roleName" control={control} render={({ field }) => (
                        <Input {...field} status={errors.roleName ? 'error' : ''} />
                    )} />
                </Field>

                <Field label="Mô tả" error={errors.description?.message}>
                    <Controller name="description" control={control} render={({ field }) => (
                        <Input.TextArea {...field} rows={3} status={errors.description ? 'error' : ''} />
                    )} />
                </Field>

                <p className="um-muted">
                    Vai trò mới chưa có quyền nào. Sau khi tạo, hãy gán quyền trong ma trận.
                </p>
            </form>
        </Modal>
    )
}