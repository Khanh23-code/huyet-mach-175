import { useCallback, useEffect, useState, useSyncExternalStore } from 'react'
import { Alert, App, Badge, Breadcrumb, Button, Input, Select, Tabs } from 'antd'
import { BarcodeOutlined, CameraOutlined, SearchOutlined } from '@ant-design/icons'
import { usePermission } from '@/features/auth/usePermission'
import { PERMISSIONS } from '@/config/permissions'
import { parseApiError } from '@/services/apiError'
import { stockApi } from '../stockApi'
import { useOfflineSync } from '../stockHooks'
import { addOffline } from '../offlineQueue'
import { MANUAL_STATUSES, isNetworkError, statusMeta } from '../stockFormat'
import type { BagScan, BagStatus } from '../stockTypes'
import CameraScanner from '../components/CameraScanner'
import BagScanResult from '../components/BagScanResult'
import RecentLogsPanel from '../components/RecentLogsPanel'
import OfflineQueuePanel from '../components/OfflineQueuePanel'
import './InventoryPages.css'
import './Disposal.css'
import './Stock.css'
import './BagScan.css'

const useOnline = () =>
    useSyncExternalStore(
        (cb) => {
            window.addEventListener('online', cb)
            window.addEventListener('offline', cb)
            return () => {
                window.removeEventListener('online', cb)
                window.removeEventListener('offline', cb)
            }
        },
        () => navigator.onLine,
    )

export default function BagScanPage() {
    const { message } = App.useApp()
    const { can } = usePermission()
    const canEdit = can(PERMISSIONS.INVENTORY_EDIT)
    const online = useOnline()

    const [code, setCode] = useState('')
    const [looking, setLooking] = useState(false)
    const [result, setResult] = useState<BagScan | null>(null)
    const [cameraOn, setCameraOn] = useState(false)
    const [tab, setTab] = useState('logs')
    // Quét thất bại do mất mạng -> cho ghi nhận lệnh vào hàng đợi ngoại tuyến
    const [offlineCode, setOfflineCode] = useState<string | null>(null)
    const [offlineTarget, setOfflineTarget] = useState<BagStatus>('AVAILABLE')

    const sync = useOfflineSync()

    // Có mạng lại -> tự gửi hàng đợi
    useEffect(() => {
        if (!canEdit) return
        const onUp = () => { if (sync.items.length > 0) sync.syncAll().then((n) => n > 0 && message.success(`Đã gửi ${n} lệnh ngoại tuyến`)) }
        window.addEventListener('online', onUp)
        return () => window.removeEventListener('online', onUp)
    }, [canEdit, sync, message])

    const lookup = useCallback(async (raw: string) => {
        const barcode = raw.trim()
        if (!barcode) return
        setLooking(true)
        setOfflineCode(null)
        try {
            setResult(await stockApi.scan(barcode))
            setCode('')
        } catch (e) {
            setResult(null)
            if (isNetworkError(e)) {
                setOfflineCode(barcode)
                message.warning('Không kết nối được máy chủ.')
            } else {
                message.error(parseApiError(e).message)
            }
        } finally {
            setLooking(false)
        }
    }, [message])

    const enqueueOffline = () => {
        if (!offlineCode) return
        addOffline({ barcode: offlineCode, targetStatus: offlineTarget })
        message.success(`Đã ghi nhận "${offlineCode}" vào hàng đợi ngoại tuyến`)
        setOfflineCode(null)
        setTab('offline')
    }

    return (
        <div className="inv">
            <Breadcrumb items={[{ title: 'Túi máu & Kho' }, { title: 'Quét vòng đời' }]} />
            <div className="stk-head">
                <div className="inv-title">
                    <h1>Quét cập nhật trạng thái túi máu</h1>
                    <p>Quét định danh để truy xuất và cập nhật trạng thái túi máu theo thời gian thực.</p>
                </div>
                <span className={`scan-pill${online ? '' : ' is-off'}`}><i /> {online ? 'Máy quét đang hoạt động' : 'Mất kết nối · chế độ ngoại tuyến'}</span>
            </div>

            <section className="inv-card inv-card__body scan-card">
                <div className="scan-stage">
                    {cameraOn ? (
                        <CameraScanner
                            active={cameraOn}
                            onDetected={(c) => { setCode(c); lookup(c) }}
                            onError={(m) => { message.error(m); setCameraOn(false) }}
                        />
                    ) : (
                        <div className="scan-idle">
                            <span className="scan-corner tl" /><span className="scan-corner tr" />
                            <span className="scan-corner bl" /><span className="scan-corner br" />
                            <BarcodeOutlined className="scan-idle__icon" />
                            <b>Đưa mã Barcode / QR vào khung</b>
                            <small>Dùng máy quét USB (tự nhập vào ô bên dưới) hoặc bật camera</small>
                        </div>
                    )}
                    <span className="scan-live"><i className={cameraOn ? 'on' : ''} /> {cameraOn ? 'CAMERA LIVE' : 'CAMERA TẮT'}</span>
                    <Button className="scan-cam-btn" icon={<CameraOutlined />} onClick={() => setCameraOn((v) => !v)}>
                        {cameraOn ? 'Tắt camera' : 'Bật camera'}
                    </Button>
                </div>

                <div className="scan-manual">
                    <small>Hoặc nhập mã định danh thủ công</small>
                    <div className="scan-manual__row">
                        <Input
                            size="large" autoFocus autoComplete="off" allowClear
                            prefix={<SearchOutlined />} placeholder="Ví dụ: TM-250602-0184"
                            value={code} disabled={looking}
                            onChange={(e) => setCode(e.target.value)}
                            onPressEnter={() => lookup(code)}
                        />
                        <Button type="primary" size="large" loading={looking} disabled={!code.trim()} onClick={() => lookup(code)}>
                            Tra cứu túi máu
                        </Button>
                    </div>
                </div>
            </section>

            {offlineCode && (
                <Alert
                    type="warning" showIcon
                    title={`Mất kết nối khi tra cứu "${offlineCode}"`}
                    description={
                        canEdit ? (
                            <div className="scan-offline">
                                <span>Ghi nhận lệnh để gửi lại khi có mạng:</span>
                                <Select
                                    style={{ width: 180 }} value={offlineTarget} onChange={setOfflineTarget}
                                    options={MANUAL_STATUSES.map((s) => ({ value: s, label: statusMeta(s).label }))}
                                />
                                <Button type="primary" onClick={enqueueOffline}>Thêm vào hàng đợi</Button>
                            </div>
                        ) : 'Hãy thử lại khi có kết nối.'
                    }
                />
            )}

            {result && (
                <BagScanResult
                    key={`${result.barcode}-${result.currentStatus}`}
                    bag={result}
                    onChanged={lookup}
                    onQueued={() => { setResult(null); setTab('offline') }}
                />
            )}

            <section className="inv-card inv-card__body">
                <Tabs
                    activeKey={tab}
                    onChange={setTab}
                    items={[
                        { key: 'logs', label: 'Nhật ký gần đây', children: <RecentLogsPanel /> },
                        {
                            key: 'offline',
                            label: <Badge count={sync.items.length} size="small" offset={[8, -2]}>Hàng đợi ngoại tuyến</Badge>,
                            children: <OfflineQueuePanel sync={sync} canEdit={canEdit} />,
                        },
                    ]}
                />
            </section>
        </div>
    )
}