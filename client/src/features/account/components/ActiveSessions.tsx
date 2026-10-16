import { Button, Empty, Popconfirm, Skeleton } from 'antd'
import { DesktopOutlined, MobileOutlined, TabletOutlined } from '@ant-design/icons'
import type { DeviceType } from '@/types/auth'
import { useRevokeSession, useSessions } from '../hooks'
import { formatLoginTime } from '../utils/device'

const ICONS: Record<DeviceType, React.ReactNode> = {
    desktop: <DesktopOutlined />,
    mobile: <MobileOutlined />,
    tablet: <TabletOutlined />,
}

export default function ActiveSessions() {
    const { data: sessions, isLoading, isError, refetch } = useSessions()
    const revoke = useRevokeSession()

    return (
        <section className="pf-card">
            <header className="pf-card__head">
                <h3>Phiên đăng nhập đang hoạt động</h3>
                <p>Các thiết bị đang truy cập vào tài khoản của bạn.</p>
            </header>

            <div className="pf-sessions">
                {isLoading && <Skeleton active paragraph={{ rows: 3 }} />}

                {isError && (
                    <Empty description="Không tải được danh sách phiên đăng nhập">
                        <Button onClick={() => refetch()}>Thử lại</Button>
                    </Empty>
                )}

                {sessions?.map((s) => {
                    const meta = [s.ipAddress, s.location, `Đăng nhập lúc ${formatLoginTime(s.lastActiveAt)}`]
                        .filter((x) => x && x !== '—')
                        .join(' · ')

                    return (
                        <article className="pf-session" key={s.id}>
                            <div className="pf-session__icon" aria-hidden>{ICONS[s.deviceType]}</div>
                            <div className="pf-session__body">
                                <div className="pf-session__title">
                                    <strong>{s.browser} · {s.os}</strong>
                                    {s.isCurrent && <span className="pf-badge"><i />Phiên hiện tại</span>}
                                </div>
                                <small>{meta}</small>
                            </div>

                            {!s.isCurrent && (
                                <Popconfirm
                                    title="Đăng xuất khỏi thiết bị này?"
                                    description="Thiết bị sẽ phải đăng nhập lại."
                                    okText="Đăng xuất"
                                    cancelText="Hủy"
                                    onConfirm={() => revoke.mutate(s.id)}
                                >
                                    <Button
                                        loading={revoke.isPending && revoke.variables === s.id}
                                    >
                                        Đăng xuất khỏi thiết bị này
                                    </Button>
                                </Popconfirm>
                            )}
                        </article>
                    )
                })}
            </div>
        </section>
    )
}
