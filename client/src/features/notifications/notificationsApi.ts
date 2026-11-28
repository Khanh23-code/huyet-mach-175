import { axiosClient } from '@/services/axiosClient'
import type {
    ListNotificationsParams,
    MarkAllNotificationsReadResponse,
    MarkNotificationReadResponse,
    NotificationInboxPage,
    UnreadCountResponse,
} from './types'

export const notificationsApi = {
    list: (params: ListNotificationsParams = {}) =>
        axiosClient
            .get<NotificationInboxPage>('/api/notifications', {
                params: {
                    unreadOnly: params.unreadOnly ?? false,
                    pageNumber: params.pageNumber ?? 1,
                    pageSize: params.pageSize ?? 12,
                },
            })
            .then((r) => r.data),

    unreadCount: () =>
        axiosClient
            .get<UnreadCountResponse>('/api/notifications/unread-count')
            .then((r) => r.data),

    markRead: (id: string) =>
        axiosClient
            .patch<MarkNotificationReadResponse>(`/api/notifications/${id}/read`)
            .then((r) => r.data),

    markAllRead: () =>
        axiosClient
            .patch<MarkAllNotificationsReadResponse>('/api/notifications/read-all')
            .then((r) => r.data),
}
