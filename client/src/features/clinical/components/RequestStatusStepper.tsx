import React from 'react'
import { CheckOutlined, CloseCircleOutlined } from '@ant-design/icons'
import type { RequestStatus } from '../types'
import './RequestStatusStepper.css'

interface RequestStatusStepperProps {
    currentStep: number
    status: RequestStatus
}

export const RequestStatusStepper: React.FC<RequestStatusStepperProps> = ({ currentStep, status }) => {
    if (status === 'REJECTED') {
        return (
            <div className="stepper-rejected">
                <CloseCircleOutlined className="stepper-rejected__icon" />
                <span>Bị từ chối</span>
            </div>
        )
    }

    if (status === 'DRAFT') {
        return (
            <div className="stepper-draft">
                <span className="stepper-draft__dot" />
                <span>Bản nháp (Chưa gửi)</span>
            </div>
        )
    }

    /**
     * Quy ước Backend:
     * - DRAFT: 0
     * - PENDING: 1 (Đã gửi) -> Bước 1 hoàn thành, bước 2 đang thực hiện
     * - READY: 2 (Đã tiếp nhận và sẵn sàng cấp phát) -> Bước 1, 2, 3 hoàn thành, bước 4 sẵn sàng
     * - COMPLETED: 3 -> Cả 4 bước hoàn thành
     */
    const steps = [
        { label: 'Đã gửi', num: 1 },
        { label: 'Đã tiếp nhận', num: 2 },
        { label: 'Đã chuẩn bị', num: 3 },
        { label: 'Sẵn sàng', num: 4 },
    ]

    // Tính toán số lượng bước đã hoàn thành dựa vào currentStep hoặc status
    let completedSteps = currentStep > 0 ? currentStep : 1
    if (status === 'COMPLETED') {
        completedSteps = 4
    } else if (status === 'READY') {
        completedSteps = Math.max(3, currentStep)
    } else if (status === 'PENDING') {
        completedSteps = Math.max(1, currentStep)
    }

    return (
        <div className="clinical-stepper">
            {steps.map((step, idx) => {
                const isCompleted = step.num <= completedSteps
                const hasNext = idx < steps.length - 1
                const isLineActive = step.num < completedSteps

                return (
                    <React.Fragment key={step.num}>
                        <div className="clinical-stepper__node">
                            <div className={`clinical-stepper__circle ${isCompleted ? 'is-completed' : 'is-pending'}`}>
                                {isCompleted ? <CheckOutlined className="clinical-stepper__check" /> : step.num}
                            </div>
                            <span className={`clinical-stepper__label ${isCompleted ? 'is-active' : ''}`}>
                                {step.label}
                            </span>
                        </div>
                        {hasNext && (
                            <div className={`clinical-stepper__line ${isLineActive ? 'is-active' : ''}`} />
                        )}
                    </React.Fragment>
                )
            })}
        </div>
    )
}
