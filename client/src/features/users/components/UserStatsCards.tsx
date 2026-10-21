import { CheckOutlined, ExclamationOutlined, TeamOutlined } from '@ant-design/icons'
import { useUserCount } from '../hooks'

const pad = (n?: number) => (n === undefined ? '–' : String(n).padStart(2, '0'))

export default function UserStatsCards() {
    const total = useUserCount()
    const active = useUserCount({ isActive: true })

    // BE hiện bỏ qua isActive=false (trả cả người đang hoạt động), nên không gọi trực tiếp.
    // Số bị khóa = tổng − đang hoạt động. Cách này đúng cả trước và sau khi BE sửa lỗi lọc.
    const locked =
        total.data !== undefined && active.data !== undefined
            ? total.data - active.data
            : undefined

    const cards = [
        { icon: <TeamOutlined />, tone: 'blue', value: total.data, label: 'Tổng người dùng' },
        { icon: <CheckOutlined />, tone: 'green', value: active.data, label: 'Đang hoạt động' },
        { icon: <ExclamationOutlined />, tone: 'red', value: locked, label: 'Tài khoản bị khóa' },
    ]

    return (
        <div className="um-stats">
            {cards.map((c) => (
                <div className="um-stat" key={c.label}>
                    <div className={`um-stat__icon um-stat__icon--${c.tone}`}>{c.icon}</div>
                    <div>
                        <div className="um-stat__value">{pad(c.value)}</div>
                        <div className="um-stat__label">{c.label}</div>
                    </div>
                </div>
            ))}
        </div>
    )
}