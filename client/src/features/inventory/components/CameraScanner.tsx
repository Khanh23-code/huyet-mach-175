import { useEffect, useId, useRef } from 'react'
import type { Html5Qrcode } from 'html5-qrcode'

interface Props {
    active: boolean
    onDetected: (code: string) => void
    onError: (message: string) => void
}

/** Quét barcode/QR bằng camera (html5-qrcode, tải lười). Camera cần HTTPS hoặc localhost. */
export default function CameraScanner({ active, onDetected, onError }: Props) {
    const elId = `cam-${useId().replace(/[^a-zA-Z0-9]/g, '')}`
    const cb = useRef({ onDetected, onError })
    cb.current = { onDetected, onError }

    useEffect(() => {
        if (!active) return
        let cancelled = false
        let scanner: Html5Qrcode | null = null
        let lastCode = ''
        let lastAt = 0

        ;(async () => {
            try {
                const { Html5Qrcode: Scanner, Html5QrcodeSupportedFormats: F } = await import('html5-qrcode')
                if (cancelled) return
                scanner = new Scanner(elId, {
                    verbose: false,
                    formatsToSupport: [F.CODE_128, F.CODE_39, F.QR_CODE],
                })
                await scanner.start(
                    { facingMode: 'environment' },
                    { fps: 10, qrbox: { width: 320, height: 140 } },
                    (text) => {
                        const now = Date.now()
                        if (text === lastCode && now - lastAt < 2500) return // chống đọc trùng liên tiếp
                        lastCode = text
                        lastAt = now
                        cb.current.onDetected(text)
                    },
                    () => {},
                )
                if (cancelled) await scanner.stop()
            } catch (e) {
                if (!cancelled) {
                    const raw = String((e as Error)?.message ?? e)
                    cb.current.onError(
                        /permission|denied|NotAllowed/i.test(raw)
                            ? 'Không truy cập được camera. Hãy cấp quyền camera cho trình duyệt (cần HTTPS).'
                            : /NotFound|no camera/i.test(raw)
                                ? 'Không tìm thấy camera trên thiết bị này.'
                                : `Không bật được camera: ${raw}`,
                    )
                }
            }
        })()

        return () => {
            cancelled = true
            const s = scanner
            if (s) s.stop().then(() => s.clear()).catch(() => {})
        }
    }, [active, elId])

    return <div id={elId} className="scan-cam" />
}