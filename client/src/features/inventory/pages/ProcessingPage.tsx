import { useEffect, useMemo, useState } from 'react'
import { Breadcrumb, Empty, Input, Skeleton, message } from 'antd'
import { BarcodeOutlined } from '@ant-design/icons'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { useProcessingQueue } from '../hooks'
import { inventoryApi } from '../inventoryApi'
import { explainNotSeparable } from '../format'
import SeparationWorkspace from '../components/SeparationWorkspace'
import BagSourceFilter from '../components/BagSourceFilter'
import BagQueueItem from '../components/BagQueueItem'
import type { ProcessingQueueItem } from '../types'
import type { SourceFilterValue } from '../bagSource'
import './InventoryPages.css'

const REFRESH_MS = 30_000

export default function ProcessingPage() {
    const { can } = usePermission()
    const [keyword, setKeyword] = useState('')
    const [search, setSearch] = useState('')
    const [sourceType, setSourceType] = useState<SourceFilterValue>('')
    const [selected, setSelected] = useState<ProcessingQueueItem | null>(null)
    const [looking, setLooking] = useState(false)

    useEffect(() => {
        const t = setTimeout(() => setSearch(keyword.trim()), 400)
        return () => clearTimeout(t)
    }, [keyword])

    const { data, isLoading } = useProcessingQueue(
        { search: search || undefined, sourceType: sourceType || undefined, pageNumber: 1, pageSize: 50 },
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
                    <BagSourceFilter value={sourceType} onChange={setSourceType} />
                    <div className="inv-queue__list">
                        {isLoading ? (
                            <Skeleton active paragraph={{ rows: 6 }} />
                        ) : items.length === 0 ? (
                            <Empty description="Không có túi nào chờ điều chế" />
                        ) : (
                            items.map((i) => (
                                <BagQueueItem
                                    key={i.bagId}
                                    barcode={i.barcode}
                                    bloodGroup={i.bloodGroup}
                                    volumeMl={i.volumeMl}
                                    collectedAt={i.collectedAt}
                                    expiredAt={i.expiredAt}
                                    isExpired={i.isExpired}
                                    source={i.source}
                                    active={selected?.bagId === i.bagId}
                                    onClick={() => setSelected(i)}
                                />
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