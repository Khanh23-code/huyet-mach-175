import type { BloodBagLite, ChildCode, ComponentCode, StorageLocation } from './types'

export const CHILD_CODES: ChildCode[] = ['PRBC', 'FFP', 'PLT']

export const COMPONENT_LABEL: Record<ComponentCode, string> = {
    WB: 'Máu toàn phần',
    PRBC: 'Khối hồng cầu',
    FFP: 'Huyết tương tươi đông lạnh',
    PLT: 'Khối tiểu cầu',
}

export const isLocationSuitable = (code: ComponentCode, temp: number) => {
    switch (code) {
        case 'WB':
        case 'PRBC':
            return temp >= 1 && temp <= 10
        case 'FFP':
            return temp <= -18
        case 'PLT':
            return temp >= 20 && temp <= 24
    }
}

export const storageHint = (code: ComponentCode) =>
    ({ WB: '1°C đến 10°C', PRBC: '1°C đến 10°C', FFP: '-18°C hoặc thấp hơn', PLT: '20°C đến 24°C' })[code]

export const locationOptions = (locations: StorageLocation[], code: ComponentCode) =>
    locations
        .filter((l) => l.isActive && !l.isFull && isLocationSuitable(code, l.targetTemperature))
        .map((l) => ({
            value: l.locationId,
            label: `${l.refrigeratorName} - ${l.shelfNumber} (${l.targetTemperature}°C)`,
        }))

export const BLOOD_GROUP_OPTIONS = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'].map((v) => ({
    value: v,
    label: v,
}))

export const splitBloodGroup = (value: string) => ({
    bloodType: value.slice(0, -1) as 'A' | 'B' | 'AB' | 'O',
    rhFactor: (value.endsWith('+') ? 'POSITIVE' : 'NEGATIVE') as 'POSITIVE' | 'NEGATIVE',
})

export const EXTERNAL_VOLUME_OPTIONS = [250, 350, 450].map((v) => ({ value: v, label: `${v} ml` }))

export const childBarcode = (parentBarcode: string, code: ChildCode) => `${parentBarcode}-${code}`

export const addDaysIso = (iso: string, days: number) =>
    new Date(new Date(iso).getTime() + days * 86_400_000).toISOString()

export const explainNotSeparable = (bag: BloodBagLite | undefined, code: string) => {
    if (!bag) return `Không tìm thấy túi máu với mã '${code}'.`
    if (bag.componentTypeCode !== 'WB') return `Túi '${bag.barcode}' không phải máu toàn phần, không thể điều chế.`
    if (bag.status === 'PROCESSED') return `Túi '${bag.barcode}' đã được điều chế trước đó.`
    return `Túi '${bag.barcode}' đang ở trạng thái ${bag.status}. Chỉ túi đã xét nghiệm đạt (TESTED_PASSED) mới được điều chế.`
}