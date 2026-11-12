import { useState } from 'react'
import { Breadcrumb, Tabs } from 'antd'
import { useInventorySummary } from '../stockHooks'
import { fmtNum } from '../stockFormat'
import BagInventoryPanel from '../components/BagInventoryPanel'
import StorageLocationsPanel from '../components/StorageLocationsPanel'
import StockCountPanel from '../components/StockCountPanel'
import './InventoryPages.css'
import './Disposal.css'
import './Stock.css'

export default function StockPage() {
    const [tab, setTab] = useState('bags')
    const { data } = useInventorySummary()
    const o = data?.overview

    const cards = [
        { label: 'Tổng túi máu', value: o?.totalBags, sub: 'Trong kho' },
        { label: 'Có sẵn', value: o?.availableBags, sub: 'Sẵn sàng cấp phát' },
        // TODO(BE): overview chưa có số túi "Đặt chỗ" (sprint sau) -> tạm 0
        { label: 'Đặt chỗ', value: 0, sub: 'Chờ bàn giao' },
        { label: 'Kiểm dịch', value: o?.testingOrProcessingBags, sub: 'Đang xét nghiệm / điều chế' },
        { label: 'Cận hạn', value: o?.expiringSoonBags, sub: '≤ 3 ngày' },
    ]

    return (
        <div className="inv">
            <Breadcrumb items={[{ title: 'Túi máu & Kho' }, { title: 'Tồn kho & Kiểm kê' }]} />
            <div className="inv-title">
                <h1>Tồn kho &amp; Kiểm kê</h1>
                <p>Tra cứu trạng thái túi máu, lọc theo nhóm máu, quản lý tủ lưu trữ và thực hiện đối soát kho.</p>
            </div>

            <div className="stk-cards">
                {cards.map((c) => (
                    <div className="inv-card stk-card" key={c.label}>
                        <small>{c.label}</small>
                        <b>{c.value === undefined ? '—' : fmtNum(c.value)}</b>
                        <span>{c.sub}</span>
                    </div>
                ))}
            </div>

            <Tabs
                activeKey={tab}
                onChange={setTab}
                destroyOnHidden
                items={[
                    { key: 'bags', label: 'Tồn kho túi máu', children: <BagInventoryPanel /> },
                    { key: 'locations', label: 'Quản lý tủ lưu trữ', children: <StorageLocationsPanel /> },
                    { key: 'count', label: 'Kiểm kê tồn kho', children: <StockCountPanel /> },
                ]}
            />
        </div>
    )
}