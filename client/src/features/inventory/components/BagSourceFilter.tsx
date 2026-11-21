import { SOURCE_FILTER_OPTIONS } from '../bagSource'
import type { SourceFilterValue } from '../bagSource'

interface Props {
    value: SourceFilterValue
    onChange: (v: SourceFilterValue) => void
}

export default function BagSourceFilter({ value, onChange }: Props) {
    return (
        <div className="inv-source-filter" role="radiogroup" aria-label="Lọc theo nguồn túi máu">
            {SOURCE_FILTER_OPTIONS.map((opt) => {
                const active = value === opt.value
                return (
                    <button
                        key={opt.value || 'ALL'}
                        type="button"
                        role="radio"
                        aria-checked={active}
                        className={`inv-source-filter__btn${active ? ' is-active' : ''}`}
                        onClick={() => onChange(opt.value)}
                    >
                        {opt.label}
                    </button>
                )
            })}
        </div>
    )
}