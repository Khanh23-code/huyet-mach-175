import { Layout } from 'antd'
import { Outlet } from 'react-router-dom'
import Header from './Header'
import Sidebar from './Sidebar'

const { Content } = Layout

export default function AppLayout() {
    return (
        <Layout className="app-layout" style={{ minHeight: '100vh' }}>
            <Header />

            <Layout className="app-layout__body">
                <Sidebar />

                <Content className="app-layout__content">
                    <Outlet />
                </Content>
            </Layout>
        </Layout>
    )
}