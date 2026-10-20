import { Button, Result } from 'antd'
import { isRouteErrorResponse, useNavigate, useRouteError } from 'react-router-dom'
import NotFoundPage from '@/pages/NotFoundPage'

export default function RouteErrorBoundary() {
    const error = useRouteError()
    const navigate = useNavigate()

    if (isRouteErrorResponse(error) && error.status === 404) {
        return <NotFoundPage />
    }

    const errorMessage =
        error instanceof Error
            ? error.message
            : isRouteErrorResponse(error)
              ? `${error.status} ${error.statusText}`
              : 'Đã xảy ra lỗi không mong muốn'

    return (
        <div style={{ padding: 48, display: 'flex', justifyContent: 'center' }}>
            <Result
                status="500"
                title="Lỗi ứng dụng"
                subTitle="Hệ thống gặp sự cố trong quá trình xử lý yêu cầu."
                extra={[
                    <Button key="back" onClick={() => navigate(-1)}>
                        Quay lại
                    </Button>,
                    <Button key="home" type="primary" onClick={() => navigate('/')}>
                        Về trang chủ
                    </Button>,
                ]}
            >
                <div style={{ maxWidth: 600, margin: '0 auto', textAlign: 'left' }}>
                    <p>
                        <strong>Chi tiết lỗi:</strong>
                    </p>
                    <pre
                        style={{
                            background: '#f5f5f5',
                            padding: 12,
                            borderRadius: 8,
                            overflowX: 'auto',
                            fontSize: 12,
                        }}
                    >
                        {errorMessage}
                    </pre>
                </div>
            </Result>
        </div>
    )
}

