import { Layout, Spin } from 'antd'
import { Outlet, useNavigation } from 'react-router-dom'
import Header from './Header'
import Sidebar from './Sidebar'

const { Content } = Layout

export default function AppLayout() {
    const navigation = useNavigation()

    return (
        <Layout className="app-layout" style={{ minHeight: '100vh' }}>
            <Header />

            <Layout className="app-layout__body" style={{ position: 'relative' }}>
                <Sidebar />

                <Content
                    className="app-layout__content"
                    style={{
                        marginLeft: '10px',
                        padding: '24px',
                        minHeight: 280,
                        background: '#f5f5f5'
                    }}
                >
                    {/* delay 150ms: chuyển trang nhanh thì không nháy spinner */}
                    <Spin spinning={navigation.state === 'loading'} delay={150}>
                        <Outlet />
                    </Spin>
                </Content>
            </Layout>
        </Layout>
    )
}