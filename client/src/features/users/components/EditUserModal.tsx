import { useEffect, useMemo, type ReactNode } from 'react'
import { App, Button, Input, Modal, Select } from 'antd'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { editUserSchema, type EditUserForm } from '../userSchema'
import { useDepartments, useRoles } from '../hooks'
import { usersApi } from '../usersApi'
import { applyServerErrors, parseApiError } from '@/services/apiError'
import type { UserItem } from '@/types/user'

interface Props {
    /** null = đóng modal */
    user: UserItem | null
    /** Đang sửa chính mình: khóa phần vai trò để tránh tự tước quyền */
    isSelf: boolean
    /** Chỉ SYS mới được gán vai trò SYS (BE trả 403 nếu MGT làm) */
    canAssignSys: boolean
    onClose: () => void
}

const Field = ({ label, error, children }: { label: string; error?: string; children: ReactNode }) => (
    <div className="um-field">
        <label>{label}</label>
        {children}
        {error && <div className="um-field__error">{error}</div>}
    </div>
)

const FORM_FIELDS = Object.keys(editUserSchema.shape)

const sameIds = (a: number[], b: number[]) => {
    const x = [...a].sort((m, n) => m - n)
    const y = [...b].sort((m, n) => m - n)
    return x.length === y.length && x.every((v, i) => v === y[i])
}

export default function EditUserModal({ user, isSelf, canAssignSys, onClose }: Props) {
    const { message } = App.useApp()
    const qc = useQueryClient()
    const roles = useRoles()
    const departments = useDepartments()

    // API danh sách chỉ trả mã vai trò -> đổi sang id để gửi lên BE
    const originalRoleIds = useMemo(
        () => (user && roles.data ? roles.data.filter((r) => user.roles.includes(r.code)).map((r) => r.id) : []),
        [user, roles.data],
    )

    const { control, handleSubmit, reset, setError, formState: { errors } } = useForm<EditUserForm>({
        resolver: zodResolver(editUserSchema),
        mode: 'onBlur',
        defaultValues: { fullName: '', email: '', phoneNumber: '', roleIds: [] },
    })

    // Nạp dữ liệu mỗi khi mở cho user khác (hoặc danh sách vai trò vừa tải xong)
    useEffect(() => {
        if (!user) return
        reset({
            fullName: user.fullName,
            email: user.email ?? '',
            phoneNumber: user.phoneNumber ?? '',
            departmentId: user.departmentId ?? undefined,
            roleIds: originalRoleIds,
        })
    }, [user, originalRoleIds, reset])

    const save = useMutation({
        mutationFn: async (v: EditUserForm) => {
            if (!user) return
            await usersApi.update(user.id, {
                fullName: v.fullName,
                email: v.email || null,
                phoneNumber: v.phoneNumber || null,
                departmentId: v.departmentId,
            })
            // Chỉ gọi API vai trò khi thật sự có thay đổi
            if (!isSelf && !sameIds(v.roleIds, originalRoleIds)) {
                await usersApi.updateRoles(user.id, v.roleIds)
            }
        },
        onSuccess: async () => {
            await Promise.all([
                qc.invalidateQueries({ queryKey: ['users'] }),
                qc.invalidateQueries({ queryKey: ['roles'] }), // số tài khoản theo vai trò đổi
            ])
            message.success('Đã cập nhật người dùng')
            onClose()
        },
        onError: (err) => {
            // Có thể thông tin đã lưu nhưng bước vai trò lỗi -> làm mới danh sách cho khớp
            qc.invalidateQueries({ queryKey: ['users'] })
            const { message: msg, fieldErrors } = parseApiError(err)
            const applied = applyServerErrors(fieldErrors, setError, FORM_FIELDS)
            message.error(applied ? 'Vui lòng kiểm tra lại các trường được đánh dấu' : msg)
        },
    })

    const submit = handleSubmit((v) => save.mutate(v))

    return (
        <Modal
            title="Chỉnh sửa người dùng"
            open={!!user}
            onCancel={onClose}
            destroyOnHidden
            width={620}
            footer={[
                <Button key="c" onClick={onClose}>Hủy</Button>,
                <Button key="s" type="primary" loading={save.isPending} onClick={submit}>Lưu thay đổi</Button>,
            ]}
        >
            <form onSubmit={submit} className="um-form">
                <div className="um-form__row">
                    <Field label="Mã nhân viên">
                        <Input value={user?.staffCode} disabled />
                    </Field>
                    <Field label="Tên đăng nhập">
                        <Input value={user?.username} disabled />
                    </Field>
                </div>

                <Field label="Họ và tên" error={errors.fullName?.message}>
                    <Controller name="fullName" control={control} render={({ field }) => (
                        <Input {...field} status={errors.fullName ? 'error' : ''} />
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

                <Field
                    label={isSelf ? 'Vai trò (không thể tự đổi vai trò của chính mình)' : 'Vai trò (chọn được nhiều)'}
                    error={errors.roleIds?.message}
                >
                    <Controller name="roleIds" control={control} render={({ field }) => (
                        <Select
                            {...field}
                            mode="multiple"
                            disabled={isSelf}
                            placeholder="Chọn vai trò"
                            loading={roles.isLoading}
                            status={errors.roleIds ? 'error' : ''}
                            options={roles.data
                                // giữ lại vai trò đang có dù đã bị vô hiệu hóa để không bị mất âm thầm
                                ?.filter((r) => r.isActive || originalRoleIds.includes(r.id))
                                .map((r) => ({
                                    value: r.id,
                                    label: `${r.code} · ${r.name}`,
                                    disabled: r.code === 'SYS' && !canAssignSys,
                                }))}
                        />
                    )} />
                </Field>
            </form>
        </Modal>
    )
}