import type { AboType, IndicatorKey, RhValue } from './labTestsTypes'

export const INDICATORS: { key: IndicatorKey; label: string; hint: string }[] = [
    { key: 'hivResult', label: 'HIV', hint: 'Kháng thể / kháng nguyên HIV' },
    { key: 'hbvResult', label: 'HBV', hint: 'Viêm gan B (HBsAg)' },
    { key: 'hcvResult', label: 'HCV', hint: 'Viêm gan C' },
    { key: 'syphilisResult', label: 'Giang mai', hint: 'Treponema pallidum' },
    { key: 'irregularAntibody', label: 'Kháng thể bất thường', hint: 'Sàng lọc kháng thể bất thường' },
]

export const ABO_OPTIONS: { value: AboType; label: string }[] = [
    { value: 'A', label: 'A' },
    { value: 'B', label: 'B' },
    { value: 'AB', label: 'AB' },
    { value: 'O', label: 'O' },
]

export const RH_OPTIONS: { value: RhValue; label: string }[] = [
    { value: 'POSITIVE', label: 'Rh dương (+)' },
    { value: 'NEGATIVE', label: 'Rh âm (-)' },
]

export const formatGroup = (abo: AboType, rh: RhValue) => `${abo}${rh === 'POSITIVE' ? '+' : '-'}`