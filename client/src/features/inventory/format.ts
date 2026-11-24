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

const ZONE_SHORT_LABEL: Record<StorageLocation['zone'], string> = {
    QUARANTINE: 'Cách ly',
    RELEASED: 'Kho sạch',
    DISPOSAL_HOLD: 'Chờ hủy',
}

export const locationOptions = (locations: StorageLocation[], code: ComponentCode) =>
    locations
        .filter(
            (l) =>
                l.isActive &&
                !l.isFull &&
                l.zone !== 'DISPOSAL_HOLD' &&
                isLocationSuitable(code, l.targetTemperature),
        )
        .map((l) => ({
            value: l.locationId,
            label: `${l.refrigeratorName} - ${l.shelfNumber} (${l.targetTemperature}°C - ${ZONE_SHORT_LABEL[l.zone] ?? l.zone})`,
        }))

export { BLOOD_GROUP_OPTIONS, EXTERNAL_VOLUME_OPTIONS } from '@/shared/constants/medical'

export const splitBloodGroup = (value: string) => ({
    bloodType: value.slice(0, -1) as 'A' | 'B' | 'AB' | 'O',
    rhFactor: (value.endsWith('+') ? 'POSITIVE' : 'NEGATIVE') as 'POSITIVE' | 'NEGATIVE',
})

export const childBarcode = (parentBarcode: string, code: ChildCode) => `${parentBarcode}-${code}`
export { addDaysUtcIso, addDaysUtcIso as addDaysIso } from '@/shared/formatters/date'

export const explainNotSeparable = (bag: BloodBagLite | undefined, code: string) => {
    if (!bag) return `Không tìm thấy túi máu với mã '${code}'.`
    if (bag.componentTypeCode !== 'WB') return `Túi '${bag.barcode}' không phải máu toàn phần, không thể điều chế.`
    if (bag.status === 'PROCESSED') return `Túi '${bag.barcode}' đã được điều chế trước đó.`
    return `Túi '${bag.barcode}' đang ở trạng thái ${bag.status}. Chỉ túi đã xét nghiệm đạt (TESTED_PASSED) mới được điều chế.`
}