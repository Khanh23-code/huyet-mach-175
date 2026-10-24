import { createBrowserRouter, Navigate } from 'react-router-dom'
import LoginPage from '@/features/auth/pages/LoginPage'
import ProtectedRoute from '@/features/auth/components/ProtectedRoute'
import AppLayout from '@/components/layouts/AppLayout'
import DashboardPage from '@/pages/DashboardPage'
import ForbiddenPage from '@/pages/ForbiddenPage'
import UserManagementPage from '@/features/users/pages/UserManagementPage'
import ProfilePage from '@/features/account/pages/ProfilePage'
import SiteMonitorPage from '@/features/campaigns/pages/SiteMonitorPage'
import ProposalPage from '@/features/campaigns/pages/ProposalPage'
import NotFoundPage from '@/pages/NotFoundPage'
import RouteErrorBoundary from '@/components/RouteErrorBoundary'
import { findRouteItem } from '@/config/routeConfig'

// Quyền của từng route lấy từ routeConfig (cùng nguồn với Sidebar)
const usersRoute = findRouteItem('users')
const siteListRoute = findRouteItem('site-list')
const proposalsRoute = findRouteItem('site-proposals')

export const router = createBrowserRouter([
    {
        path: '/login',
        element: <LoginPage />,
        errorElement: <RouteErrorBoundary />,
    },
    {
        path: '/403-forbidden',
        element: <ForbiddenPage />,
        errorElement: <RouteErrorBoundary />,
    },
    // Đường dẫn cũ
    { path: '/403', element: <Navigate to="/403-forbidden" replace /> },
    {
        element: <ProtectedRoute />,
        errorElement: <RouteErrorBoundary />,
        children: [
            {
                element: <AppLayout />,
                children: [
                    { path: '/', element: <DashboardPage /> },
                    // Dùng chung cho mọi tài khoản đã đăng nhập
                    { path: '/account/profile', element: <ProfilePage /> },
                    {
                        // Route theo quyền: phải nằm trong AppLayout (nơi đồng bộ permissions)
                        element: <ProtectedRoute requiredPermission={usersRoute.requiredPermission} />,
                        children: [{ path: usersRoute.path, element: <UserManagementPage /> }],
                    },
                    // Điểm hiến & lịch slot
                    { path: '/campaigns', element: <Navigate to={siteListRoute.path} replace /> },
                    {
                        element: <ProtectedRoute requiredPermission={siteListRoute.requiredPermission} />,
                        children: [{ path: siteListRoute.path, element: <SiteMonitorPage /> }],
                    },
                    {
                        element: <ProtectedRoute requiredPermission={proposalsRoute.requiredPermission} />,
                        children: [{ path: proposalsRoute.path, element: <ProposalPage /> }],
                    },
                    // Bắt tất cả các đường dẫn chưa có giao diện bên trong AppLayout
                    { path: '*', element: <NotFoundPage /> },
                ],
            },
        ],
    },
    // Bắt các đường dẫn không xác định ở cấp ngoài cùng
    {
        path: '*',
        element: <NotFoundPage />,
    },
])
