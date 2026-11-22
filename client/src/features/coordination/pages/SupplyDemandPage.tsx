import { useMemo } from 'react'
import { Alert, Button, Empty, Skeleton, Space } from 'antd'
import {
    AlertOutlined,
    ReloadOutlined,
    WarningFilled,
} from '@ant-design/icons'
import { BloodGroupTag, PageContainer, PageHeader, StatusPill } from '@/shared/components'
import { useComponentTypes, useSupplyDemand } from '../hooks'
import './SupplyDemandPage.css'

export default function SupplyDemandPage() {
    const {
        types,
        selectedTypeId,
        setSelectedTypeId,
        loading: typesLoading,
        error: typesError,
    } = useComponentTypes()

    const {
        data,
        loading: dataLoading,
        isFetching,
        error: dataError,
        lastUpdated,
        refetch,
    } = useSupplyDemand(selectedTypeId)

    // Chuẩn hóa nhãn hiển thị cho 4 loại chế phẩm theo yêu cầu UI
    const getTabLabel = (code: string, originalName: string) => {
        switch (code.toUpperCase()) {
            case 'PRBC':
                return 'Hồng cầu lắng'
            case 'PLT':
                return 'Khối tiểu cầu'
            case 'FFP':
                return 'Huyết tương'
            case 'WB':
                return 'Tủa lạnh'
            default:
                return originalName
        }
    }

    const formattedTime = useMemo(() => {
        if (!lastUpdated) return ''
        const hours = String(lastUpdated.getHours()).padStart(2, '0')
        const minutes = String(lastUpdated.getMinutes()).padStart(2, '0')
        return `${hours}:${minutes}`
    }, [lastUpdated])

    // Tính giá trị cực đại để render tỷ lệ thanh progress bar chuẩn xác
    const maxBarValue = useMemo(() => {
        if (!data || !data.details || data.details.length === 0) return 1
        const max = Math.max(
            ...data.details.map((d) => Math.max(d.stockUnits, d.demandUnits)),
        )
        return max > 0 ? max : 1
    }, [data])

    const getBarWidth = (value: number) => {
        if (value <= 0) return '0%'
        const percent = Math.min(100, Math.max(4, Math.round((value / maxBarValue) * 100)))
        return `${percent}%`
    }

    return (
        <PageContainer className="supply-demand-page">
            <PageHeader
                breadcrumb={[{ title: 'Kho máu' }, { title: 'Điều phối Cung – Cầu' }]}
                title="Bảng điều phối Cung – Cầu"
                subtitle="So sánh tồn kho khả dụng với tổng nhu cầu đang chờ cấp phát theo nhóm máu và sản phẩm."
                extra={
                    <div className="coordination-header-right">
                        <Space size={8}>
                            {formattedTime && (
                                <StatusPill tone="success">Cập nhật {formattedTime}</StatusPill>
                            )}
                            <Button
                                icon={<ReloadOutlined spin={dataLoading || isFetching} />}
                                onClick={() => refetch()}
                                size="small"
                            >
                                Làm mới
                            </Button>
                        </Space>
                        <span className="refresh-subtext">Tự động làm mới mỗi 5 phút</span>
                    </div>
                }
            />

            {/* Error State khi tải danh mục chế phẩm */}
            {typesError && (
                <Alert
                    message="Lỗi tải danh mục chế phẩm"
                    description={typesError}
                    type="warning"
                    showIcon
                />
            )}

            {/* Alert Banner: Cảnh báo nhóm máu thiếu hụt */}
            {data && data.criticalShortages && data.criticalShortages.length > 0 && (
                <div className="shortage-banner">
                    <WarningFilled className="shortage-icon" />
                    <div>
                        <div className="shortage-title">
                            {data.criticalShortages.length} nhóm máu có nhu cầu vượt tồn kho khả dụng
                        </div>
                        <div className="shortage-desc">
                            {data.criticalShortages.join(', ')} — Cần ưu tiên bổ sung hoặc điều tiết nguồn cung ngay.
                        </div>
                    </div>
                </div>
            )}

            {/* Chuyển tab 4 loại chế phẩm */}
            <div className="component-switcher-tabs">
                {types.map((type) => (
                    <button
                        key={type.componentTypeId}
                        type="button"
                        className={`component-tab-btn ${selectedTypeId === type.componentTypeId ? 'is-active' : ''}`}
                        onClick={() => setSelectedTypeId(type.componentTypeId)}
                    >
                        {getTabLabel(type.typeCode, type.typeName)}
                    </button>
                ))}
            </div>

            {/* Error State khi tải dữ liệu Cung - Cầu */}
            {dataError && (
                <Alert
                    message="Không thể tải dữ liệu Cung – Cầu"
                    description={dataError}
                    type="error"
                    showIcon
                    action={
                        <Button size="small" danger onClick={() => refetch()}>
                            Thử lại
                        </Button>
                    }
                />
            )}

            {/* Loading State */}
            {(typesLoading || dataLoading) && !data && (
                <Skeleton active paragraph={{ rows: 8 }} />
            )}

            {/* Empty State */}
            {!dataLoading && !dataError && (!data || !data.details || data.details.length === 0) && (
                <Empty
                    description="Chưa có dữ liệu tồn kho hoặc nhu cầu cho chế phẩm này."
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                />
            )}

            {/* Main Content: Thống kê & 8 Card nhóm máu */}
            {data && data.details && data.details.length > 0 && (
                <>
                    {/* Section Header & Legend */}
                    <div className="section-meta-bar">
                        <div className="section-summary-text">
                            <span className="section-summary-title">
                                {getTabLabel(data.componentCode, data.componentName)}
                            </span>
                            <span>
                                Tổng tồn kho: <strong>{data.totalAvailableStock}</strong> đơn vị · Tổng nhu cầu:{' '}
                                <strong>{data.totalPendingDemand}</strong> đơn vị
                            </span>
                        </div>

                        <div className="supply-demand-legend">
                            <div className="legend-item">
                                <span className="legend-color-box stock" />
                                <span>Tồn kho khả dụng</span>
                            </div>
                            <div className="legend-item">
                                <span className="legend-color-box demand" />
                                <span>Nhu cầu chờ cấp</span>
                            </div>
                            <div className="legend-item">
                                <span className="legend-color-box shortage" />
                                <span>Vượt tồn kho</span>
                            </div>
                        </div>
                    </div>

                    {/* Danh sách 8 nhóm máu */}
                    <div className="blood-groups-stack">
                        {data.details.map((item) => (
                            <div
                                key={item.bloodType}
                                className={`blood-group-card ${item.isShortage ? 'has-shortage' : ''}`}
                            >
                                <div className="blood-group-card-header">
                                    <BloodGroupTag value={item.bloodType} size="md" />
                                    {item.isShortage && (
                                        <StatusPill tone="danger" dot={false}>
                                            <span className="shortage-pill-content">
                                                <AlertOutlined /> Thiếu {item.shortageUnits} đv
                                            </span>
                                        </StatusPill>
                                    )}
                                </div>

                                {/* Thanh Tồn kho */}
                                <div className="bars-row">
                                    <span className="bar-label">Tồn kho</span>
                                    <div className="bar-track">
                                        <div
                                            className="bar-fill is-stock"
                                            style={{ width: getBarWidth(item.stockUnits) }}
                                        />
                                    </div>
                                    <span className="bar-val">{item.stockUnits} đv</span>
                                </div>

                                {/* Thanh Nhu cầu */}
                                <div className="bars-row">
                                    <span className="bar-label">Nhu cầu</span>
                                    <div className="bar-track">
                                        <div
                                            className={`bar-fill ${item.isShortage ? 'is-shortage' : 'is-demand'}`}
                                            style={{ width: getBarWidth(item.demandUnits) }}
                                        />
                                    </div>
                                    <span className="bar-val">{item.demandUnits} đv</span>
                                </div>
                            </div>
                        ))}
                    </div>
                </>
            )}
        </PageContainer>
    )
}
