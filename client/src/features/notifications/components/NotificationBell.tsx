import { useState } from 'react'
import { Button, Empty, Popover, Spin, Tooltip } from 'antd'
import { BellOutlined, CheckOutlined, RightOutlined } from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import {
    useNotificationActions,
    useNotificationList,
    useUnreadNotificationCount,
} from '../useNotificationInbox'
import { formatFullDateTimeVi, formatRelativeTimeVi, getModuleLabelVi } from '../format'
import type { NotificationItem } from '../types'
import './NotificationBell.css'

export default function NotificationBell() {
    const navigate = useNavigate()
    const [open, setOpen] = useState(false)
    const [unreadOnly, setUnreadOnly] = useState(false)

    const { data: unreadData } = useUnreadNotificationCount()
    const unreadCount = unreadData?.count ?? 0

    const { data: inboxPage, isLoading, isFetching } = useNotificationList({
        unreadOnly,
        pageNumber: 1,
        pageSize: 12,
        enabled: open,
    })

    const { markRead, markAllRead, isMarkingAllRead } = useNotificationActions()

    const items = inboxPage?.items ?? []
    const badgeText = unreadCount > 99 ? '99+' : String(unreadCount)

    const handleSelectItem = (item: NotificationItem) => {
        if (!item.isRead) {
            markRead(item.id)
        }
        setOpen(false)
        if (item.actionPath) {
            navigate(item.actionPath)
        }
    }

    const handleOpenCenter = () => {
        setOpen(false)
        navigate('/account/notifications')
    }

    const popoverContent = (
        <div className="notif-bell__panel">
            <div className="notif-bell__header">
                <div className="notif-bell__header-top">
                    <div className="notif-bell__title-group">
                        <span className="notif-bell__title">Thông báo</span>
                        {unreadCount > 0 && (
                            <span className="notif-bell__unread-pill">{badgeText} chưa đọc</span>
                        )}
                    </div>

                    <Button
                        type="link"
                        size="small"
                        icon={<CheckOutlined />}
                        disabled={unreadCount === 0 || isMarkingAllRead}
                        loading={isMarkingAllRead}
                        onClick={() => markAllRead()}
                        className="notif-bell__read-all-btn"
                    >
                        Đọc tất cả
                    </Button>
                </div>

                <div className="notif-bell__tabs" role="tablist" aria-label="Bộ lọc thông báo">
                    <button
                        type="button"
                        role="tab"
                        aria-selected={!unreadOnly}
                        className={`notif-bell__tab${!unreadOnly ? ' is-active' : ''}`}
                        onClick={() => setUnreadOnly(false)}
                    >
                        Tất cả
                    </button>
                    <button
                        type="button"
                        role="tab"
                        aria-selected={unreadOnly}
                        className={`notif-bell__tab${unreadOnly ? ' is-active' : ''}`}
                        onClick={() => setUnreadOnly(true)}
                    >
                        Chưa đọc
                    </button>
                </div>
            </div>

            <div className="notif-bell__list" aria-busy={isLoading || isFetching}>
                {isLoading ? (
                    <div className="notif-bell__loading">
                        <Spin size="small" />
                        <span>Đang tải thông báo...</span>
                    </div>
                ) : items.length === 0 ? (
                    <div className="notif-bell__empty">
                        <Empty
                            image={Empty.PRESENTED_IMAGE_SIMPLE}
                            description={
                                unreadOnly
                                    ? 'Bạn đã đọc hết mọi thông báo'
                                    : 'Chưa có thông báo nào trong hộp thư'
                            }
                        />
                    </div>
                ) : (
                    items.map((item) => {
                        const isCritical = item.priority === 'Critical'
                        return (
                            <button
                                key={item.id}
                                type="button"
                                className={[
                                    'notif-bell__item',
                                    !item.isRead ? 'is-unread' : '',
                                    isCritical ? 'is-critical' : '',
                                ]
                                    .filter(Boolean)
                                    .join(' ')}
                                onClick={() => handleSelectItem(item)}
                            >
                                <span
                                    className={`notif-bell__item-dot${!item.isRead ? ' is-visible' : ''}`}
                                    aria-hidden
                                />
                                <div className="notif-bell__item-body">
                                    <div className="notif-bell__item-meta">
                                        <span className="notif-bell__module-tag">
                                            {getModuleLabelVi(item.sourceModule)}
                                        </span>
                                        {isCritical && (
                                            <span className="notif-bell__critical-tag">Khẩn cấp</span>
                                        )}
                                        <Tooltip title={formatFullDateTimeVi(item.createdAt)}>
                                            <span className="notif-bell__time">
                                                {formatRelativeTimeVi(item.createdAt)}
                                            </span>
                                        </Tooltip>
                                    </div>

                                    <div className="notif-bell__item-title">{item.title}</div>
                                    <div className="notif-bell__item-content">{item.content}</div>
                                </div>
                            </button>
                        )
                    })
                )}
            </div>

            <div className="notif-bell__footer">
                <button
                    type="button"
                    className="notif-bell__view-all"
                    onClick={handleOpenCenter}
                >
                    <span>Xem tất cả tại Trung tâm thông báo</span>
                    <RightOutlined className="notif-bell__view-all-icon" />
                </button>
            </div>
        </div>
    )

    return (
        <Popover
            content={popoverContent}
            trigger="click"
            placement="bottomRight"
            arrow={false}
            open={open}
            onOpenChange={setOpen}
            styles={{ container: { padding: 0 } }}
        >
            <button
                type="button"
                className="app-header__icon-btn notif-bell__trigger"
                aria-label={
                    unreadCount > 0 ? `Thông báo (${unreadCount} chưa đọc)` : 'Thông báo'
                }
                aria-expanded={open}
            >
                <BellOutlined />
                {unreadCount > 0 && (
                    <span className="notif-bell__badge" aria-hidden>
                        {badgeText}
                    </span>
                )}
            </button>
        </Popover>
    )
}
