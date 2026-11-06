import type { ComponentType } from 'react'
import { createBrowserRouter, Navigate, type RouteObject } from 'react-router-dom'
import LoginPage from '@/features/auth/pages/LoginPage'
import AuthGate from '@/features/auth/components/AuthGate'
import ProtectedRoute from '@/features/auth/components/ProtectedRoute'
import AppLayout from '@/components/layouts/AppLayout'
import ForbiddenPage from '@/pages/ForbiddenPage'
import NotFoundPage from '@/pages/NotFoundPage'
import RouteErrorBoundary from '@/components/RouteErrorBoundary'
import { findRouteItem } from '@/config/routeConfig'

type RouteItem = ReturnType<typeof findRouteItem>
type PageLoader = () => Promise<{ default: ComponentType }>

/** Code-split: chỉ tải JS của trang khi người dùng vào trang đó */
const lazyPage = (loader: PageLoader): Pick<RouteObject, 'lazy'> => ({
    lazy: async () => ({ Component: (await loader()).default }),
})

/** Route có kiểm tra quyền + lazy page */
const guarded = (route: RouteItem, loader: PageLoader): RouteObject => ({
    element: <ProtectedRoute requiredPermission={route.requiredPermission} />,
    children: [{ path: route.path, ...lazyPage(loader) }],
})

const usersRoute = findRouteItem('users')
const siteListRoute = findRouteItem('site-list')
const proposalsRoute = findRouteItem('site-proposals')
const queueRoute = findRouteItem('queue')
const screeningRoute = findRouteItem('screening')
const sessionsRoute = findRouteItem('sessions')
const bagIntakeRoute = findRouteItem('bag-intake')
const processingRoute = findRouteItem('processing')
const labRoute = findRouteItem('lab-tests')
const disposalRoute = findRouteItem('disposals')
const approvalRoute = findRouteItem('disposal-approvals')

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
    { path: '/403', element: <Navigate to="/403-forbidden" replace /> },
    {
        element: <AuthGate />,
        errorElement: <RouteErrorBoundary />,
        children: [
            {
                element: <AppLayout />,
                children: [
                    { path: '/', ...lazyPage(() => import('@/pages/DashboardPage')) },
                    { path: '/account/profile', ...lazyPage(() => import('@/features/account/pages/ProfilePage')) },
                    guarded(usersRoute, () => import('@/features/users/pages/UserManagementPage')),
                    { path: '/campaigns', element: <Navigate to={siteListRoute.path} replace /> },
                    {
                        element: <ProtectedRoute requiredPermission={siteListRoute.requiredPermission} />,
                        children: [
                            { path: siteListRoute.path, ...lazyPage(() => import('@/features/campaigns/pages/SiteMonitorPage')) },
                            { path: '/campaigns/:id/summary', ...lazyPage(() => import('@/features/campaigns/pages/CampaignSummaryPage')) },
                        ],
                    },
                    guarded(proposalsRoute, () => import('@/features/campaigns/pages/ProposalPage')),
                    guarded(queueRoute, () => import('@/features/reception/pages/ReceptionQueuePage')),
                    guarded(screeningRoute, () => import('@/features/screening/pages/ScreeningPage')),
                    guarded(sessionsRoute, () => import('@/features/sessions/pages/SessionPage')),
                    guarded(bagIntakeRoute, () => import('@/features/inventory/pages/ExternalBagPage')),
                    guarded(processingRoute, () => import('@/features/inventory/pages/ProcessingPage')),
                    guarded(labRoute, () => import('@/features/inventory/pages/LabTestPage')),
                    guarded(disposalRoute, () => import('@/features/inventory/pages/DisposalPage')),
                    guarded(approvalRoute, () => import('@/features/inventory/pages/DisposalApprovalPage')),
                    { path: '*', element: <NotFoundPage /> },
                ],
            },
        ],
    },
    {
        path: '*',
        element: <NotFoundPage />,
    },
])