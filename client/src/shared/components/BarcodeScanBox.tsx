import { useId, useRef, useState } from 'react'
import { Alert, Button, Input } from 'antd'
import { BarcodeOutlined, CameraOutlined, PictureOutlined, SearchOutlined } from '@ant-design/icons'
import CameraScanner from '@/features/inventory/components/CameraScanner'
import './BarcodeScanBox.css'

interface BarcodeScanBoxProps {
    title: string
    description: string
    placeholder?: string
    submitLabel?: string
    loading?: boolean
    errorMessage?: string | null
    onClearError?: () => void
    onScan: (barcode: string) => void | Promise<void>
    enableCamera?: boolean
}

export default function BarcodeScanBox({
    title,
    description,
    placeholder = 'Nhập mã barcode túi máu...',
    submitLabel = 'Quét & kiểm tra',
    loading = false,
    errorMessage,
    onClearError,
    onScan,
    enableCamera = true,
}: BarcodeScanBoxProps) {
    const [code, setCode] = useState('')
    const [cameraOn, setCameraOn] = useState(false)
    const [scanningFile, setScanningFile] = useState(false)
    const [cameraError, setCameraError] = useState<string | null>(null)
    const fileInputRef = useRef<HTMLInputElement>(null)
    const tempScannerId = `bscan-file-${useId().replace(/[^a-zA-Z0-9]/g, '')}`

    const submit = async (rawValue: string) => {
        const trimmed = rawValue.trim()
        if (!trimmed || loading) return
        setCameraError(null)
        await onScan(trimmed)
    }

    const handleImageUpload = async (file: File) => {
        setScanningFile(true)
        setCameraError(null)
        onClearError?.()

        try {
            const { Html5Qrcode, Html5QrcodeSupportedFormats: F } = await import('html5-qrcode')
            const scanner = new Html5Qrcode(tempScannerId, {
                verbose: false,
                formatsToSupport: [F.CODE_128, F.CODE_39, F.QR_CODE],
            })
            try {
                const decoded = await scanner.scanFile(file, false)
                const cleaned = decoded.trim()
                setCode(cleaned)
                await submit(cleaned)
            } finally {
                try {
                    scanner.clear()
                } catch {
                    // ignore cleanup error
                }
            }
        } catch {
            setCameraError('Không nhận diện được mã QR / Barcode từ ảnh đã chọn. Vui lòng thử ảnh rõ nét hơn hoặc nhập mã trực tiếp.')
        } finally {
            setScanningFile(false)
            if (fileInputRef.current) fileInputRef.current.value = ''
        }
    }

    return (
        <div className="bscan-box">
            <div id={tempScannerId} style={{ display: 'none' }} aria-hidden="true" />
            <input
                ref={fileInputRef}
                type="file"
                accept="image/*"
                style={{ display: 'none' }}
                onChange={(e) => {
                    const file = e.target.files?.[0]
                    if (file) void handleImageUpload(file)
                }}
            />

            {enableCamera && cameraOn ? (
                <div className="bscan-stage">
                    <CameraScanner
                        active={cameraOn}
                        onDetected={(detected) => {
                            setCode(detected)
                            setCameraOn(false)
                            void submit(detected)
                        }}
                        onError={(msg) => {
                            setCameraError(msg)
                            setCameraOn(false)
                        }}
                    />
                    <span className="bscan-live">
                        <i className="on" /> CAMERA LIVE
                    </span>
                    <Button
                        size="small"
                        className="bscan-cam-btn"
                        icon={<CameraOutlined />}
                        onClick={() => setCameraOn(false)}
                    >
                        Tắt camera
                    </Button>
                </div>
            ) : (
                <div className="bscan-dropzone">
                    {enableCamera && (
                        <div className="bscan-dropzone__cam">
                            <Button
                                size="small"
                                icon={<PictureOutlined />}
                                loading={scanningFile}
                                disabled={loading}
                                onClick={() => fileInputRef.current?.click()}
                            >
                                Quét từ ảnh
                            </Button>
                            <Button
                                size="small"
                                icon={<CameraOutlined />}
                                disabled={loading || scanningFile}
                                onClick={() => {
                                    setCameraError(null)
                                    setCameraOn(true)
                                }}
                            >
                                Bật camera
                            </Button>
                        </div>
                    )}

                    <div className="bscan-dropzone__icon">
                        <BarcodeOutlined />
                    </div>
                    <h3 className="bscan-dropzone__title">{title}</h3>
                    <p className="bscan-dropzone__desc">{description}</p>

                    <div className="bscan-input-row">
                        <Input
                            size="large"
                            autoFocus
                            autoComplete="off"
                            allowClear
                            prefix={<SearchOutlined style={{ color: '#94a3b8' }} />}
                            placeholder={placeholder}
                            value={code}
                            disabled={loading || scanningFile}
                            onChange={(e) => {
                                setCode(e.target.value)
                                if (errorMessage) onClearError?.()
                            }}
                            onPressEnter={() => void submit(code)}
                        />
                        <Button
                            type="primary"
                            size="large"
                            loading={loading || scanningFile}
                            disabled={!code.trim()}
                            className="bscan-submit-btn"
                            onClick={() => void submit(code)}
                        >
                            {submitLabel}
                        </Button>
                    </div>
                </div>
            )}

            {cameraError && (
                <Alert
                    type="warning"
                    showIcon
                    closable
                    onClose={() => setCameraError(null)}
                    message={cameraError}
                />
            )}

            {errorMessage && (
                <Alert
                    type="error"
                    showIcon
                    closable={Boolean(onClearError)}
                    onClose={onClearError}
                    message="Không thể tiếp tục với mã túi máu này"
                    description={errorMessage}
                />
            )}
        </div>
    )
}
