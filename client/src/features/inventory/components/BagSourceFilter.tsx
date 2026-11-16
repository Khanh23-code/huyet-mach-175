import { Segmented } from 'antd'
import { SOURCE_FILTER_OPTIONS } from '../bagSource'
import type { SourceFilterValue } from '../bagSource'

interface Props {
    value: SourceFilterValue
    onChange: (v: SourceFilterValue) => void
}

export default function BagSourceFilter({ value, onChange }: Props) {
    return (
        <Segmented
            block
            size="small"
            options={SOURCE_FILTER_OPTIONS}
            value={value}
            onChange={(v) => onChange(v as SourceFilterValue)}
        />
    )
}