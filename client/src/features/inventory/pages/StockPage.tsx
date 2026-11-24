import { Tabs } from 'antd'
import { useSearchParams } from 'react-router-dom'
import { PageContainer, PageHeader } from '@/shared/components'
import { useInventorySummary } from '../stockHooks'
import { fmtNum } from '../stockFormat'
import BagInventoryPanel from '../components/BagInventoryPanel'
import ComponentTypesConfigPanel from '../components/ComponentTypesConfigPanel'
import StorageLocationsPanel from '../components/StorageLocationsPanel'
import StockCountPanel from '../components/StockCountPanel'
import './InventoryPages.css'
import './Disposal.css'
import './Stock.css'

type StockTab = 'bags' | 'locations' | 'count' | 'component-types'

const VALID_TABS: StockTab[] = ['bags', 'locations', 'count', 'component-types']

export default function StockPage() {
    const [searchParams, setSearchParams] = useSearchParams()
    const rawTab = searchParams.get('tab') as StockTab | null
    const tab: StockTab = rawTab && VALID_TABS.includes(rawTab) ? rawTab : 'bags'
    const { data } = useInventorySummary()
    const o = data?.overview

    const handleTabChange = (next: string) => {
        const key: StockTab = VALID_TABS.includes(next as StockTab) ? (next as StockTab) : 'bags'
        if (key === 'bags') {
            setSearchParams({}, { replace: true })
        } else {
            setSearchParams({ tab: key }, { replace: true })
        }
    }

    const cards = [
        { label: 'Tổng túi máu', value: o?.totalBags, sub: 'Trong kho' },
        { label: 'Có sẵn', value: o?.availableBags, sub: 'Sẵn sàng cấp phát' },
        // TODO(BE): overview chưa có số túi "Đặt chỗ" (sprint sau) -> tạm 0
        { label: 'Đặt chỗ', value: 0, sub: 'Chờ bàn giao' },
        { label: 'Kiểm dịch', value: o?.testingOrProcessingBags, sub: 'Đang xét nghiệm / điều chế' },
        { label: 'Cận hạn', value: o?.expiringSoonBags, sub: '≤ 3 ngày' },
    ]

    return (
        <PageContainer>
            <PageHeader
                breadcrumb={[{ title: 'Túi máu & Kho' }, { title: 'Tồn kho & Kiểm kê' }]}
                title="Tồn kho & Kiểm kê"
                subtitle="Tra cứu trạng thái túi máu, quản lý tủ lưu trữ theo phân khu, đối soát kho và cấu hình quy chuẩn chế phẩm máu."
            />

            <div className="stk-cards">
                {cards.map((c) => (
                    <div className="stk-card" key={c.label}>
                        <small>{c.label}</small>
                        <b>{c.value === undefined ? '—' : fmtNum(c.value)}</b>
                        <span>{c.sub}</span>
                    </div>
                ))}
            </div>

            <Tabs
                activeKey={tab}
                onChange={handleTabChange}
                destroyOnHidden
                items={[
                    { key: 'bags', label: 'Tồn kho túi máu', children: <BagInventoryPanel /> },
                    { key: 'locations', label: 'Quản lý tủ lưu trữ', children: <StorageLocationsPanel /> },
                    { key: 'count', label: 'Kiểm kê tồn kho', children: <StockCountPanel /> },
                    { key: 'component-types', label: 'Cấu hình chế phẩm máu', children: <ComponentTypesConfigPanel /> },
                ]}
            />
        </PageContainer>
    )
}