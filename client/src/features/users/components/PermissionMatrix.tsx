import { useMemo, useState } from 'react'
import { Alert, App, Button, Checkbox, Input, Tooltip } from 'antd'
import { PlusOutlined, SearchOutlined } from '@ant-design/icons'

import { ACTIONS, ROLE_META } from '@/config/rbac'
import { MODULE_LABEL_OVERRIDES, resolvePreset } from '@/config/permissionDefaults'
import type { RoleCode } from '@/types/user'
import { useRoles } from '../hooks'
import {
    useRolePermissions,
    useUpdateRolePermissions,
} from '../permissionsHooks'
import type { PermissionDto } from '../permissionsApi'
import CreateRoleModal from './CreateRoleModal'
import CreatePermissionModal from './CreatePermissionModal'
import { parseApiError } from '@/services/apiError'

/** Role luôn có toàn quyền, BE cũng chặn sửa (RolesService.UpdateRolePermissionsAsync). */
const LOCKED_ROLE = 'SYS'

interface ModuleRow {
    code: string
    name: string
    description: string
    /** key = actionType viết hoa (VIEW, EDIT, APPROVE, CANCEL...) */
    byAction: Map<string, PermissionDto>
}

interface ActionColumn {
    key: string
    label: string
}

/** Gom danh sách permission phẳng từ API thành các dòng theo module. */
const groupByModule = (permissions: PermissionDto[]): ModuleRow[] => {
    const map = new Map<string, ModuleRow>()

    for (const p of permissions) {
        let row = map.get(p.moduleCode)
        if (!row) {
            const override = MODULE_LABEL_OVERRIDES[p.moduleCode]
            row = {
                code: p.moduleCode,
                name: override?.name ?? p.moduleName,
                description: override?.description ?? p.description ?? '',
                byAction: new Map(),
            }
            map.set(p.moduleCode, row)
        }
        row.byAction.set(p.actionType.toUpperCase(), p)
    }

    return [...map.values()]
}

/** Cột = các action thực sự tồn tại trong DB, theo thứ tự chuẩn của ACTIONS. */
const buildColumns = (permissions: PermissionDto[]): ActionColumn[] => {
    const present = new Set(permissions.map((p) => p.actionType.toUpperCase()))
    const knownKeys = new Set<string>(ACTIONS.map((a) => a.key))

    const known: ActionColumn[] = ACTIONS.filter((a) => present.has(a.key)).map(
        (a) => ({ key: a.key, label: a.label }),
    )
    const extra: ActionColumn[] = [...present]
        .filter((key) => !knownKeys.has(key))
        .map((key) => ({ key, label: key }))

    return [...known, ...extra]
}

const sameSet = (a: Set<number>, b: Set<number>) =>
    a.size === b.size && [...a].every((id) => b.has(id))

