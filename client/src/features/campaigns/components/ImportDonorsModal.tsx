import { useState } from 'react'
import {
    Alert,
    App,
    Button,
    Card,
    Col,
    Modal,
    Row,
    Space,
    Statistic,
    Table,
    Tag,
    Upload,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import type { UploadFile } from 'antd/es/upload/interface'
import {
    CheckCircleOutlined,
    CloseCircleOutlined,
    DownloadOutlined,
    FileExcelOutlined,
    InboxOutlined,
    ReloadOutlined,
    UploadOutlined,
    WarningOutlined,
} from '@ant-design/icons'
import { campaignsApi } from '../campaignsApi'
import { useImportDonors } from '../hooks'
import type { DonorImportResult, DonorImportRowError } from '../types'
import { parseApiError } from '@/services/apiError'

interface Props {
    campaignId?: number | null
    campaignName?: string
    open: boolean
    onClose: () => void
}

export default function ImportDonorsModal({
    campaignId,
    campaignName,
    open,
    onClose,
}: Props) {
    const { message } = App.useApp()
    const [selectedFile, setSelectedFile] = useState<File | null>(null)
    const [fileList, setFileList] = useState<UploadFile[]>([])
    const [isDownloading, setIsDownloading] = useState(false)
    const [result, setResult] = useState<DonorImportResult | null>(null)

    const importMutation = useImportDonors()

    const handleDownloadTemplate = async () => {
        if (!campaignId) return
        setIsDownloading(true)
        try {
            await campaignsApi.downloadTemplate(campaignId)
            message.success('Đã tải file Excel mẫu thành công.')
        } catch (e) {
            message.error(parseApiError(e).message)
        } finally {
            setIsDownloading(false)
        }
    }

    const handleImport = async () => {
        if (!campaignId || !selectedFile) return
        try {
            const res = await importMutation.mutateAsync({
                campaignId,
                file: selectedFile,
            })
            setResult(res)
            if (res.errorsCount === 0) {
                message.success(`Nhập thành công toàn bộ ${res.successCount} người hiến máu!`)
            } else if (res.successCount > 0) {
                message.warning(`Đã nhập ${res.successCount} người hiến, có ${res.errorsCount} dòng bị lỗi.`)
            } else {
                message.error('Không có người hiến nào được nhập thành công.')
            }
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    const handleReset = () => {
        setSelectedFile(null)
        setFileList([])
        setResult(null)
    }

    const handleModalClose = () => {
        handleReset()
        onClose()
    }

    const errorColumns: ColumnsType<DonorImportRowError> = [
        {
            title: 'STT',
            dataIndex: 'stt',
            key: 'stt',
            align: 'center',
            width: 80,
            render: (val, record) => (
                <span style={{ fontWeight: 600 }}>
                    {val ?? (record.rowNumber ? Math.max(1, record.rowNumber - 4) : 1)}
                </span>
            ),
        },
        {
            title: 'Họ và tên',
            dataIndex: 'fullName',
            key: 'fullName',
            width: 170,
            render: (val) => val || <span style={{ color: 'var(--c-text-muted, #5f7367)' }}>N/A</span>,
        },
        {
            title: 'Số CCCD',
            dataIndex: 'citizenId',
            key: 'citizenId',
            width: 140,
            render: (val) => (
                <span style={{ fontFamily: 'monospace' }}>
                    {val || 'N/A'}
                </span>
            ),
        },
        {
            title: 'Chi tiết lỗi',
            dataIndex: 'errorMessage',
            key: 'errorMessage',
            render: (text) => (
                <span style={{ color: '#b91c1c', fontWeight: 500 }}>
                    {text}
                </span>
            ),
        },
    ]

    return (
        <Modal
            title={
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <FileExcelOutlined style={{ color: '#1a7534', fontSize: 20 }} />
                    <span>Nhập danh sách người hiến máu từ Excel</span>
                    {campaignId && <Tag color="green">Đợt #{campaignId}</Tag>}
                </div>
            }
            open={open}
            onCancel={handleModalClose}
            width={780}
            footer={
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    {result ? (
                        <Button icon={<ReloadOutlined />} onClick={handleReset}>
                            Nhập file khác
                        </Button>
                    ) : (
                        <div />
                    )}
                    <Button onClick={handleModalClose}>Đóng</Button>
                </div>
            }
            destroyOnHidden
        >
            {campaignName && (
                <div style={{ marginBottom: 16, color: 'var(--c-text-muted, #5f7367)', fontSize: 13 }}>
                    Chiến dịch: <b>{campaignName}</b>
                </div>
            )}

            {!result ? (
                <Space direction="vertical" style={{ width: '100%' }} size="middle">
                    <Card size="small" style={{ background: '#f6ffed', borderColor: '#b7eb8f' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
                            <div>
                                <div style={{ fontWeight: 600, color: '#135200', marginBottom: 2 }}>
                                    Bước 1: Tải file Excel mẫu chuẩn
                                </div>
                                <div style={{ fontSize: 12, color: 'var(--c-text-muted, #5f7367)' }}>
                                    File mẫu được cấu hình riêng cho đợt hiến #{campaignId}, bao gồm các khung giờ và 9 câu hỏi khảo sát.
                                </div>
                            </div>
                            <Button
                                icon={<DownloadOutlined />}
                                onClick={handleDownloadTemplate}
                                loading={isDownloading}
                            >
                                Tải file mẫu (.xlsx)
                            </Button>
                        </div>
                    </Card>

                    <div>
                        <div style={{ fontWeight: 600, marginBottom: 8 }}>
                            Bước 2: Chọn hoặc kéo thả file Excel danh sách đã điền
                        </div>
                        <Upload.Dragger
                            accept=".xlsx"
                            maxCount={1}
                            fileList={fileList}
                            beforeUpload={(file) => {
                                setSelectedFile(file as File)
                                setFileList([file])
                                return false
                            }}
                            onRemove={() => {
                                setSelectedFile(null)
                                setFileList([])
                            }}
                            style={{ padding: '24px 0', background: '#fafafa' }}
                        >
                            <p className="ant-upload-drag-icon">
                                <InboxOutlined style={{ color: '#1a7534', fontSize: 44 }} />
                            </p>
                            <p className="ant-upload-text" style={{ fontWeight: 600 }}>
                                Nhấp hoặc kéo thả file Excel (.xlsx) vào khu vực này
                            </p>
                            <p className="ant-upload-hint" style={{ fontSize: 12, color: 'var(--c-text-muted, #5f7367)' }}>
                                Hệ thống sẽ tự động đọc dữ liệu, đối soát trùng lặp CCCD, tạo lịch hẹn và nạp kết quả bảng hỏi online.
                            </p>
                        </Upload.Dragger>
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 12 }}>
                        <Button
                            type="primary"
                            icon={<UploadOutlined />}
                            loading={importMutation.isPending}
                            disabled={!selectedFile || importMutation.isPending}
                            onClick={handleImport}
                        >
                            Tiến hành nhập dữ liệu
                        </Button>
                    </div>
                </Space>
            ) : (
                <Space direction="vertical" style={{ width: '100%' }} size="middle">
                    {result.errorsCount === 0 ? (
                        <Alert
                            type="success"
                            showIcon
                            icon={<CheckCircleOutlined />}
                            message="Nhập danh sách người hiến thành công!"
                            description={`Đã xử lý trọn vẹn ${result.successCount} người hiến máu mà không phát sinh lỗi nào.`}
                        />
                    ) : result.successCount > 0 ? (
                        <Alert
                            type="warning"
                            showIcon
                            icon={<WarningOutlined />}
                            message={`Đã nhập thành công ${result.successCount} / ${result.totalRows} người hiến`}
                            description={`Phát hiện ${result.errorsCount} dòng bị lỗi định dạng hoặc trùng lặp. Vui lòng kiểm tra chi tiết bảng bên dưới.`}
                        />
                    ) : (
                        <Alert
                            type="error"
                            showIcon
                            icon={<CloseCircleOutlined />}
                            message="Không thể nhập danh sách người hiến máu"
                            description={`Toàn bộ ${result.errorsCount} dòng trong file đều không hợp lệ. Vui lòng kiểm tra bảng lỗi bên dưới để điều chỉnh.`}
                        />
                    )}

                    <Card size="small" style={{ background: '#f8fafc' }}>
                        <Row gutter={[16, 16]}>
                            <Col span={6}>
                                <Statistic
                                    title="Tổng số dòng"
                                    value={result.totalRows}
                                />
                            </Col>
                            <Col span={6}>
                                <Statistic
                                    title="Thành công"
                                    value={result.successCount}
                                    valueStyle={{ color: '#1a7534' }}
                                />
                            </Col>
                            <Col span={6}>
                                <Statistic
                                    title="Người hiến mới"
                                    value={result.createdDonorsCount}
                                    suffix={`/ ${result.reusedDonorsCount} cũ`}
                                />
                            </Col>
                            <Col span={6}>
                                <Statistic
                                    title="Bảng hỏi online"
                                    value={result.surveysIncludedCount}
                                    valueStyle={{ color: '#0369a1' }}
                                />
                            </Col>
                        </Row>
                    </Card>

                    {result.errors && result.errors.length > 0 && (
                        <div>
                            <div style={{ fontWeight: 600, color: '#b91c1c', marginBottom: 8, display: 'flex', alignItems: 'center', gap: 6 }}>
                                <CloseCircleOutlined />
                                Danh sách các dòng bị lỗi ({result.errors.length} dòng):
                            </div>
                            <Table
                                rowKey="rowNumber"
                                dataSource={result.errors}
                                columns={errorColumns}
                                pagination={{ pageSize: 5 }}
                                size="small"
                                bordered
                            />
                        </div>
                    )}
                </Space>
            )}
        </Modal>
    )
}
