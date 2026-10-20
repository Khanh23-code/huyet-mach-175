import { Alert, Button, Form, Input } from 'antd'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { isAxiosError } from 'axios'
import { changePasswordSchema, type ChangePasswordForm } from '@/features/auth/authSchema'
import { useChangePassword } from '../hooks'

const DEFAULTS: ChangePasswordForm = { currentPassword: '', newPassword: '', confirmNewPassword: '' }

const FIELDS: { name: keyof ChangePasswordForm; label: string; autoComplete: string }[] = [
    { name: 'currentPassword', label: 'Mật khẩu hiện tại', autoComplete: 'current-password' },
    { name: 'newPassword', label: 'Mật khẩu mới', autoComplete: 'new-password' },
    { name: 'confirmNewPassword', label: 'Xác nhận mật khẩu mới', autoComplete: 'new-password' },
]

export default function ChangePasswordFormCard() {
    const { control, handleSubmit, reset, setError, formState: { errors } } = useForm<ChangePasswordForm>({
        resolver: zodResolver(changePasswordSchema),
        defaultValues: DEFAULTS,
        mode: 'onBlur',
        reValidateMode: 'onChange',
    })
    const change = useChangePassword()

    const onSubmit = handleSubmit((values) =>
        change.mutate(values, {
            onSuccess: () => reset(DEFAULTS),
            onError: (err) => {
                // Backend nên trả 400 khi sai mật khẩu hiện tại (không dùng 401 vì sẽ kích hoạt refresh-token)
                if (isAxiosError(err) && err.response?.status === 400) {
                    const msg = (err.response.data as { message?: string; title?: string })
                    setError('currentPassword', {
                        message: msg?.message ?? msg?.title ?? 'Mật khẩu hiện tại không đúng',
                    })
                }
            },
        }),
    )

    return (
        <section className="pf-card">
            <header className="pf-card__head">
                <h3>Đổi mật khẩu</h3>
                <p>Mật khẩu mới phải có ít nhất 8 ký tự, bao gồm chữ hoa và số.</p>
            </header>

            <Form layout="vertical" className="pf-pw" onFinish={onSubmit} requiredMark="optional" noValidate>
                {FIELDS.map((f) => (
                    <Form.Item
                        key={f.name}
                        label={f.label}
                        required
                        validateStatus={errors[f.name] ? 'error' : undefined}
                        help={errors[f.name]?.message}
                    >
                        <Controller
                            name={f.name}
                            control={control}
                            render={({ field }) => (
                                <Input.Password {...field} size="large" autoComplete={f.autoComplete} placeholder="••••••••" />
                            )}
                        />
                    </Form.Item>
                ))}

                <Alert
                    type="warning"
                    showIcon
                    title="Lưu ý bảo mật"
                    description="Sau khi đổi mật khẩu, tất cả các phiên đăng nhập khác sẽ bị đăng xuất ngay lập tức."
                />

                <div className="pf-pw__actions">
                    <Button size="large" onClick={() => reset(DEFAULTS)} disabled={change.isPending}>Hủy</Button>
                    <Button size="large" type="primary" htmlType="submit" loading={change.isPending}>
                        Cập nhật mật khẩu
                    </Button>
                </div>
            </Form>
        </section>
    )
}
