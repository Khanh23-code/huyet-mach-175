export type NotificationPriority = 'Normal' | 'Critical'

export interface NotificationItem {
    id: string
    title: string
    content: string
    notificationType: string
    sourceModule: string
    resourceType?: string | null
    resourceId?: string | null
    actionPath?: string | null
    priority: NotificationPriority
    isRead: boolean
    createdAt: string
    readAt?: string | null
    expiresAt?: string | null
}

export interface NotificationInboxPage {
    items: NotificationItem[]
    totalCount: number
    unreadCount: number
    pageNumber: number
    pageSize: number
}

export interface UnreadCountResponse {
    count: number
}

export interface MarkNotificationReadResponse {
    id: string
    isRead: boolean
    readAt?: string | null
}

export interface MarkAllNotificationsReadResponse {
    updatedCount: number
}

export interface ListNotificationsParams {
    unreadOnly?: boolean
    pageNumber?: number
    pageSize?: number
}
