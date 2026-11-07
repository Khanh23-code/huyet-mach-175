import { useMemo, useState } from 'react'
import { Alert, Breadcrumb, Button, DatePicker, Empty, Form, Input, InputNumber, Select, message } from 'antd'
import { BarcodeOutlined, PlusOutlined, PrinterOutlined } from '@ant-design/icons'
import dayjs, { type Dayjs } from 'dayjs'
import { parseApiError } from '@/services/apiError'
import { useRegisterExternal, useStorageLocations } from '../hooks'
import {
    BLOOD_GROUP_OPTIONS,
    EXTERNAL_VOLUME_OPTIONS,
    locationOptions,
    splitBloodGroup,
    storageHint,
} from '../format'
import BagLabel, { type LabelData } from '../components/BagLabel'
import LabelsPrintModal from '../components/LabelsPrintModal'
import type { RegisterExternalResponse } from '../types'
import './InventoryPages.css'

interface FormValues {
    externalUnitName: string
    externalCode: string
    bloodGroup: string
    volumeMl: number
    collectedAt: Dayjs
    receivedTemperature: number
    storageLocationId: number
}

const toLabel = (r: RegisterExternalResponse): LabelData => ({
    barcode: r.barcode,
    bloodGroup: r.bloodGroup,
    volumeMl: r.volumeMl,
    collectedAt: r.collectedAt,
    expiredAt: r.expiredAt,
    componentName: r.componentTypeName,
})

