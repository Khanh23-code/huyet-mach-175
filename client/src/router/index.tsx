import { createBrowserRouter, Navigate } from 'react-router-dom'
import LoginPage from '@/features/auth/pages/LoginPage'
import ProtectedRoute from '@/features/auth/components/ProtectedRoute'
import AppLayout from '@/components/layouts/AppLayout'
import DashboardPage from '@/pages/DashboardPage'
import ForbiddenPage from '@/pages/ForbiddenPage'
import UserManagementPage from '@/features/users/pages/UserManagementPage'
import ProfilePage from '@/features/account/pages/ProfilePage'
import SiteMonitorPage from '@/features/campaigns/pages/SiteMonitorPage'
import CampaignSummaryPage from '@/features/campaigns/pages/CampaignSummaryPage'
import ProposalPage from '@/features/campaigns/pages/ProposalPage'
import ReceptionQueuePage from '@/features/reception/pages/ReceptionQueuePage'
import ScreeningPage from '@/features/screening/pages/ScreeningPage'
import NotFoundPage from '@/pages/NotFoundPage'
import RouteErrorBoundary from '@/components/RouteErrorBoundary'
import { findRouteItem } from '@/config/routeConfig'
import SessionPage from '@/features/sessions/pages/SessionPage'
import ExternalBagPage from '@/features/inventory/pages/ExternalBagPage'
import ProcessingPage from '@/features/inventory/pages/ProcessingPage'

const usersRoute = findRouteItem('users')
const siteListRoute = findRouteItem('site-list')
const proposalsRoute = findRouteItem('site-proposals')
const queueRoute = findRouteItem('queue')
const screeningRoute = findRouteItem('screening')
const sessionsRoute = findRouteItem('sessions')
const bagIntakeRoute = findRouteItem('bag-intake')
const processingRoute = findRouteItem('processing')

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
        element: <ProtectedRoute />,
        errorElement: <RouteErrorBoundary />,
        children: [
            {
                element: <AppLayout />,
                children: [
                    { path: '/', element: <DashboardPage /> },
                    { path: '/account/profile', element: <ProfilePage /> },
                    {
                        element: <ProtectedRoute requiredPermission={usersRoute.requiredPermission} />,
                        children: [{ path: usersRoute.path, element: <UserManagementPage /> }],
                    },
                    { path: '/campaigns', element: <Navigate to={siteListRoute.path} replace /> },
                    {
                        element: <ProtectedRoute requiredPermission={siteListRoute.requiredPermission} />,
                        children: [
                            { path: siteListRoute.path, element: <SiteMonitorPage /> },
                            { path: '/campaigns/:id/summary', element: <CampaignSummaryPage /> },
                        ],
                    },
                    {
                        element: <ProtectedRoute requiredPermission={proposalsRoute.requiredPermission} />,
                        children: [{ path: proposalsRoute.path, element: <ProposalPage /> }],
                    },
                    {
                        element: <ProtectedRoute requiredPermission={queueRoute.requiredPermission} />,
                        children: [{ path: queueRoute.path, element: <ReceptionQueuePage /> }],
                    },
                    {
                        element: <ProtectedRoute requiredPermission={screeningRoute.requiredPermission} />,
                        children: [{ path: screeningRoute.path, element: <ScreeningPage /> }],
                    },
                    {
                        element: <ProtectedRoute requiredPermission={sessionsRoute.requiredPermission} />,
                        children: [{ path: sessionsRoute.path, element: <SessionPage /> }],
                    },
                    {
                        element: <ProtectedRoute requiredPermission={bagIntakeRoute.requiredPermission} />,
                        children: [{ path: bagIntakeRoute.path, element: <ExternalBagPage /> }],
                    },
                    {
                        element: <ProtectedRoute requiredPermission={processingRoute.requiredPermission} />,
                        children: [{ path: processingRoute.path, element: <ProcessingPage /> }],
                    },
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