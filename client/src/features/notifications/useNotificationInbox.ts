import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { message } from 'antd'
import { useAuthStore } from '@/features/auth/authStore'
import { notificationsApi } from './notificationsApi'
import type { NotificationInboxPage, UnreadCountResponse } from './types'

export const notificationKeys = {
    all: (userId?: string) => ['notifications', userId ?? 'anonymous'] as const,
    unreadCount: (userId?: string) => [...notificationKeys.all(userId), 'unread-count'] as const,
    lists: (userId?: string) => [...notificationKeys.all(userId), 'list'] as const,
    list: (userId: string | undefined, unreadOnly: boolean, pageNumber: number, pageSize: number) =>
        [...notificationKeys.lists(userId), { unreadOnly, pageNumber, pageSize }] as const,
}

function useCurrentNotificationUserKey() {
    return useAuthStore((s) => s.user?.userId ?? s.user?.id ?? s.user?.username ?? 'authenticated')
}

export function useUnreadNotificationCount() {
    const userKey = useCurrentNotificationUserKey()
    const accessToken = useAuthStore((s) => s.accessToken)
    const enabled = Boolean(accessToken)

    return useQuery({
        queryKey: notificationKeys.unreadCount(userKey),
        queryFn: notificationsApi.unreadCount,
        enabled,
        refetchInterval: 60_000,
        refetchIntervalInBackground: false,
        refetchOnWindowFocus: true,
        staleTime: 20_000,
    })
}

export interface UseNotificationListOptions {
    unreadOnly?: boolean
    pageNumber?: number
    pageSize?: number
    enabled?: boolean
}

export function useNotificationList(options: UseNotificationListOptions = {}) {
    const userKey = useCurrentNotificationUserKey()
    const accessToken = useAuthStore((s) => s.accessToken)
    const unreadOnly = options.unreadOnly ?? false
    const pageNumber = options.pageNumber ?? 1
    const pageSize = options.pageSize ?? 12
    const isEnabled = Boolean(accessToken) && (options.enabled ?? true)

    return useQuery({
        queryKey: notificationKeys.list(userKey, unreadOnly, pageNumber, pageSize),
        queryFn: () => notificationsApi.list({ unreadOnly, pageNumber, pageSize }),
        enabled: isEnabled,
        refetchOnWindowFocus: true,
    })
}

export function useNotificationActions() {
    const queryClient = useQueryClient()
    const userKey = useCurrentNotificationUserKey()

    const markReadMutation = useMutation({
        mutationFn: (id: string) => notificationsApi.markRead(id),
        onMutate: async (id: string) => {
            const rootKey = notificationKeys.all(userKey)
            await queryClient.cancelQueries({ queryKey: rootKey })

            const previousUnread = queryClient.getQueryData<UnreadCountResponse>(
                notificationKeys.unreadCount(userKey),
            )
            const previousLists = queryClient.getQueriesData<NotificationInboxPage>({
                queryKey: notificationKeys.lists(userKey),
            })

            const nowIso = new Date().toISOString()

            queryClient.setQueryData<UnreadCountResponse>(
                notificationKeys.unreadCount(userKey),
                (old) => (old ? { count: Math.max(0, old.count - 1) } : { count: 0 }),
            )

            for (const [key, page] of previousLists) {
                if (!page) continue
                const target = page.items.find((item) => item.id === id)
                if (!target || target.isRead) continue

                const filterMeta = key[3] as { unreadOnly?: boolean } | undefined
                const isUnreadOnlyFilter = Boolean(filterMeta?.unreadOnly)

                queryClient.setQueryData<NotificationInboxPage>(key, {
                    ...page,
                    unreadCount: Math.max(0, page.unreadCount - 1),
                    totalCount: isUnreadOnlyFilter ? Math.max(0, page.totalCount - 1) : page.totalCount,
                    items: isUnreadOnlyFilter
                        ? page.items.filter((item) => item.id !== id)
                        : page.items.map((item) =>
                              item.id === id ? { ...item, isRead: true, readAt: nowIso } : item,
                          ),
                })
            }

            return { previousUnread, previousLists }
        },
        onError: (_err, _id, context) => {
            if (context?.previousUnread) {
                queryClient.setQueryData(notificationKeys.unreadCount(userKey), context.previousUnread)
            }
            if (context?.previousLists) {
                for (const [key, data] of context.previousLists) {
                    queryClient.setQueryData(key, data)
                }
            }
            message.error('Không thể đánh dấu đã đọc. Vui lòng thử lại.')
        },
        onSettled: () => {
            void queryClient.invalidateQueries({ queryKey: notificationKeys.all(userKey) })
        },
    })

    const markAllReadMutation = useMutation({
        mutationFn: () => notificationsApi.markAllRead(),
        onMutate: async () => {
            const rootKey = notificationKeys.all(userKey)
            await queryClient.cancelQueries({ queryKey: rootKey })

            const previousUnread = queryClient.getQueryData<UnreadCountResponse>(
                notificationKeys.unreadCount(userKey),
            )
            const previousLists = queryClient.getQueriesData<NotificationInboxPage>({
                queryKey: notificationKeys.lists(userKey),
            })

            const nowIso = new Date().toISOString()

            queryClient.setQueryData<UnreadCountResponse>(notificationKeys.unreadCount(userKey), {
                count: 0,
            })

            for (const [key, page] of previousLists) {
                if (!page) continue
                const filterMeta = key[3] as { unreadOnly?: boolean } | undefined
                const isUnreadOnlyFilter = Boolean(filterMeta?.unreadOnly)

                queryClient.setQueryData<NotificationInboxPage>(key, {
                    ...page,
                    unreadCount: 0,
                    totalCount: isUnreadOnlyFilter ? 0 : page.totalCount,
                    items: isUnreadOnlyFilter
                        ? []
                        : page.items.map((item) =>
                              item.isRead ? item : { ...item, isRead: true, readAt: nowIso },
                          ),
                })
            }

            return { previousUnread, previousLists }
        },
        onError: (_err, _vars, context) => {
            if (context?.previousUnread) {
                queryClient.setQueryData(notificationKeys.unreadCount(userKey), context.previousUnread)
            }
            if (context?.previousLists) {
                for (const [key, data] of context.previousLists) {
                    queryClient.setQueryData(key, data)
                }
            }
            message.error('Không thể đánh dấu tất cả thông báo là đã đọc.')
        },
        onSettled: () => {
            void queryClient.invalidateQueries({ queryKey: notificationKeys.all(userKey) })
        },
    })

    return {
        markRead: markReadMutation.mutate,
        markReadAsync: markReadMutation.mutateAsync,
        isMarkingRead: markReadMutation.isPending,
        markAllRead: markAllReadMutation.mutate,
        isMarkingAllRead: markAllReadMutation.isPending,
    }
}
