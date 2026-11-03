import { useEffect, useRef } from 'react'
import JsBarcode from 'jsbarcode'

interface Props {
    value: string
    height?: number
}

export default function BarcodeSvg({ value, height = 56 }: Props) {
    const ref = useRef<SVGSVGElement>(null)

    useEffect(() => {
        const svg = ref.current
        if (!svg) return
        try {
            JsBarcode(svg, value, {
                format: 'CODE128',
                displayValue: false,
                margin: 8,
                width: 2,
                height,
                lineColor: '#000000',
                background: '#ffffff',
            })
            const w = parseFloat(svg.getAttribute('width') ?? '')
            const h = parseFloat(svg.getAttribute('height') ?? '')
            if (w && h) {
                svg.setAttribute('viewBox', `0 0 ${w} ${h}`)
                svg.removeAttribute('width')
                svg.removeAttribute('height')
                svg.setAttribute('preserveAspectRatio', 'none')
            }
        } catch {
            svg.replaceChildren()
        }
    }, [value, height])

    return <svg ref={ref} className="ss-barcode" role="img" aria-label={`Mã vạch ${value}`} />
}