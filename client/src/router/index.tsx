import { createBrowserRouter } from 'react-router-dom'
import LoginPage from '@/features/auth/pages/LoginPage'
import ProtectedRoute from '@/features/auth/components/ProtectedRoute'
import AppLayout from '@/components/layouts/AppLayout'
import DashboardPage from '@/pages/DashboardPage'
import ForbiddenPage from '@/pages/ForbiddenPage'
import UserManagementPage from "@/features/users/pages/UserManagementPage.tsx";
import ProfilePage from '@/features/account/pages/ProfilePage'

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
                    // Dùng chung cho mọi role
                    { path: '/account/profile', element: <ProfilePage /> },
                    { element: <ProtectedRoute allowedRoles={['SYS']} />, children: [
                            { path: '/admin/users', element: <UserManagementPage /> },
                        ]},
                ],
            },
        ],
    },
])