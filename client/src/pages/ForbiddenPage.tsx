import { Button, Result } from 'antd'
import { useNavigate } from 'react-router-dom'
import { PageContainer, SectionCard } from '@/shared/components'

export default function ForbiddenPage() {
    const navigate = useNavigate()
    return (
        <PageContainer maxWidth="md">
            <SectionCard>
                <Result
                    status="403"
                    title="403"
                    subTitle="Bạn không có quyền truy cập trang này."
                    extra={<Button type="primary" onClick={() => navigate('/')}>Về trang chủ</Button>}
                />
            </SectionCard>
        </PageContainer>
    )
}