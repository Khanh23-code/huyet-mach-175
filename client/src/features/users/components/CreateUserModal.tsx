import type { ReactNode } from 'react'
import { App, Button, Input, Modal, Select } from 'antd'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createUserSchema, type CreateUserForm } from '../userSchema'
import { useDepartments, useRoles } from '../hooks'
import { usersApi } from '../usersApi'

interface Props { open: boolean; onClose: () => void }

const Field = ({ label, error, children }: { label: string; error?: string; children: ReactNode }) => (
    <div className="um-field">
        <label>{label}</label>
        {children}
        {error && <div className="um-field__error">{error}</div>}
    </div>
)

export default function CreateUserModal({ open, onClose }: Props) {
    const { message } = App.useApp()
    const qc = useQueryClient()
    const roles = useRoles()
    const departments = useDepartments()

    const { control, handleSubmit, reset, formState: { errors } } = useForm<CreateUserForm>({
        resolver: zodResolver(createUserSchema),
        defaultValues: {
            staffCode: '', username: '', fullName: '', password: '',
            email: '', phoneNumber: '', roleIds: [],
        },
    })

    const close = () => { reset(); onClose() }

    const create = useMutation({
        mutationFn: usersApi.create,
        onSuccess: () => {
            message.success('Đã tạo người dùng')
            qc.invalidateQueries({ queryKey: ['users'] })
            close()
        },
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        onError: (err: any) => {
            const d = err?.response?.data
            message.error(d?.message ?? d?.title ?? 'Không thể tạo người dùng, vui lòng thử lại')
        },
    })

    const submit = handleSubmit((v) =>
        create.mutate({
            ...v,
            email: v.email || null,
            phoneNumber: v.phoneNumber || null,
        }),
    )

    return (
        <Modal
            title="Thêm người dùng"
            open={open}
            onCancel={close}
            destroyOnHidden
            width={620}
            footer={[
                <Button key="c" onClick={close}>Hủy</Button>,
                <Button key="s" type="primary" loading={create.isPending} onClick={submit}>Tạo người dùng</Button>,
            ]}
        >
            <form onSubmit={submit} className="um-form">
                <Field label="Họ và tên" error={errors.fullName?.message}>
                    <Controller name="fullName" control={control} render={({ field }) => (
                        <Input {...field} status={errors.fullName ? 'error' : ''} />
                    )} />
                </Field>

                <div className="um-form__row">
                    <Field label="Mã nhân viên" error={errors.staffCode?.message}>
                        <Controller name="staffCode" control={control} render={({ field }) => (
                            <Input {...field} placeholder="175-01245" status={errors.staffCode ? 'error' : ''} />
                        )} />
                    </Field>
                    <Field label="Tên đăng nhập" error={errors.username?.message}>
                        <Controller name="username" control={control} render={({ field }) => (
                            <Input {...field} status={errors.username ? 'error' : ''} />
                        )} />
                    </Field>
                </div>

                <Field label="Mật khẩu" error={errors.password?.message}>
                    <Controller name="password" control={control} render={({ field }) => (
                        <Input.Password {...field} autoComplete="new-password" status={errors.password ? 'error' : ''} />
                    )} />
                </Field>

                <div className="um-form__row">
                    <Field label="Email" error={errors.email?.message}>
                        <Controller name="email" control={control} render={({ field }) => (
                            <Input {...field} status={errors.email ? 'error' : ''} />
                        )} />
                    </Field>
                    <Field label="Số điện thoại" error={errors.phoneNumber?.message}>
                        <Controller name="phoneNumber" control={control} render={({ field }) => (
                            <Input {...field} status={errors.phoneNumber ? 'error' : ''} />
                        )} />
                    </Field>
                </div>

                <Field label="Khoa / phòng" error={errors.departmentId?.message}>
                    <Controller name="departmentId" control={control} render={({ field }) => (
                        <Select
                            {...field}
                            placeholder="Chọn khoa/phòng"
                            loading={departments.isLoading}
                            status={errors.departmentId ? 'error' : ''}
                            options={departments.data?.map((d) => ({ value: d.id, label: d.name }))}
                        />
                    )} />
                </Field>

                <Field label="Vai trò (chọn được nhiều)" error={errors.roleIds?.message}>
                    <Controller name="roleIds" control={control} render={({ field }) => (
                        <Select
                            {...field}
                            mode="multiple"
                            placeholder="Chọn vai trò"
                            loading={roles.isLoading}
                            status={errors.roleIds ? 'error' : ''}
                            options={roles.data?.map((r) => ({ value: r.id, label: `${r.code} · ${r.name}` }))}
                        />
                    )} />
                </Field>
            </form>
        </Modal>
    )
}
