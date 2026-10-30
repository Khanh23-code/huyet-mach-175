import { useState, type ReactNode } from 'react'
import { Alert, App, Button, Card, Descriptions, Form, Input, Modal, Select, Steps } from 'antd'
import { CheckCircleFilled, CloseOutlined } from '@ant-design/icons'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { applyServerErrors, parseApiError } from '@/services/apiError'
import { useCreateWalkIn } from '@/features/reception/hooks'
import { useCheckEligibility, useCreateDonor } from '../hooks'
import { walkInSchema, type WalkInForm } from '../donorSchema'
import { fmtDate, GENDER_OPTIONS } from '../format'
import type { DonorDetail, DonorEligibility } from '../types'
import { useWorkCampaignStore } from '@/features/reception/workCampaignStore'

interface Props {
    onClose: () => void
}

const FORM_FIELDS = ['citizenId', 'fullName', 'dateOfBirth', 'gender', 'phoneNumber']
const todayIso = () => new Date().toISOString().slice(0, 10)

function FieldItem({ label, error, children }: { label: string; error?: string; children: ReactNode }) {
    return (
        <Form.Item label={label} required validateStatus={error ? 'error' : undefined} help={error}>
            {children}
        </Form.Item>
    )
}

export default function WalkInRegistration({ onClose }: Props) {
    const { message } = App.useApp()
    const check = useCheckEligibility()
    const create = useCreateDonor()
    const walkIn = useCreateWalkIn()
    const campaignId = useWorkCampaignStore((s) => s.campaign?.id)

    const {
        control,
        handleSubmit,
        trigger,
        getValues,
        setError,
        formState: { errors },
    } = useForm<WalkInForm>({
        resolver: zodResolver(walkInSchema),
        defaultValues: { citizenId: '', fullName: '', dateOfBirth: '', gender: '', phoneNumber: '' },
        mode: 'onTouched',
    })

    const [step, setStep] = useState<0 | 1>(0)
    const [lookup, setLookup] = useState<DonorEligibility | null>(null)
    const [created, setCreated] = useState<DonorDetail | null>(null)
    const [queueError, setQueueError] = useState<string | null>(null)

    const isNew = lookup !== null && lookup.donorId == null

    const handleSearch = async () => {
        if (!(await trigger('citizenId'))) return
        try {
            setLookup(await check.mutateAsync(getValues('citizenId')))
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    const onValid = async (v: WalkInForm) => {
        try {
            const donor = await create.mutateAsync({
                citizenId: v.citizenId,
                fullName: v.fullName,
                dateOfBirth: v.dateOfBirth, // input type="date" -> "YYYY-MM-DD" khớp DateOnly
                gender: v.gender,
                phoneNumber: v.phoneNumber,
            })
            setCreated(donor)
            // Đưa vào hàng chờ ngay (tạo lịch hẹn walk-in -> CHECKED_IN)
            try {
                await walkIn.mutateAsync({ donorId: donor.donorId, campaignId })
                setQueueError(null)
            } catch (err) {
                setQueueError(parseApiError(err).message)
            }
        } catch (e) {
            const { message: msg, fieldErrors } = parseApiError(e)
            // 409 trùng CCCD / SĐT: gắn vào ô nếu BE chỉ rõ field, không thì báo toast
            if (!applyServerErrors(fieldErrors, setError, FORM_FIELDS)) message.error(msg)
        }
    }

    // Người hiến đã có hồ sơ -> đưa thẳng vào hàng chờ
    const handleQueueExisting = async () => {
        if (!lookup?.donorId) return
        try {
            await walkIn.mutateAsync({ donorId: lookup.donorId, campaignId })
            message.success('Đã thêm người hiến vào hàng chờ tiếp nhận')
            onClose()
        } catch (e) {
            message.error(parseApiError(e).message)
        }
    }

    const finish = () => {
        setCreated(null)
        setQueueError(null)
        onClose()
    }

    return (
        <Card
            style={{ marginBottom: 24, borderRadius: 'var(--radius-md)' }}
            title={
                <div>
                    <div style={{ color: 'var(--c-primary)', fontSize: 12, fontWeight: 700 }}>
                        ĐĂNG KÝ KHÔNG CẦN LỊCH HẸN
                    </div>
                    <div style={{ fontSize: 20, fontWeight: 500 }}>Đăng ký tại chỗ</div>
                    <div style={{ color: 'var(--c-text-muted)', fontSize: 13, fontWeight: 400 }}>
                        Tiếp nhận trực tiếp người hiến chưa sử dụng ứng dụng di động.
                    </div>
                </div>
            }
            extra={<Button type="text" icon={<CloseOutlined />} onClick={onClose} aria-label="Đóng" />}
        >
            <div style={{ maxWidth: 760, margin: '0 auto' }}>
                <Steps
                    current={step}
                    size="small"
                    style={{ marginBottom: 24 }}
                    items={[{ title: 'Xác định người hiến' }, { title: 'Kiểm tra điều kiện' }]}
                />

                {step === 0 && (
                    <Form layout="vertical" onFinish={() => handleSubmit(onValid)()}>
                        <FieldItem label="Số CCCD" error={errors.citizenId?.message}>
                            <div style={{ display: 'flex', gap: 12 }}>
                                <Controller
                                    name="citizenId"
                                    control={control}
                                    render={({ field }) => (
                                        <Input
                                            {...field}
                                            size="large"
                                            inputMode="numeric"
                                            maxLength={12}
                                            placeholder="Nhập 12 chữ số CCCD"
                                            status={errors.citizenId ? 'error' : undefined}
                                            onChange={(e) => {
                                                field.onChange(e.target.value.replace(/\D/g, ''))
                                                setLookup(null) // đổi CCCD -> phải tìm lại
                                            }}
                                            onKeyDown={(e) => {
                                                if (e.key === 'Enter') {
                                                    e.preventDefault()
                                                    void handleSearch()
                                                }
                                            }}
                                        />
                                    )}
                                />
                                <Button type="primary" size="large" loading={check.isPending} onClick={handleSearch}>
                                    Tìm kiếm
                                </Button>
                            </div>
                        </FieldItem>

                        {isNew && (
                            <>
                                <Alert
                                    type="info"
                                    showIcon
                                    style={{ marginBottom: 20 }}
                                    title="Chưa tìm thấy người hiến"
                                    description="Tạo hồ sơ tối thiểu để tiếp tục kiểm tra điều kiện hiến máu."
                                />
                                <div style={{ fontSize: 16, marginBottom: 12 }}>Tạo hồ sơ mới</div>
                                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', columnGap: 16 }}>
                                    <FieldItem label="Họ và tên" error={errors.fullName?.message}>
                                        <Controller
                                            name="fullName"
                                            control={control}
                                            render={({ field }) => (
                                                <Input {...field} size="large" placeholder="Nhập họ tên đầy đủ"
                                                       status={errors.fullName ? 'error' : undefined} />
                                            )}
                                        />
                                    </FieldItem>
                                    <FieldItem label="Ngày sinh" error={errors.dateOfBirth?.message}>
                                        <Controller
                                            name="dateOfBirth"
                                            control={control}
                                            render={({ field }) => (
                                                <Input {...field} size="large" type="date" max={todayIso()}
                                                       status={errors.dateOfBirth ? 'error' : undefined} />
                                            )}
                                        />
                                    </FieldItem>
                                    <FieldItem label="Giới tính" error={errors.gender?.message}>
                                        <Controller
                                            name="gender"
                                            control={control}
                                            render={({ field }) => (
                                                <Select
                                                    size="large"
                                                    placeholder="Chọn giới tính"
                                                    options={GENDER_OPTIONS}
                                                    value={field.value || undefined}
                                                    onChange={field.onChange}
                                                    onBlur={field.onBlur}
                                                    status={errors.gender ? 'error' : undefined}
                                                />
                                            )}
                                        />
                                    </FieldItem>
                                    <FieldItem label="Số điện thoại" error={errors.phoneNumber?.message}>
                                        <Controller
                                            name="phoneNumber"
                                            control={control}
                                            render={({ field }) => (
                                                <Input {...field} size="large" inputMode="tel" maxLength={10}
                                                       placeholder="Ví dụ: 0912345678"
                                                       status={errors.phoneNumber ? 'error' : undefined} />
                                            )}
                                        />
                                    </FieldItem>
                                </div>
                                <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
                                    <Button type="primary" size="large" htmlType="submit" loading={create.isPending}>
                                        Tạo &amp; tiếp tục
                                    </Button>
                                </div>
                            </>
                        )}

                        {lookup && !isNew && (
                            <>
                                <Alert
                                    type="success"
                                    showIcon
                                    style={{ marginBottom: 16 }}
                                    title="Đã có hồ sơ người hiến"
                                    description={lookup.message}
                                />
                                <Descriptions column={2} size="small" bordered style={{ marginBottom: 16 }}>
                                    <Descriptions.Item label="Họ và tên" span={2}><b>{lookup.fullName}</b></Descriptions.Item>
                                    <Descriptions.Item label="Ngày sinh">{fmtDate(lookup.dateOfBirth)}</Descriptions.Item>
                                    <Descriptions.Item label="Giới tính">{lookup.gender}</Descriptions.Item>
                                    <Descriptions.Item label="Số điện thoại">{lookup.phoneNumber}</Descriptions.Item>
                                    <Descriptions.Item label="Số lần hiến">{lookup.totalDonations}</Descriptions.Item>
                                </Descriptions>
                                <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
                                    <Button type="primary" size="large" onClick={() => setStep(1)}>
                                        Tiếp tục
                                    </Button>
                                </div>
                            </>
                        )}
                    </Form>
                )}

                {step === 1 && (
                    <>
                        {lookup && (
                            <Alert
                                type={lookup.isEligible && lookup.isAgeQualified ? 'success' : 'warning'}
                                showIcon
                                title={lookup.isEligible && lookup.isAgeQualified ? 'Đủ điều kiện hiến máu' : 'Chưa đủ điều kiện hiến máu'}
                                description={lookup.message}
                                style={{ marginBottom: 16 }}
                            />
                        )}
                        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                            <Button onClick={() => setStep(0)}>Quay lại</Button>
                            <Button
                                type="primary"
                                loading={walkIn.isPending}
                                disabled={!lookup || !lookup.isEligible || !lookup.isAgeQualified}
                                onClick={handleQueueExisting}
                            >
                                Tiếp nhận vào hàng chờ
                            </Button>
                        </div>
                    </>
                )}
            </div>

            <Modal
                open={created !== null}
                onCancel={finish}
                closable={false}
                mask={{ closable: false }}
                width={520}
                destroyOnHidden
                footer={<Button type="primary" onClick={finish}>Hoàn tất</Button>}
            >
                {created && (
                    <>
                        <div style={{ textAlign: 'center', marginBottom: 16 }}>
                            <CheckCircleFilled style={{ fontSize: 48, color: 'var(--c-primary)' }} />
                            <div style={{ fontSize: 18, fontWeight: 600, marginTop: 8 }}>Đăng ký thành công</div>
                            <div style={{ color: 'var(--c-text-muted)' }}>Đã tạo hồ sơ người hiến máu mới.</div>
                        </div>
                        {queueError ? (
                            <Alert type="warning" showIcon style={{ marginBottom: 12 }}
                                   title="Đã tạo hồ sơ nhưng chưa thêm được vào hàng chờ" description={queueError} />
                        ) : (
                            <Alert type="success" showIcon style={{ marginBottom: 12 }}
                                   title="Đã thêm vào hàng chờ tiếp nhận" />
                        )}
                        <Descriptions column={1} size="small" bordered>
                            <Descriptions.Item label="Họ và tên"><b>{created.fullName}</b></Descriptions.Item>
                            <Descriptions.Item label="Số CCCD">{created.citizenId}</Descriptions.Item>
                            <Descriptions.Item label="Ngày sinh">{fmtDate(created.dateOfBirth)}</Descriptions.Item>
                            <Descriptions.Item label="Giới tính">{created.gender}</Descriptions.Item>
                            <Descriptions.Item label="Số điện thoại">{created.phoneNumber}</Descriptions.Item>
                            <Descriptions.Item label="Mã hồ sơ">{created.donorId}</Descriptions.Item>
                        </Descriptions>
                    </>
                )}
            </Modal>
        </Card>
    )
}