import { createBrowserRouter, Navigate } from 'react-router-dom'
import LoginPage from '@/features/auth/pages/LoginPage'
import ProtectedRoute from '@/features/auth/components/ProtectedRoute'
import AppLayout from '@/components/layouts/AppLayout'
import DashboardPage from '@/pages/DashboardPage'
import ForbiddenPage from '@/pages/ForbiddenPage'
import UserManagementPage from '@/features/users/pages/UserManagementPage'
import ProfilePage from '@/features/account/pages/ProfilePage'
import NotFoundPage from '@/pages/NotFoundPage'
import RouteErrorBoundary from '@/components/RouteErrorBoundary'

export const router = createBrowserRouter([
    {
        path: '/login',
        element: <LoginPage />,
        errorElement: <RouteErrorBoundary />,
    },
    {
        path: '/403',
        element: <ForbiddenPage />,
        errorElement: <RouteErrorBoundary />,
    },
    {
        element: <ProtectedRoute />,
        errorElement: <RouteErrorBoundary />,
        children: [
            {
                element: <AppLayout />,
                children: [
                    { path: '/', element: <DashboardPage /> },
                    // Trang chủ cho role SYS tự động chuyển đến Quản lý người dùng
                    { path: '/admin', element: <Navigate to="/admin/users" replace /> },
                    // Dùng chung cho mọi role
                    { path: '/account/profile', element: <ProfilePage /> },
                    {
                        element: <ProtectedRoute allowedRoles={['SYS']} />,
                        children: [
                            { path: '/admin/users', element: <UserManagementPage /> },
                        ],
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
