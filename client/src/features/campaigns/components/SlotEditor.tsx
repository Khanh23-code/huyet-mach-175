import { Button, Input, InputNumber } from 'antd'
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import { Controller, type Control, type FieldErrors } from 'react-hook-form'
import type { CampaignFormValues } from '../campaignSchema'

interface Props {
    control: Control<CampaignFormValues>
    errors: FieldErrors<CampaignFormValues>
    count: number
    onAdd: () => void
    onRemove: (index: number) => void
}

export default function SlotEditor({ control, errors, count, onAdd, onRemove }: Props) {
    const rootErr = errors.slots as unknown as { root?: { message?: string }; message?: string } | undefined
    const arrayError = rootErr?.root?.message ?? rootErr?.message

    return (
        <div className="ds-field ds-field--full">
            <label>Khung giờ &amp; sức chứa mỗi slot <span className="ds-req">*</span></label>

            <div className="ds-slots">
                <div className="ds-slots__row ds-slots__head" aria-hidden>
                    <span>Giờ bắt đầu</span>
                    <span />
                    <span>Giờ kết thúc</span>
                    <span>Sức chứa (tối thiểu 1 – tối đa 1.000 người/slot)</span>
                    <span />
                </div>

                {Array.from({ length: count }).map((_, i) => {
                    const e = errors.slots?.[i]
                    return (
                        <div className="ds-slots__row" key={i}>
                            <div>
                                <Controller
                                    name={`slots.${i}.startTime`}
                                    control={control}
                                    render={({ field }) => (
                                        <Input
                                            {...field}
                                            type="time"
                                            status={e?.startTime ? 'error' : ''}
                                            aria-label="Giờ bắt đầu"
                                        />
                                    )}
                                />
                                {e?.startTime && <div className="ds-field__error">{e.startTime.message}</div>}
                            </div>
                            <span className="ds-slots__sep">–</span>
                            <div>
                                <Controller
                                    name={`slots.${i}.endTime`}
                                    control={control}
                                    render={({ field }) => (
                                        <Input
                                            {...field}
                                            type="time"
                                            status={e?.endTime ? 'error' : ''}
                                            aria-label="Giờ kết thúc"
                                        />
                                    )}
                                />
                                {e?.endTime && <div className="ds-field__error">{e.endTime.message}</div>}
                            </div>
                            <div>
                                <div className="ds-cap">
                                    <Controller
                                        name={`slots.${i}.capacity`}
                                        control={control}
                                        render={({ field }) => (
                                            <InputNumber
                                                value={field.value}
                                                onChange={(v) => field.onChange(v ?? undefined)}
                                                onBlur={field.onBlur}
                                                controls={false}
                                                precision={0}
                                                placeholder="1 – 1000"
                                                status={e?.capacity ? 'error' : ''}
                                                aria-label="Sức chứa mỗi slot"
                                                style={{ width: 140 }}
                                            />
                                        )}
                                    />
                                    <span className="ds-muted">người / slot</span>
                                </div>
                                {e?.capacity && <div className="ds-field__error">{e.capacity.message}</div>}
                            </div>
                            <Button
                                type="text"
                                danger
                                icon={<DeleteOutlined />}
                                disabled={count <= 1}
                                onClick={() => onRemove(i)}
                                aria-label="Xóa khung giờ"
                            />
                        </div>
                    )
                })}
            </div>

            {arrayError && <div className="ds-field__error">{arrayError}</div>}

            <div className="ds-slots__foot">
                <Button className="ds-dashed" type="dashed" icon={<PlusOutlined />} onClick={onAdd} disabled={count >= 12}>
                    Thêm khung giờ
                </Button>
                <span className="ds-muted ds-small">Tối đa 12 khung giờ, không được chồng lấn nhau.</span>
            </div>
        </div>
    )
}
