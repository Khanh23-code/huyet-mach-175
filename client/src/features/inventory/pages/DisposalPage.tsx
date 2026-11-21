import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Tabs } from 'antd'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { PageContainer, PageHeader } from '@/shared/components'
import DisposalCandidatesPanel from '../components/DisposalCandidatesPanel'
import DisposalRecordsPanel from '../components/DisposalRecordsPanel'
import './InventoryPages.css'
import './Disposal.css'

export default function DisposalPage() {
    const { can } = usePermission()
    const canEdit = can(PERMISSIONS.DISPOSAL_EDIT)
    const [params, setParams] = useSearchParams()
    // Đọc 1 lần: barcode do màn xét nghiệm chuyển sang
    const [presetBarcode] = useState(() => params.get('barcode')?.trim() ?? '')
    const [tab, setTab] = useState('candidates')

    const clearPreset = () => {
        if (params.has('barcode')) setParams({}, { replace: true })
    }

    return (
        <PageContainer>
            <PageHeader
                breadcrumb={[{ title: 'Túi máu & Kho' }, { title: 'Xử lý hủy túi máu' }]}
                title="Xử lý hủy túi máu"
                subtitle="Lập biên bản đề xuất hủy cho túi bị khóa, theo dõi phê duyệt và xác nhận tiêu hủy vật lý."
            />

            <Tabs
                activeKey={tab}
                onChange={setTab}
                items={[
                    {
                        key: 'candidates',
                        label: 'Túi chờ hủy',
                        children: (
                            <DisposalCandidatesPanel
                                presetBarcode={presetBarcode}
                                canEdit={canEdit}
                                onCreated={() => {
                                    clearPreset()
                                    setTab('records')
                                }}
                            />
                        ),
                    },
                    { key: 'records', label: 'Biên bản', children: <DisposalRecordsPanel canEdit={canEdit} /> },
                ]}
            />
        </PageContainer>
    )
}