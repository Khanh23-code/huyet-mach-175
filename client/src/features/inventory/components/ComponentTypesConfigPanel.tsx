import { useState } from 'react'
import { Alert, Button, Table } from 'antd'
import { EditOutlined, ReloadOutlined } from '@ant-design/icons'
import Can from '@/features/auth/components/Can'
import { PERMISSIONS } from '@/config/permissions'
import { SectionCard, StatusPill } from '@/shared/components'
import { COMPATIBILITY_RULE_META } from '../stockFormat'
import { useBloodComponentTypesConfig } from '../stockHooks'
import type { BloodComponentTypeConfig } from '../stockTypes'
import ComponentTypeFormModal from './ComponentTypeFormModal'

export default function ComponentTypesConfigPanel() {
    const { data: items = [], isLoading, isFetching, refetch } = useBloodComponentTypesConfig()
    const [editing, setEditing] = useState<BloodComponentTypeConfig | null>(null)

    return (
        <SectionCard
            title="Cấu hình nghiệp vụ các loại chế phẩm máu"
            subtitle="Quản lý hạn sử dụng chuẩn, dải nhiệt độ bảo quản, nhóm hòa hợp miễn dịch và chính sách tái nhập kho."
            extra={
                <Button
                    icon={<ReloadOutlined spin={isFetching} />}
                    onClick={() => void refetch()}
                    disabled={isLoading || isFetching}
                >
                    Làm mới
                </Button>
            }
        >
            <Alert
                type="info"
                showIcon
                style={{ marginBottom: 16 }}
                message="Quy tắc áp dụng đồng bộ trên toàn hệ thống"
                description="Dải nhiệt độ và chính sách tái nhập kho được dùng trực tiếp để thẩm định điều kiện lưu kho và kiểm định hoàn trả tại Ngân hàng máu."
            />

            <Table<BloodComponentTypeConfig>
                size="middle"
                rowKey="componentTypeId"
                loading={isLoading}
                dataSource={items}
                pagination={false}
                scroll={{ x: 960 }}
                locale={{ emptyText: 'Chưa có cấu hình chế phẩm máu nào' }}
                columns={[
                    {
                        title: 'Mã',
                        dataIndex: 'typeCode',
                        width: 90,
                        render: (code: string) => (
                            <span className="inv-mono" style={{ fontWeight: 700 }}>
                                {code}
                            </span>
                        ),
                    },
                    {
                        title: 'Tên chế phẩm',
                        key: 'name',
                        width: 220,
                        render: (_, r) => (
                            <div>
                                <div style={{ fontWeight: 600 }}>{r.typeName}</div>
                                {r.description && (
                                    <div style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)' }}>
                                        {r.description}
                                    </div>
                                )}
                            </div>
                        ),
                    },
                    {
                        title: 'Hạn sử dụng',
                        dataIndex: 'lifeDays',
                        width: 115,
                        align: 'right',
                        render: (days: number) => <strong>{days} ngày</strong>,
                    },
                    {
                        title: 'Nhiệt độ bảo quản',
                        key: 'temp',
                        width: 185,
                        render: (_, r) => (
                            <div>
                                <div style={{ fontWeight: 600 }}>{r.storageTemperature}</div>
                                <div style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)' }}>
                                    Dải kiểm tra: {r.minTemp}°C đến {r.maxTemp}°C
                                </div>
                            </div>
                        ),
                    },
                    {
                        title: 'Quy tắc tái nhập kho',
                        key: 'restock',
                        width: 240,
                        render: (_, r) => (
                            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start', gap: 4 }}>
                                <StatusPill tone={r.allowRestock ? 'success' : 'danger'} dot={false}>
                                    {r.allowRestock ? 'Cho phép tái nhập' : 'Bắt buộc tiêu hủy khi hoàn'}
                                </StatusPill>
                                <span style={{ fontSize: 'var(--text-xs)', color: 'var(--c-text-muted)' }}>
                                    {r.allowRestock
                                        ? `Tối đa ${r.maxRestockCount} lần - ${
                                              r.maxOutsideMinutesForRestock
                                                  ? `tối đa ${r.maxOutsideMinutesForRestock} phút ngoài tủ`
                                                  : 'không giới hạn phút ngoài tủ'
                                          }`
                                        : 'Không đủ điều kiện đưa lại kho sạch'}
                                </span>
                            </div>
                        ),
                    },
                    {
                        title: 'Hòa hợp miễn dịch',
                        dataIndex: 'compatibilityRuleType',
                        width: 175,
                        render: (rule: BloodComponentTypeConfig['compatibilityRuleType']) => {
                            const meta = COMPATIBILITY_RULE_META[rule]
                            return (
                                <StatusPill tone={meta?.tone ?? 'neutral'} dot={false}>
                                    {meta?.label ?? rule}
                                </StatusPill>
                            )
                        },
                    },
                    {
                        title: '',
                        key: 'actions',
                        width: 110,
                        align: 'right',
                        render: (_, r) => (
                            <Can do={PERMISSIONS.INVENTORY_EDIT}>
                                <Button size="small" icon={<EditOutlined />} onClick={() => setEditing(r)}>
                                    Chỉnh sửa
                                </Button>
                            </Can>
                        ),
                    },
                ]}
            />

            <ComponentTypeFormModal
                open={Boolean(editing)}
                item={editing}
                onClose={() => setEditing(null)}
            />
        </SectionCard>
    )
}
