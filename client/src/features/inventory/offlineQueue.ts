import { useSyncExternalStore } from 'react'
import type { OfflineTransition, TransitionRequest } from './stockTypes'

/** Hàng đợi lệnh chuyển trạng thái khi mất mạng (localStorage). */
const KEY = 'inventory.offlineTransitions.v1'
const listeners = new Set<() => void>()

const read = (): OfflineTransition[] => {
    try {
        const raw = JSON.parse(localStorage.getItem(KEY) ?? '[]')
        return Array.isArray(raw) ? raw : []
    } catch {
        return []
    }
}

let snapshot: OfflineTransition[] = read()
const emit = () => listeners.forEach((l) => l())
const write = (items: OfflineTransition[]) => {
    snapshot = items
    try {
        localStorage.setItem(KEY, JSON.stringify(items))
    } catch {
        /* storage đầy / bị chặn: vẫn giữ trong bộ nhớ */
    }
    emit()
}

if (typeof window !== 'undefined') {
    window.addEventListener('storage', (e) => {
        if (e.key === KEY) {
            snapshot = read()
            emit()
        }
    })
}

export const addOffline = (req: TransitionRequest) =>
    write([
        ...snapshot,
        { ...req, id: `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`, createdAt: new Date().toISOString() },
    ])

export const removeOffline = (id: string) => write(snapshot.filter((i) => i.id !== id))

export const markOfflineError = (id: string, error?: string) =>
    write(snapshot.map((i) => (i.id === id ? { ...i, error } : i)))

export const toRequest = ({ barcode, targetStatus, storageLocationId, notes }: OfflineTransition): TransitionRequest => ({
    barcode, targetStatus, storageLocationId, notes,
})

export const useOfflineQueue = () =>
    useSyncExternalStore(
        (cb) => {
            listeners.add(cb)
            return () => listeners.delete(cb)
        },
        () => snapshot,
    )