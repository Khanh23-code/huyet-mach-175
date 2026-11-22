import { useEffect, useMemo, useState } from 'react'
import { Empty, Input, Skeleton, message } from 'antd'
import { BarcodeOutlined } from '@ant-design/icons'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { PageContainer, PageHeader, SectionCard } from '@/shared/components'
import { useLabQueue } from '../labTestsHooks'
import { labTestsApi } from '../labTestsApi'
import LabTestWorkspace from '../components/LabTestWorkspace'
import BagSourceFilter from '../components/BagSourceFilter'
import BagQueueItem from '../components/BagQueueItem'
import type { LabQueueItem } from '../labTestsTypes'
import type { SourceFilterValue } from '../bagSource'
import './InventoryPages.css'
import './LabTest.css'

const REFRESH_MS = 30_000

export default function LabTestPage() {
    const { can } = usePermission()
    const [keyword, setKeyword] = useState('')
    const [search, setSearch] = useState('')
    const [sourceType, setSourceType] = useState<SourceFilterValue>('')
    const [selected, setSelected] = useState<LabQueueItem | null>(null)
    const [looking, setLooking] = useState(false)

    useEffect(() => {
        const t = setTimeout(() => setSearch(keyword.trim()), 400)
        return () => clearTimeout(t)
    }, [keyword])

    const { data, isLoading } = useLabQueue(
        { search: search || undefined, sourceType: sourceType || undefined, pageNumber: 1, pageSize: 50 },
        { refetchInterval: REFRESH_MS },
    )
    const items = useMemo(() => data?.items ?? [], [data])

    const lookup = async () => {
        const code = keyword.trim()
        if (!code || looking) return
        setLooking(true)
        try {
            const res = await labTestsApi.queue({ search: code, pageNumber: 1, pageSize: 10 })
            const hit = res.items.find((i) => i.barcode.toLowerCase() === code.toLowerCase())
            if (hit) {
                setSelected(hit)
                setKeyword('')
            } else {
                message.warning(
                    `Túi '${code}' không có trong hàng chờ xét nghiệm. Túi cần ở trạng thái READY_TESTING và chưa có kết quả.`,
                )
            }
        } catch (e) {
            message.error(parseApiError(e).message)
        } finally {
            setLooking(false)
        }
    }

    return (
        <PageContainer>
            <PageHeader
                breadcrumb={[{ title: 'Túi máu & Kho' }, { title: 'Xét nghiệm túi máu' }]}
                title="Nhập kết quả xét nghiệm"
                subtitle="Ghi nhận kết quả sàng lọc 5 chỉ số và nhóm máu khẳng định cho túi máu đã tiếp nhận."
            />

            <div className="inv-proc">
                <SectionCard bodyClassName="inv-queue">
                    <Input
                        size="large"
                        prefix={<BarcodeOutlined />}
                        placeholder="Quét / nhập barcode rồi Enter"
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
                            <Empty description="Không có túi nào chờ xét nghiệm" />
                        ) : (
                            items.map((i) => (
                                <BagQueueItem
                                    key={i.bagId}
                                    barcode={i.barcode}
                                    bloodGroup={i.expectedBloodGroup}
                                    volumeMl={i.volumeMl}
                                    componentTypeCode={i.componentTypeCode}
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
                </SectionCard>

                <div>
                    {selected ? (
                        <LabTestWorkspace
                            key={selected.bagId}
                            item={selected}
                            canEdit={can(PERMISSIONS.LAB_EDIT)}
                            canDispose={can(PERMISSIONS.DISPOSAL_EDIT)}
                            onDone={() => setSelected(null)}
                        />
                    ) : (
                        <SectionCard>
                            <Empty description="Chọn hoặc quét một túi máu để nhập kết quả xét nghiệm" />
                        </SectionCard>
                    )}
                </div>
            </div>
        </PageContainer>
    )
}