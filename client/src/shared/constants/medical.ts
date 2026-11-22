import type { AboGroup, TargetVolume } from '../types/medical'

/** 8 nhóm máu tiêu chuẩn */
export const BLOOD_GROUPS: readonly string[] = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'] as const

/** Thứ tự hiển thị 8 nhóm máu theo thiết kế phân hệ kho */
export const BLOOD_GROUPS_DISPLAY: readonly string[] = ['O+', 'A+', 'B+', 'AB+', 'O-', 'A-', 'B-', 'AB-'] as const

export const BLOOD_GROUP_OPTIONS = BLOOD_GROUPS.map((g) => ({ value: g, label: g }))

export const TARGET_VOLUME_OPTIONS: { value: TargetVolume; label: string; volumeMl: number }[] = [
    { value: 'VOL_250', label: '250 ml', volumeMl: 250 },
    { value: 'VOL_350', label: '350 ml', volumeMl: 350 },
    { value: 'VOL_450', label: '450 ml', volumeMl: 450 },
]

export const VOLUME_CHOICES = [250, 350, 450] as const

export const EXTERNAL_VOLUME_OPTIONS = [250, 350, 450].map((v) => ({ value: v, label: `${v} ml` }))

export const ABO_OPTIONS: { value: AboGroup; label: string }[] = [
    { value: 'A', label: 'A' },
    { value: 'B', label: 'B' },
    { value: 'AB', label: 'AB' },
    { value: 'O', label: 'O' },
]
