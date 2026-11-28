import { useState } from 'react'
import { Button, Empty, Pagination, Segmented, Space, Spin, Tooltip } from 'antd'
import {
    ArrowRightOutlined,
    CheckCircleOutlined,
    CheckOutlined,
    ReloadOutlined,
} from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import { PageContainer, PageHeader, SectionCard } from '@/shared/components'
import {
    useNotificationActions,
    useNotificationList,
    useUnreadNotificationCount,
} from '../useNotificationInbox'
import { formatFullDateTimeVi, formatRelativeTimeVi, getModuleLabelVi } from '../format'
import type { NotificationItem } from '../types'
import './NotificationsCenterPage.css'

type FilterTab = 'all' | 'unread'

const PAGE_SIZE = 12

export default function NotificationsCenterPage() {
    const navigate = useNavigate()
    const [filterTab, setFilterTab] = useState<FilterTab>('all')
    const [pageNumber, setPageNumber] = useState(1)

    const unreadOnly = filterTab === 'unread'

    const { data: unreadData } = useUnreadNotificationCount()
    const {
        data: pageData,
        isLoading,
        isFetching,
        refetch,
    } = useNotificationList({
        unreadOnly,
        pageNumber,
        pageSize: PAGE_SIZE,
    })

    const { markRead, markAllRead, isMarkingAllRead } = useNotificationActions()

    const unreadCount = unreadData?.count ?? pageData?.unreadCount ?? 0
    const items = pageData?.items ?? []
    const totalCount = pageData?.totalCount ?? 0

    const handleChangeTab = (next: FilterTab) => {
        setFilterTab(next)
        setPageNumber(1)
    }

    const handleOpenAction = (item: NotificationItem) => {
        if (!item.isRead) {
            markRead(item.id)
        }
        if (item.actionPath) {
            navigate(item.actionPath)
        }
    }

    return (
        <PageContainer>
            <PageHeader
                breadcrumb={[{ title: 'Tài khoản' }, { title: 'Trung tâm thông báo' }]}
                title="Trung tâm thông báo"
                subtitle="Theo dõi các thông báo điều phối máu, kiểm định kho, phê duyệt biên bản và cảnh báo nghiệp vụ."
                extra={
                    <Space wrap>
                        <Button
                            icon={<ReloadOutlined />}
                            loading={isFetching && !isLoading}
                            onClick={() => void refetch()}
                        >
                            Làm mới
                        </Button>
                        <Button
                            type="primary"
                            icon={<CheckOutlined />}
                            disabled={unreadCount === 0 || isMarkingAllRead}
                            loading={isMarkingAllRead}
                            onClick={() => markAllRead()}
                        >
                            Đánh dấu tất cả đã đọc
                        </Button>
                    </Space>
                }
            />

            <SectionCard>
                <div className="notif-center__toolbar">
                    <Segmented<FilterTab>
                        value={filterTab}
                        onChange={handleChangeTab}
                        options={[
                            { label: 'Tất cả thông báo', value: 'all' },
                            {
                                label:
                                    unreadCount > 0
                                        ? `Chưa đọc (${unreadCount > 99 ? '99+' : unreadCount})`
                                        : 'Chưa đọc',
                                value: 'unread',
                            },
                        ]}
                    />

                    <span className="notif-center__summary">
                        Tổng cộng <strong>{totalCount}</strong> thông báo
                    </span>
                </div>

                {isLoading ? (
                    <div className="notif-center__loading">
                        <Spin />
                        <span>Đang tải danh sách thông báo...</span>
                    </div>
                ) : items.length === 0 ? (
                    <div className="notif-center__empty">
                        <Empty
                            description={
                                unreadOnly
                                    ? 'Hiện không còn thông báo nào chưa đọc.'
                                    : 'Hộp thư thông báo của bạn đang trống.'
                            }
                        />
                    </div>
                ) : (
                    <>
                        <div className="notif-center__list">
                            {items.map((item) => {
                                const isCritical = item.priority === 'Critical'
                                return (
                                    <div
                                        key={item.id}
                                        className={[
                                            'notif-center__card',
                                            !item.isRead ? 'is-unread' : '',
                                            isCritical ? 'is-critical' : '',
                                        ]
                                            .filter(Boolean)
                                            .join(' ')}
                                    >
                                        <div className="notif-center__card-indicator">
                                            <span
                                                className={`notif-center__dot${!item.isRead ? ' is-visible' : ''}`}
                                                aria-hidden
                                            />
                                        </div>

                                        <div className="notif-center__card-main">
                                            <div className="notif-center__card-meta">
                                                <span className="notif-center__module-badge">
                                                    {getModuleLabelVi(item.sourceModule)}
                                                </span>

                                                {isCritical && (
                                                    <span className="notif-center__critical-badge">
                                                        Khẩn cấp
                                                    </span>
                                                )}

                                                {item.isRead ? (
                                                    <span className="notif-center__read-state">
                                                        Đã đọc
                                                    </span>
                                                ) : (
                                                    <span className="notif-center__unread-state">
                                                        Mới
                                                    </span>
                                                )}

                                                <Tooltip title={formatFullDateTimeVi(item.createdAt)}>
                                                    <span className="notif-center__timestamp">
                                                        {formatRelativeTimeVi(item.createdAt)} (
                                                        {formatFullDateTimeVi(item.createdAt)})
                                                    </span>
                                                </Tooltip>
                                            </div>

                                            <h3 className="notif-center__card-title">{item.title}</h3>
                                            <p className="notif-center__card-content">{item.content}</p>

                                            <div className="notif-center__card-actions">
                                                {item.actionPath && (
                                                    <Button
                                                        type="primary"
                                                        size="small"
                                                        ghost={!isCritical}
                                                        danger={isCritical}
                                                        icon={<ArrowRightOutlined />}
                                                        onClick={() => handleOpenAction(item)}
                                                    >
                                                        Đi tới trang xử lý
                                                    </Button>
                                                )}

                                                {!item.isRead && (
                                                    <Button
                                                        type="text"
                                                        size="small"
                                                        icon={<CheckCircleOutlined />}
                                                        onClick={() => markRead(item.id)}
                                                    >
                                                        Đánh dấu đã đọc
                                                    </Button>
                                                )}
                                            </div>
                                        </div>
                                    </div>
                                )
                            })}
                        </div>

                        {totalCount > PAGE_SIZE && (
                            <div className="notif-center__pagination">
                                <Pagination
                                    current={pageNumber}
                                    pageSize={PAGE_SIZE}
                                    total={totalCount}
                                    showSizeChanger={false}
                                    onChange={(page) => setPageNumber(page)}
                                />
                            </div>
                        )}
                    </>
                )}
            </SectionCard>
        </PageContainer>
    )
}