export default function ExternalBagPage() {
    const [form] = Form.useForm<FormValues>()
    const [result, setResult] = useState<RegisterExternalResponse | null>(null)
    const [printOpen, setPrintOpen] = useState(false)

    const register = useRegisterExternal()
    const { data: locData, isLoading: locLoading } = useStorageLocations()
    const locations = useMemo(() => locationOptions(locData?.items ?? [], 'WB'), [locData])

    const onFinish = async (v: FormValues) => {
        const { bloodType, rhFactor } = splitBloodGroup(v.bloodGroup)
        try {
            const r = await register.mutateAsync({
                externalUnitName: v.externalUnitName.trim(),
                externalCode: v.externalCode.trim(),
                bloodType,
                rhFactor,
                volumeMl: v.volumeMl,
                collectedAt: v.collectedAt.toISOString(),
                receivedTemperature: v.receivedTemperature,
                storageLocationId: v.storageLocationId,
            })
            setResult(r)
            message.success(`Đã khởi tạo mã ${r.barcode}`)
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    const reset = () => {
        setResult(null)
        form.resetFields()
    }

    return (
        <div className="inv">
            <Breadcrumb items={[{ title: 'Túi máu & Kho' }, { title: 'Khởi tạo mã định danh' }]} />
            <div className="inv-title">
                <h1>Khởi tạo mã định danh túi máu</h1>
                <p>Tạo mã định danh cho túi máu toàn phần tiếp nhận từ đơn vị ngoài. Chế phẩm được tạo ở màn Điều chế chế phẩm.</p>
            </div>

            <div className="inv-grid">
                <section className="inv-card">
                    <header className="inv-card__head">
                        <h2>Thông tin túi máu</h2>
                        <p>Điền đầy đủ thông tin trước khi khởi tạo mã.</p>
                    </header>
                    <div className="inv-card__body">
                        <Form form={form} layout="vertical" size="large" requiredMark onFinish={onFinish} disabled={!!result}>
                            <div className="inv-form-row">
                                <Form.Item
                                    name="externalUnitName"
                                    label="Đơn vị cung cấp"
                                    rules={[
                                        { required: true, whitespace: true, message: 'Vui lòng nhập đơn vị cung cấp' },
                                        { max: 150, message: 'Tối đa 150 ký tự' },
                                    ]}
                                >
                                    <Input placeholder="VD: Viện Huyết học - Truyền máu TW" autoComplete="off" />
                                </Form.Item>
                                <Form.Item
                                    name="externalCode"
                                    label="Mã túi của đơn vị ngoài"
                                    rules={[
                                        { required: true, whitespace: true, message: 'Vui lòng nhập hoặc quét mã túi gốc' },
                                        { max: 100, message: 'Tối đa 100 ký tự' },
                                    ]}
                                >
                                    <Input prefix={<BarcodeOutlined />} placeholder="Quét hoặc nhập mã trên túi" autoComplete="off" />
                                </Form.Item>
                            </div>

                            <div className="inv-form-row">
                                <Form.Item name="bloodGroup" label="Nhóm máu (dự kiến)" rules={[{ required: true, message: 'Vui lòng chọn nhóm máu' }]}>
                                    <Select placeholder="Chọn nhóm máu..." options={BLOOD_GROUP_OPTIONS} />
                                </Form.Item>
                                <Form.Item name="volumeMl" label="Thể tích (ml)" rules={[{ required: true, message: 'Vui lòng chọn thể tích' }]}>
                                    <Select placeholder="Chọn thể tích..." options={EXTERNAL_VOLUME_OPTIONS} />
                                </Form.Item>
                            </div>

                            <Form.Item
                                name="collectedAt"
                                label="Thời gian thu thập"
                                rules={[
                                    { required: true, message: 'Vui lòng chọn thời gian thu thập' },
                                    {
                                        validator: (_, value: Dayjs | undefined) =>
                                            !value || !value.isAfter(dayjs())
                                                ? Promise.resolve()
                                                : Promise.reject(new Error('Thời gian thu thập không được ở tương lai')),
                                    },
                                ]}
                            >
                                <DatePicker
                                    style={{ width: '100%' }}
                                    showTime={{ format: 'HH:mm' }}
                                    format="DD/MM/YYYY HH:mm"
                                    placeholder="Chọn ngày giờ thu thập"
                                    disabledDate={(d) => d.isAfter(dayjs(), 'day')}
                                />
                            </Form.Item>

                            <div className="inv-form-row">
                                <Form.Item
                                    name="receivedTemperature"
                                    label="Nhiệt độ tiếp nhận (°C)"
                                    extra="Cho phép 2.0 đến 10.0°C"
                                    rules={[{ required: true, message: 'Vui lòng nhập nhiệt độ tiếp nhận' }]}
                                >
                                    <InputNumber style={{ width: '100%' }} min={2} max={10} step={0.1} precision={1} placeholder="VD: 4.5" />
                                </Form.Item>
                                <Form.Item
                                    name="storageLocationId"
                                    label="Vị trí lưu trữ"
                                    extra={`Chỉ hiện tủ phù hợp máu toàn phần (${storageHint('WB')})`}
                                    rules={[{ required: true, message: 'Vui lòng chọn vị trí lưu trữ' }]}
                                >
                                    <Select
                                        placeholder="Chọn tủ / ngăn..."
                                        loading={locLoading}
                                        options={locations}
                                        notFoundContent="Không có tủ phù hợp đang trống"
                                    />
                                </Form.Item>
                            </div>

                            <Button type="primary" htmlType="submit" block icon={<BarcodeOutlined />} loading={register.isPending}>
                                Khởi tạo mã định danh
                            </Button>
                        </Form>
                    </div>
                </section>

                <section className="inv-card">
                    <header className="inv-card__head">
                        <h2>Mã định danh &amp; Barcode</h2>
                        <p>Mã EXT-… sẽ hiện sau khi khởi tạo.</p>
                    </header>
                    <div className="inv-card__body">
                        {!result ? (
                            <Empty
                                image={<BarcodeOutlined style={{ fontSize: 64, color: 'var(--c-text-muted)' }} />}
                                description='Điền thông tin và nhấn "Khởi tạo" để tạo mã và barcode.'
                            />
                        ) : (
                            <div className="inv-result">
                                <Alert type="success" showIcon title={result.message} />
                                <BagLabel bag={toLabel(result)} />
                                <div className="inv-actions">
                                    <Button type="primary" icon={<PrinterOutlined />} onClick={() => setPrintOpen(true)}>
                                        In tem nhãn
                                    </Button>
                                    <Button icon={<PlusOutlined />} onClick={reset}>
                                        Nhập túi tiếp theo
                                    </Button>
                                </div>
                                <Alert
                                    type="info"
                                    showIcon
                                    title={`Túi đã được lưu tại ${result.storageLocationName} và nằm trong hàng chờ xét nghiệm. Nhóm máu sẽ được khẳng định lại khi xét nghiệm.`}
                                />
                            </div>
                        )}
                    </div>
                </section>
            </div>

            {result && <LabelsPrintModal open={printOpen} labels={[toLabel(result)]} onClose={() => setPrintOpen(false)} />}
        </div>
    )
}