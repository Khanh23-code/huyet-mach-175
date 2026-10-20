import { useMemo, useState } from 'react'
import { App, Button, Checkbox, Input, Tooltip } from 'antd'
import { PlusOutlined, SearchOutlined } from '@ant-design/icons'
import {
    ACTIONS, MODULES, ROLE_CODES, ROLE_META,
    type ActionKey, type ModuleCode, type RoleMatrix,
} from '@/config/rbac'
import type { RoleCode } from '@/types/user'
import { useUserCount } from '../hooks'

/* ===== MOCK: BE chưa có API permissions. Thay initialMatrix + onSave bằng API thật. ===== */
const empty = (): RoleMatrix =>
    Object.fromEntries(
        MODULES.map((m) => [m.code, { view: false, edit: false, approve: false, delete: false }]),
    ) as RoleMatrix

const initialMatrix = (): Record<RoleCode, RoleMatrix> => {
    const all = Object.fromEntries(ROLE_CODES.map((r) => [r, empty()])) as Record<RoleCode, RoleMatrix>
    all.BBNK.W2 = { view: true, edit: true, approve: false, delete: true }
    all.BBNK.W4 = { view: true, edit: true, approve: true, delete: false }
    return all
}

export default function PermissionMatrix() {
    const { message } = App.useApp()
    const [saved, setSaved] = useState(initialMatrix)
    const [draft, setDraft] = useState(initialMatrix)
    const [role, setRole] = useState<RoleCode>('BBNK')
    const [filter, setFilter] = useState('')
    const count = useUserCount({ roleCode: role, isActive: true })

    const dirty = useMemo(() => JSON.stringify(saved) !== JSON.stringify(draft), [saved, draft])

    const rows = MODULES.filter((m) =>
        `${m.code} ${m.name} ${m.desc}`.toLowerCase().includes(filter.trim().toLowerCase()),
    )

    const toggle = (m: ModuleCode, a: ActionKey, v: boolean) =>
        setDraft((d) => ({ ...d, [role]: { ...d[role], [m]: { ...d[role][m], [a]: v } } }))

    const save = () => {
        setSaved(draft) // TODO: gọi API lưu role_permissions
        message.success('Đã lưu phân quyền')
    }

    return (
        <>
            <div className="um-rolebar">
                <div className="um-rolebar__chips">
                    {ROLE_CODES.map((r) => (
                        <button
                            key={r} type="button"
                            className={`um-chip ${r === role ? 'is-active' : ''}`}
                            onClick={() => setRole(r)}
                        >{r}</button>
                    ))}
                </div>
                <Tooltip title="Chưa có API tạo vai trò">
                    <Button className="um-dashed" icon={<PlusOutlined />} disabled>Thêm vai trò mới</Button>
                </Tooltip>
            </div>

            <section className="um-panel">
                <header className="um-panel__head um-panel__head--col">
                    <div>
                        <div className="um-eyebrow">RBAC theo phân hệ</div>
                        <h3 className="um-panel__title-lg">Chi tiết quyền hạn của vai trò: {ROLE_META[role].label}</h3>
                        <span className="um-pill">{count.data ?? '–'} tài khoản đang áp dụng</span>
                    </div>
                    <Input
                        allowClear prefix={<SearchOutlined />} placeholder="Lọc phân hệ..."
                        value={filter} onChange={(e) => setFilter(e.target.value)} style={{ maxWidth: 360 }}
                    />
                </header>

                <div className="um-matrix-wrap">
                    <table className="um-matrix">
                        <thead>
                        <tr>
                            <th>PHÂN HỆ NGHIỆP VỤ</th>
                            <th>MÔ TẢ PHẠM VI</th>
                            {ACTIONS.map((a) => <th key={a.key} className="um-matrix__c">{a.label.toUpperCase()}</th>)}
                        </tr>
                        </thead>
                        <tbody>
                        {rows.map((m) => (
                            <tr key={m.code}>
                                <td><strong>{m.code} – {m.name}</strong></td>
                                <td className="um-muted">{m.desc}</td>
                                {ACTIONS.map((a) => (
                                    <td key={a.key} className="um-matrix__c">
                                        <Checkbox
                                            checked={draft[role][m.code][a.key]}
                                            onChange={(e) => toggle(m.code, a.key, e.target.checked)}
                                            aria-label={`${m.name} – ${a.label}`}
                                        />
                                    </td>
                                ))}
                            </tr>
                        ))}
                        {rows.length === 0 && (
                            <tr><td colSpan={6} className="um-muted um-matrix__empty">Không có phân hệ phù hợp</td></tr>
                        )}
                        </tbody>
                    </table>
                </div>

                <footer className="um-legend">
                    <span><i className="um-dot um-dot--on" /> Cho phép</span>
                    <span><i className="um-dot" /> Từ chối truy cập</span>
                </footer>
            </section>

            <div className="um-savebar">
                <span>{dirty ? 'Có thay đổi chưa lưu' : 'Không có thay đổi chưa lưu'}</span>
                <Button type="primary" size="large" disabled={!dirty} onClick={save}>Lưu thay đổi</Button>
            </div>
        </>
    )
}
