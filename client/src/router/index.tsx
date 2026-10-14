import { createBrowserRouter } from 'react-router-dom'
import LoginPage from '@/features/auth/pages/LoginPage'
import ProtectedRoute from '@/features/auth/components/ProtectedRoute'
import AppLayout from '@/components/layouts/AppLayout'
import DashboardPage from '@/pages/DashboardPage'
import ForbiddenPage from '@/pages/ForbiddenPage'
import UserManagementPage from "@/features/users/pages/UserManagementPage.tsx";

export const router = createBrowserRouter([
    { path: '/login', element: <LoginPage /> },
    { path: '/403', element: <ForbiddenPage /> },
    {
        element: <ProtectedRoute />,
        children: [
            {
                element: <AppLayout />,
                children: [
                    { path: '/', element: <DashboardPage /> },
                    { element: <ProtectedRoute allowedRoles={['SYS']} />, children: [
                            { path: '/admin/users', element: <UserManagementPage /> },
                        ]},
                    // route cần quyền riêng:
                    // { element: <ProtectedRoute allowedRoles={['AD']} />, children: [{ path: '/admin', element: <AdminPage /> }] },
                ],
            },
        ],
    },
])