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

            <Layout className="app-layout__body">
                <Sidebar />

                <Content className="app-layout__content">
                    {/* delay 150ms: chuyển trang nhanh thì không nháy spinner */}
                    <Spin spinning={navigation.state === 'loading'} delay={150}>
                        <Outlet />
                    </Spin>
                </Content>
            </Layout>
        </Layout>
    )
}