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
import { PERMISSIONS } from '@/config/permissions'

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
const bagScanRoute = findRouteItem('bag-scan')
const stockRoute = findRouteItem('stock')
const returnsScanRoute = findRouteItem('returns-scan')
const expiryRoute = findRouteItem('expiry-alerts')
const clinicalRequestsRoute = findRouteItem('clinical-requests')
const clinicalHandoversRoute = findRouteItem('clinical-handovers')
const supplyDemandRoute = findRouteItem('supply-demand')
const coordinationQueueRoute = findRouteItem('coordination-queue')
const coordinationHandoverRoute = findRouteItem('coordination-handover')

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
                    { path: '/account/notifications', ...lazyPage(() => import('@/features/notifications/pages/NotificationsCenterPage')) },
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
                    guarded(bagScanRoute, () => import('@/features/inventory/pages/BagScanPage')),
                    guarded(stockRoute, () => import('@/features/inventory/pages/StockPage')),
                    {
                        element: <ProtectedRoute requiredPermission={returnsScanRoute.requiredPermission} />,
                        children: [
                            { path: returnsScanRoute.path, ...lazyPage(() => import('@/features/inventory/pages/ReturnScanPage')) },
                            { path: '/inventory/returns/:barcode', ...lazyPage(() => import('@/features/inventory/pages/ReturnDetailPage')) },
                        ],
                    },
                    guarded(expiryRoute, () => import('@/features/inventory/pages/ExpiryAlertsPage')),
                    guarded(clinicalRequestsRoute, () => import('@/features/clinical/pages/ClinicalBloodRequestsPage')),
                    {
                        element: <ProtectedRoute requiredPermission={clinicalHandoversRoute.requiredPermission} />,
                        children: [
                            { path: clinicalHandoversRoute.path, ...lazyPage(() => import('@/features/clinical/pages/ClinicalHandoverScanPage')) },
                            { path: '/clinical/handovers/:sessionId', ...lazyPage(() => import('@/features/clinical/pages/ClinicalHandoverDetailPage')) },
                        ],
                    },
                    {
                        element: <ProtectedRoute requiredPermission={PERMISSIONS.BLOOD_RETURN_EDIT} />,
                        children: [
                            { path: '/clinical/returns', ...lazyPage(() => import('@/features/clinical/pages/ClinicalReturnCreatePage')) },
                        ],
                    },
                    guarded(supplyDemandRoute, () => import('@/features/coordination/pages/SupplyDemandPage')),
                    guarded(coordinationQueueRoute, () => import('@/features/coordination/pages/CoordinationQueuePage')),
                    {
                        element: <ProtectedRoute requiredPermission={coordinationHandoverRoute.requiredPermission} />,
                        children: [
                            { path: coordinationHandoverRoute.path, ...lazyPage(() => import('@/features/coordination/pages/HandoverScanPage')) },
                            { path: '/coordination/handover/:barcode', ...lazyPage(() => import('@/features/coordination/pages/HandoverDetailPage')) },
                        ],
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