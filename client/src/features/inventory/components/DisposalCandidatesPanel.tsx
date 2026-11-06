import { useEffect, useRef, useState } from 'react'
import { Alert, Button, Input, Popconfirm, Select, Table, Tag, message } from 'antd'
import { BarcodeOutlined } from '@ant-design/icons'
import { parseApiError } from '@/services/apiError'
import { fmtDateTime } from '@/features/screening/format'
import { useCreateDisposal, useDisposalCandidates } from '../disposalsHooks'
import { REASON_OPTIONS } from '../disposalsFormat'
import type { DisposalCandidate, DisposalReason } from '../disposalsTypes'

interface Props {
    presetBarcode: string
    canEdit: boolean
    onCreated: () => void
}

const PAGE_SIZE = 20

export default function DisposalCandidatesPanel({ presetBarcode, canEdit, onCreated }: Props) {
    const [keyword, setKeyword] = useState(presetBarcode)
    const [search, setSearch] = useState(presetBarcode)
    const [page, setPage] = useState(1)
    const [selected, setSelected] = useState<Record<string, DisposalCandidate>>({})
    const [reason, setReason] = useState<DisposalReason | null>(presetBarcode ? 'INFECTIOUS_POSITIVE' : null)
    const [notes, setNotes] = useState('')
    const presetDone = useRef(false)

    const create = useCreateDisposal()
    const { data, isLoading, isFetching } = useDisposalCandidates({
        search: search || undefined,
        pageNumber: page,
        pageSize: PAGE_SIZE,
    })

    useEffect(() => {
        const t = setTimeout(() => {
            setSearch(keyword.trim())
            setPage(1)
        }, 400)
        return () => clearTimeout(t)
    }, [keyword])

    // Tự chọn sẵn túi được chuyển từ màn xét nghiệm
    useEffect(() => {
        if (!presetBarcode || presetDone.current || !data) return
        presetDone.current = true
        const hit = data.items.find((i) => i.barcode.toLowerCase() === presetBarcode.toLowerCase())
        if (hit) {
            setSelected({ [hit.barcode]: hit })
            setNotes(hit.discardReason ?? '')
        } else {
            message.warning(
                `Túi '${presetBarcode}' không nằm trong danh sách chờ hủy (có thể đã nằm trong biên bản khác).`,
            )
        }
    }, [data, presetBarcode])

    const chosen = Object.values(selected)
    const canSubmit = canEdit && chosen.length > 0 && reason !== null

    const submit = async () => {
        if (!canSubmit || reason === null) return
        try {
            const r = await create.mutateAsync({
                barcodes: chosen.map((c) => c.barcode),
                reason,
                notes: notes.trim() || undefined,
            })
            message.success(r.message)
            setSelected({})
            setNotes('')
            setReason(null)
            onCreated()
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    const toggle = (rows: DisposalCandidate[], checked: boolean) =>
        setSelected((prev) => {
            const next = { ...prev }
            rows.forEach((r) => {
                if (checked) next[r.barcode] = r
                else delete next[r.barcode]
            })
            return next
        })

    return (
        <div className="disp-layout">
            <section className="inv-card inv-card__body">
                <div className="disp-tools">
                    <Input
                        style={{ maxWidth: 320 }}
                        prefix={<BarcodeOutlined />}
                        placeholder="Tìm theo barcode"
                        value={keyword}
                        onChange={(e) => setKeyword(e.target.value)}
                        allowClear
                        autoComplete="off"
                    />
                </div>
                <Table<DisposalCandidate>
                    size="middle"
                    rowKey="barcode"
                    loading={isLoading || isFetching}
                    dataSource={data?.items ?? []}
                    scroll={{ x: 720 }}
                    locale={{ emptyText: 'Không có túi máu nào đang chờ lập biên bản hủy' }}
                    rowSelection={{
                        selectedRowKeys: Object.keys(selected),
                        getCheckboxProps: () => ({ disabled: !canEdit }),
                        onSelect: (record, checked) => toggle([record], checked),
                        onSelectAll: (checked, _rows, changed) => toggle(changed, checked),
                    }}
                    pagination={{
                        current: page,
                        pageSize: PAGE_SIZE,
                        total: data?.totalCount ?? 0,
                        showSizeChanger: false,
                        onChange: setPage,
                    }}
                    columns={[
                        { title: 'Barcode', dataIndex: 'barcode', render: (v: string) => <span className="inv-mono">{v}</span> },
                        { title: 'Nhóm máu', dataIndex: 'bloodGroup', width: 90, render: (v: string) => <Tag color="red">{v}</Tag> },
                        { title: 'Chế phẩm', dataIndex: 'componentTypeCode', width: 90 },
                        { title: 'Thể tích', dataIndex: 'volumeMl', width: 90, render: (v: number) => `${v} ml` },
                        { title: 'Lý do khóa', dataIndex: 'discardReason', render: (v?: string | null) => v || '—' },
                        { title: 'Hạn dùng', dataIndex: 'expiredAt', width: 150, render: (v: string) => fmtDateTime(v) },
                    ]}
                />
            </section>

            <aside className="inv-card disp-side">
                <h3>Lập biên bản đề xuất hủy</h3>
                {chosen.length === 0 ? (
                    <div className="disp-note">Chọn ít nhất 1 túi trong danh sách bên trái.</div>
                ) : (
                    <div className="disp-chips">
                        {chosen.map((c) => (
                            <Tag
                                key={c.barcode}
                                closable={canEdit}
                                onClose={(e) => {
                                    e.preventDefault()
                                    toggle([c], false)
                                }}
                            >
                                <span className="inv-mono">{c.barcode}</span>
                            </Tag>
                        ))}
                    </div>
                )}
                <Select
                    placeholder="Lý do tiêu hủy"
                    options={REASON_OPTIONS}
                    value={reason ?? undefined}
                    onChange={setReason}
                    disabled={!canEdit}
                />
                <Input.TextArea
                    rows={4}
                    maxLength={500}
                    showCount
                    placeholder="Ghi chú (không bắt buộc)"
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    disabled={!canEdit}
                />
                {!canEdit && <Alert type="warning" showIcon title="Bạn chỉ có quyền xem." />}
                <Popconfirm
                    title={`Lập biên bản cho ${chosen.length} túi?`}
                    description="Các túi sẽ bị khóa cho đến khi biên bản được duyệt hoặc rút."
                    okText="Lập biên bản"
                    cancelText="Hủy"
                    disabled={!canSubmit}
                    onConfirm={submit}
                >
                    <Button type="primary" disabled={!canSubmit} loading={create.isPending} block>
                        Lập biên bản ({chosen.length} túi)
                    </Button>
                </Popconfirm>
            </aside>
        </div>
    )
}