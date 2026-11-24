import { PageContainer, PageHeader } from '@/shared/components'
import { ClinicalReturnCreatePanel } from '../components/ClinicalReturnCreatePanel'
import './ClinicalBloodRequestsPage.css'

export default function ClinicalReturnCreatePage() {
    return (
        <PageContainer>
            <PageHeader
                breadcrumb={[
                    { title: 'Yêu cầu máu Lâm sàng', path: '/clinical/requests' },
                    { title: 'Lập phiếu hoàn trả' },
                ]}
                title="Lập phiếu hoàn trả túi máu"
                subtitle="Chọn túi máu đủ điều kiện hoàn trả tại khoa, xác nhận 4 điều kiện bảo quản và gửi phiếu về Ngân hàng máu."
            />

            <ClinicalReturnCreatePanel />
        </PageContainer>
    )
}
