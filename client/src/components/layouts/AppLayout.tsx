import { Layout } from 'antd'
import { Outlet } from 'react-router-dom'
import Header from './Header'
import Sidebar from './Sidebar'

export default function AppLayout() {
    return (
        <Layout style={{ minHeight: '100vh' }}>
            <Sidebar />
            <Layout>
                <Header />
                <Layout.Content style={{ padding: 24 }}>
                    <Outlet />
                </Layout.Content>
            </Layout>
        </Layout>
    )
}