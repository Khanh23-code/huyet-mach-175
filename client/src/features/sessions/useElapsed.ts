import { useEffect, useState } from 'react'

export function useElapsed(baseMs: number | null, endMs: number | null, running: boolean) {
    const [now, setNow] = useState(() => Date.now())

    useEffect(() => {
        if (!running) return
        const t = setInterval(() => setNow(Date.now()), 1000)
        return () => clearInterval(t)
    }, [running])

    if (baseMs === null) return 0
    return Math.max(0, Math.floor(((endMs ?? now) - baseMs) / 1000))
}