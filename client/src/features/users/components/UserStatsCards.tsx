import { CheckOutlined, ExclamationOutlined, TeamOutlined } from '@ant-design/icons'
import { useUserCount } from '../hooks'

const pad = (n?: number) => (n === undefined ? '–' : String(n).padStart(2, '0'))

export default function UserStatsCards() {
    const total = useUserCount()
    const active = useUserCount({ isActive: true })
    const locked = useUserCount({ isActive: false })

    const cards = [
        { icon: <TeamOutlined />, tone: 'blue', value: total.data, label: 'Tổng người dùng' },
        { icon: <CheckOutlined />, tone: 'green', value: active.data, label: 'Đang hoạt động' },
        { icon: <ExclamationOutlined />, tone: 'red', value: locked.data, label: 'Tài khoản bị khóa' },
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
