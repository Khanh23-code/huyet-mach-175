import { useEffect, useMemo, useState } from 'react'
import { Breadcrumb, Empty, Input, Skeleton, Tag, message } from 'antd'
import { BarcodeOutlined } from '@ant-design/icons'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { fmtDateTime } from '@/features/screening/format'
import { useProcessingQueue } from '../hooks'
import { inventoryApi } from '../inventoryApi'
import { explainNotSeparable } from '../format'
import SeparationWorkspace from '../components/SeparationWorkspace'
import type { ProcessingQueueItem } from '../types'
import './InventoryPages.css'

const REFRESH_MS = 30_000

export default function ProcessingPage() {
    const { can } = usePermission()
    const [keyword, setKeyword] = useState('')
    const [search, setSearch] = useState('')
    const [selected, setSelected] = useState<ProcessingQueueItem | null>(null)
    const [looking, setLooking] = useState(false)

    useEffect(() => {
        const t = setTimeout(() => setSearch(keyword.trim()), 400)
        return () => clearTimeout(t)
    }, [keyword])

    const { data, isLoading } = useProcessingQueue(
        { search: search || undefined, pageNumber: 1, pageSize: 50 },
        { refetchInterval: REFRESH_MS },
    )
    const items = useMemo(() => data?.items ?? [], [data])

    const lookup = async () => {
        const code = keyword.trim()
        if (!code || looking) return
        setLooking(true)
        try {
            const res = await inventoryApi.processingQueue({ search: code, pageNumber: 1, pageSize: 10 })
            const hit = res.items.find((i) => i.barcode.toLowerCase() === code.toLowerCase())
            if (hit) {
                setSelected(hit)
                setKeyword('')
                return
            }
            const bags = await inventoryApi.searchBags({ search: code, pageNumber: 1, pageSize: 10 }).catch(() => null)
            const bag = bags?.items.find((b) => b.barcode.toLowerCase() === code.toLowerCase())
            message.warning(explainNotSeparable(bag, code))
        } catch (e) {
            message.error(parseApiError(e).message)
        } finally {
            setLooking(false)
        }
    }

    return (
        <div className="inv">
            <Breadcrumb items={[{ title: 'Túi máu & Kho' }, { title: 'Điều chế chế phẩm' }]} />
            <div className="inv-title">
                <h1>Điều chế chế phẩm máu</h1>
                <p>Chọn túi máu toàn phần đã xét nghiệm đạt, tách thành các chế phẩm con và in tem định danh.</p>
            </div>

            <div className="inv-proc">
                <section className="inv-card inv-queue">
                    <Input
                        size="large"
                        prefix={<BarcodeOutlined />}
                        placeholder="Quét / nhập barcode túi gốc rồi Enter"
                        value={keyword}
                        onChange={(e) => setKeyword(e.target.value)}
                        onPressEnter={lookup}
                        disabled={looking}
                        allowClear
                        autoComplete="off"
                    />
                    <div className="inv-queue__list">
                        {isLoading ? (
                            <Skeleton active paragraph={{ rows: 6 }} />
                        ) : items.length === 0 ? (
                            <Empty description="Không có túi nào chờ điều chế" />
                        ) : (
                            items.map((i) => (
                                <button
                                    type="button"
                                    key={i.bagId}
                                    className={`inv-queue__item${selected?.bagId === i.bagId ? ' is-active' : ''}`}
                                    onClick={() => setSelected(i)}
                                >
                                    <div className="inv-queue__top">
                                        <span className="inv-mono">{i.barcode}</span>
                                        <Tag color="red" style={{ marginInlineEnd: 0 }}>{i.bloodGroup}</Tag>
                                    </div>
                                    <div className="inv-queue__sub">
                                        {i.volumeMl} ml · {i.donorName || 'Tiếp nhận ngoài viện'}
                                    </div>
                                    <div className="inv-queue__sub">Thu thập {fmtDateTime(i.collectedAt)}</div>
                                </button>
                            ))
                        )}
                    </div>
                </section>

                <div>
                    {selected ? (
                        <SeparationWorkspace
                            key={selected.bagId}
                            parent={selected}
                            canEdit={can(PERMISSIONS.PROCESSING_EDIT)}
                            onDone={() => setSelected(null)}
                        />
                    ) : (
                        <section className="inv-card inv-card__body">
                            <Empty description="Chọn hoặc quét một túi máu gốc để bắt đầu điều chế" />
                        </section>
                    )}
                </div>
            </div>
        </div>
    )
}