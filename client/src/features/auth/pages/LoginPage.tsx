import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { Button, Input, Checkbox, message } from 'antd'
import { InfoCircleOutlined } from '@ant-design/icons'
import { Navigate, useNavigate } from 'react-router-dom'
import { authApi } from '../authApi'
import { useAuthStore } from '../authStore'
import { loginSchema, type LoginForm } from '../authSchema'
import './LoginPage.css'

export default function LoginPage() {
    const navigate = useNavigate()
    const { accessToken, setTokens, setUser } = useAuthStore()

    const {
        control,
        handleSubmit,
        formState: { errors },
    } = useForm<LoginForm>({
        resolver: zodResolver(loginSchema),
        defaultValues: { username: '', password: '', remember: false },
    })

    const loginMutation = useMutation({
        mutationFn: authApi.login,
        onSuccess: async (res) => {
            setTokens(res.accessToken, res.refreshToken)
            const user = res.user ?? (await authApi.me())
            setUser(user)
            navigate('/', { replace: true })
        },
        onError: (err: any) => {
            const status = err?.response?.status
            message.error(
                status === 401 || status === 400
                    ? 'Mã nhân viên hoặc mật khẩu không đúng'
                    : 'Không thể kết nối máy chủ, vui lòng thử lại'
            )
        },
    })

    if (accessToken) return <Navigate to="/" replace />

    return (
        <div className="login">
            {/* ===== Cột trái ===== */}
            <section className="login__left">
                <div className="login__circle login__circle--top" />
                <div className="login__circle login__circle--bottom" />

                <div className="login__brand">
                    <div className="login__logo">
                        <svg width="30" height="30" viewBox="0 0 24 24" fill="#1b7a3a">
                            <path d="M12 2C12 2 5 10 5 15a7 7 0 0014 0c0-5-7-13-7-13z" />
                        </svg>
                    </div>
                    <div>
                        <div className="login__hospital">BỆNH VIỆN QUÂN Y 175</div>
                        <div className="login__app">Điều Phối Máu 175</div>
                    </div>
                </div>

                <div className="login__hero">
                    <span className="login__tag">HỆ THỐNG QUẢN LÝ NỘI BỘ</span>
                    <h1>
                        Kết nối từng đơn vị máu.
                        <br />
                        Bảo vệ từng sự sống.
                    </h1>
                    <p>
                        Nền tảng điều phối tập trung, hỗ trợ nhân viên y tế ra quyết định nhanh chóng,
                        chính xác và an toàn.
                    </p>
                </div>

                <div className="login__status">
                    <span className="login__dot" /> HIS/LIS đang kết nối ổn định
                </div>
            </section>

            {/* ===== Cột phải ===== */}
            <section className="login__right">
                <form
                    className="login__card"
                    onSubmit={handleSubmit((v) =>
                        loginMutation.mutate({ username: v.username, password: v.password })
                    )}
                >
                    <div className="login__eyebrow">ĐĂNG NHẬP</div>
                    <h2>Chào mừng trở lại</h2>
                    <p className="login__sub">Sử dụng tài khoản nhân viên bệnh viện để tiếp tục.</p>

                    <label className="login__label">Mã nhân viên</label>
                    <Controller
                        name="username"
                        control={control}
                        render={({ field }) => (
                            <Input
                                {...field}
                                size="large"
                                placeholder="Ví dụ: 175-01245"
                                status={errors.username ? 'error' : ''}
                            />
                        )}
                    />
                    {errors.username && <div className="login__error">{errors.username.message}</div>}

                    <label className="login__label">Mật khẩu</label>
                    <Controller
                        name="password"
                        control={control}
                        render={({ field }) => (
                            <Input.Password
                                {...field}
                                size="large"
                                placeholder="Nhập mật khẩu"
                                status={errors.password ? 'error' : ''}
                            />
                        )}
                    />
                    {errors.password && <div className="login__error">{errors.password.message}</div>}

                    <div className="login__options">
                        <Controller
                            name="remember"
                            control={control}
                            render={({ field }) => (
                                <Checkbox
                                    checked={field.value}
                                    onChange={(e) => field.onChange(e.target.checked)}
                                >
                                    Ghi nhớ đăng nhập
                                </Checkbox>
                            )}
                        />

                        <a
                            className="login__forgotLink"
                            href="#"
                            onClick={(e) => e.preventDefault()}
                        >
                            Quên mật khẩu?
                        </a>
                    </div>

                    <Button
                        type="primary"
                        htmlType="submit"
                        size="large"
                        block
                        loading={loginMutation.isPending}
                        style={{ marginTop: 20, height: 48, fontWeight: 600 }}
                    >
                        Đăng nhập
                    </Button>

                    <div className="login__info">
                        <InfoCircleOutlined /> Hệ thống chỉ truy cập được trong mạng nội bộ bệnh viện.
                    </div>

                    <p className="login__help">Cần hỗ trợ? Liên hệ Trung tâm CNTT · Máy lẻ 115</p>
                </form>
            </section>
        </div>
    )
}