export default function PermissionMatrix() {
    const { message, modal } = App.useApp()

    const [roleId, setRoleId] = useState<number>()
    // Bản nháp gắn với 1 role cụ thể; đổi role thì bản nháp cũ tự bị bỏ qua.
    const [draft, setDraft] = useState<{ roleId: number; ids: Set<number> } | null>(null)
    const [filter, setFilter] = useState('')
    const [creatingRole, setCreatingRole] = useState(false)
    const [creatingPermission, setCreatingPermission] = useState(false)

    const rolesQuery = useRoles()
    const roles = rolesQuery.data
    const selectedRole = roles?.find((r) => r.id === roleId) ?? roles?.[0]

    const permissionsQuery = useRolePermissions(selectedRole?.id)
    const updatePermissions = useUpdateRolePermissions()

    const data = permissionsQuery.data
    const permissions = useMemo(() => data?.permissions ?? [], [data])
    const modules = useMemo(() => groupByModule(permissions), [permissions])
    const columns = useMemo(() => buildColumns(permissions), [permissions])

    const locked = selectedRole?.code === LOCKED_ROLE

    const savedIds = useMemo(
        () => new Set(data?.assignedPermissionIds ?? []),
        [data],
    )
    const allIds = useMemo(
        () => new Set(permissions.map((p) => p.permissionId)),
        [permissions],
    )

    const activeDraft =
        draft && draft.roleId === selectedRole?.id ? draft.ids : null
    const currentIds = locked ? allIds : (activeDraft ?? savedIds)
    const dirty =
        !locked && activeDraft !== null && !sameSet(activeDraft, savedIds)

    const readOnly =
        locked ||
        !selectedRole ||
        permissionsQuery.isLoading ||
        permissionsQuery.isError ||
        updatePermissions.isPending

    const keyword = filter.trim().toLowerCase()
    const rows = modules.filter((m) =>
        `${m.code} ${m.name} ${m.description}`.toLowerCase().includes(keyword),
    )

    const mutateDraft = (apply: (next: Set<number>) => void) => {
        if (!selectedRole) return
        const next = new Set(currentIds)
        apply(next)
        setDraft({ roleId: selectedRole.id, ids: next })
    }

    const togglePermission = (permissionId: number, checked: boolean) =>
        mutateDraft((next) => {
            if (checked) next.add(permissionId)
            else next.delete(permissionId)
        })

    const toggleModule = (row: ModuleRow, checked: boolean) =>
        mutateDraft((next) => {
            for (const p of row.byAction.values()) {
                if (checked) next.add(p.permissionId)
                else next.delete(p.permissionId)
            }
        })

    const selectRole = (id: number) => {
        if (id === selectedRole?.id) return

        if (!dirty) {
            setRoleId(id)
            return
        }

        modal.confirm({
            title: 'Bỏ các thay đổi chưa lưu?',
            content: 'Các thay đổi phân quyền của vai trò hiện tại sẽ bị mất.',
            okText: 'Bỏ thay đổi',
            cancelText: 'Ở lại',
            onOk: () => {
                setDraft(null)
                setRoleId(id)
            },
        })
    }

    const preset = selectedRole
        ? resolvePreset(selectedRole.code, permissions)
        : null

    const applyPreset = () => {
        if (!selectedRole || !preset) return
        setDraft({ roleId: selectedRole.id, ids: preset })
        message.info('Đã áp dụng mẫu mặc định. Kiểm tra lại rồi bấm "Lưu thay đổi".')
    }

    const save = async () => {
        if (!selectedRole || !dirty) return

        try {
            await updatePermissions.mutateAsync({
                roleId: selectedRole.id,
                permissionIds: [...currentIds].sort((a, b) => a - b),
            })
            setDraft(null)
            message.success(`Đã lưu phân quyền cho vai trò ${selectedRole.code}`)
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    const roleLabel = selectedRole
        ? (ROLE_META[selectedRole.code as RoleCode]?.label ?? selectedRole.name)
        : ''

    return (
        <>
            <div className="um-rolebar">
                {rolesQuery.isLoading && <p>Đang tải vai trò...</p>}
                {rolesQuery.isError && <p>Không thể tải danh sách vai trò.</p>}

                <div className="um-rolebar__chips">
                    {roles?.map((r) => (
                        <button
                            key={r.id}
                            type="button"
                            className={`um-chip ${r.id === selectedRole?.id ? 'is-active' : ''}`}
                            onClick={() => selectRole(r.id)}
                        >
                            {r.code}
                        </button>
                    ))}
                </div>

                <Button
                    className="um-dashed"
                    icon={<PlusOutlined />}
                    onClick={() => setCreatingRole(true)}
                >
                    Thêm vai trò mới
                </Button>

            </div>

            <section className="um-panel">
                <header className="um-panel__head um-panel__head--col">
                    <div>
                        <div className="um-eyebrow">RBAC theo phân hệ</div>
                        <h3 className="um-panel__title-lg">
                            Chi tiết quyền hạn của vai trò: {roleLabel}
                        </h3>
                        <span className="um-pill">
                            {data?.usersCount ?? '–'} tài khoản được gán vai trò này
                        </span>
                    </div>

                    <Input
                        allowClear
                        prefix={<SearchOutlined />}
                        placeholder="Lọc phân hệ..."
                        value={filter}
                        onChange={(e) => setFilter(e.target.value)}
                        style={{ maxWidth: 360 }}
                    />
                </header>

                {locked && (
                    <Alert
                        type="info"
                        showIcon
                        style={{ margin: '0 0 12px' }}
                        message="Vai trò SYS luôn có toàn bộ quyền và không thể chỉnh sửa."
                    />
                )}

                {permissionsQuery.isLoading && <p>Đang tải phân quyền...</p>}
                {permissionsQuery.isError && (
                    <p>Không thể tải phân quyền. Vui lòng thử lại.</p>
                )}

                <div className="um-matrix-wrap">
                    <table className="um-matrix">
                        <thead>
                        <tr>
                            <th>PHÂN HỆ NGHIỆP VỤ</th>
                            <th>MÔ TẢ PHẠM VI</th>
                            {columns.map((c) => (
                                <th key={c.key} className="um-matrix__c">
                                    {c.label.toUpperCase()}
                                </th>
                            ))}
                            <th className="um-matrix__c">TẤT CẢ</th>
                        </tr>
                        </thead>

                        <tbody>
                        {rows.map((row) => {
                            const rowPerms = [...row.byAction.values()]
                            const checkedCount = rowPerms.filter((p) =>
                                currentIds.has(p.permissionId),
                            ).length

                            return (
                                <tr key={row.code}>
                                    <td>
                                        <strong>
                                            {row.code} – {row.name}
                                        </strong>
                                    </td>
                                    <td className="um-muted">{row.description}</td>

                                    {columns.map((c) => {
                                        const p = row.byAction.get(c.key)

                                        return (
                                            <td key={c.key} className="um-matrix__c">
                                                {p ? (
                                                    <Tooltip title={p.permissionName}>
                                                        <Checkbox
                                                            checked={currentIds.has(p.permissionId)}
                                                            disabled={readOnly}
                                                            onChange={(e) =>
                                                                togglePermission(
                                                                    p.permissionId,
                                                                    e.target.checked,
                                                                )
                                                            }
                                                            aria-label={`${row.name} – ${c.label}`}
                                                        />
                                                    </Tooltip>
                                                ) : (
                                                    <span className="um-muted">–</span>
                                                )}
                                            </td>
                                        )
                                    })}

                                    <td className="um-matrix__c">
                                        <Checkbox
                                            checked={
                                                rowPerms.length > 0 &&
                                                checkedCount === rowPerms.length
                                            }
                                            indeterminate={
                                                checkedCount > 0 &&
                                                checkedCount < rowPerms.length
                                            }
                                            disabled={readOnly}
                                            onChange={(e) =>
                                                toggleModule(row, e.target.checked)
                                            }
                                            aria-label={`${row.name} – tất cả`}
                                        />
                                    </td>
                                </tr>
                            )
                        })}

                        {rows.length === 0 && !permissionsQuery.isLoading && (
                            <tr>
                                <td
                                    colSpan={3 + columns.length}
                                    className="um-muted um-matrix__empty"
                                >
                                    Không có phân hệ phù hợp
                                </td>
                            </tr>
                        )}
                        </tbody>
                    </table>
                </div>

                <div style={{ padding: '12px 0' }}>
                    <Button
                        className="um-dashed"
                        icon={<PlusOutlined />}
                        disabled={!data}
                        onClick={() => setCreatingPermission(true)}
                    >
                        Thêm quyền mới
                    </Button>
                </div>

                <footer className="um-legend">
                    <span>
                        <i className="um-dot um-dot--on" /> Cho phép
                    </span>
                    <span>
                        <i className="um-dot" /> Từ chối truy cập
                    </span>
                    <span>– Phân hệ không có thao tác này</span>
                </footer>
            </section>

            <div className="um-savebar">
                <span>
                    {dirty ? 'Có thay đổi chưa lưu' : 'Không có thay đổi chưa lưu'}
                </span>

                <Button disabled={readOnly || !preset} onClick={applyPreset}>
                    Áp dụng mẫu mặc định
                </Button>

                <Button disabled={!dirty || updatePermissions.isPending} onClick={() => setDraft(null)}>
                    Hoàn tác
                </Button>

                <Button
                    type="primary"
                    size="large"
                    disabled={!dirty || readOnly}
                    loading={updatePermissions.isPending}
                    onClick={save}
                >
                    Lưu thay đổi
                </Button>
            </div>

            <CreateRoleModal
                open={creatingRole}
                onClose={() => setCreatingRole(false)}
                onCreated={(r) => {
                    // Không tự chuyển role nếu đang có thay đổi chưa lưu.
                    if (!dirty) setRoleId(r.roleId)
                }}
            />

            <CreatePermissionModal
                open={creatingPermission}
                onClose={() => setCreatingPermission(false)}
                permissions={permissions}
                roleLabel={locked ? undefined : (selectedRole?.code ?? undefined)}
                onCreated={(p) => mutateDraft((next) => { next.add(p.permissionId) })}
            />
        </>
    )
}