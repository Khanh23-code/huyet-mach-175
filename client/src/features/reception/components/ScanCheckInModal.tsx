import { useRef, useState } from 'react'
import { Alert, Button, Input, Modal, type InputRef } from 'antd'
import { QrcodeOutlined } from '@ant-design/icons'
import { parseApiError } from '@/services/apiError'
import { useCheckIn } from '../hooks'

interface Props {
    open: boolean
    campaignId: number
    onClose: () => void
}

interface ScanResult {
    key: number
    ok: boolean
    text: string
}

/**
 * Máy quét USB (keyboard-wedge) gõ mã rồi gửi Enter vào ô đang focus.
 * Quét liên tục: sau mỗi lần quét ô nhập được xóa và focus lại.
 */
export default function ScanCheckInModal({ open, campaignId, onClose }: Props) {
    const inputRef = useRef<InputRef>(null)
    const [token, setToken] = useState('')
    const [results, setResults] = useState<ScanResult[]>([])
    const checkIn = useCheckIn()

    const push = (ok: boolean, text: string) =>
        setResults((prev) => [{ key: Date.now(), ok, text }, ...prev].slice(0, 5))

    const submit = async () => {
        const qrCodeToken = token.trim()
        if (!qrCodeToken || checkIn.isPending) return
        setToken('')

        try {
            const r = await checkIn.mutateAsync({ qrCodeToken, campaignId })
            push(true, `${r.donorName}: check-in thành công${r.timeSlot ? ` (khung ${r.timeSlot})` : ''}`)
        } catch (e) {
            push(false, parseApiError(e).message)
        } finally {
            inputRef.current?.focus()
        }
    }

    return (
        <Modal
            title="Quét mã QR check-in"
            open={open}
            onCancel={onClose}
            width={560}
            destroyOnHidden
            footer={<Button onClick={onClose}>Đóng</Button>}
            afterOpenChange={(opened) => {
                if (opened) inputRef.current?.focus()
                else setResults([])
            }}
        >
            <p style={{ color: 'var(--c-text-muted)', marginTop: 0 }}>
                Đưa mã trên phiếu hẹn hoặc ứng dụng của người hiến vào máy quét. Có thể nhập mã bằng tay rồi nhấn Enter.
            </p>

            <Input
                ref={inputRef}
                size="large"
                prefix={<QrcodeOutlined />}
                placeholder="Chờ quét mã..."
                value={token}
                onChange={(e) => setToken(e.target.value)}
                onPressEnter={submit}
                autoComplete="off"
            />

            <div style={{ display: 'grid', gap: 8, marginTop: 16 }}>
                {results.map((r) => (
                    <Alert key={r.key} type={r.ok ? 'success' : 'error'} showIcon title={r.text} />
                ))}
            </div>
        </Modal>
    )
}