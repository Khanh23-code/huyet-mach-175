import { useState, useMemo } from 'react'
import { Modal, Input, Button } from 'antd'
import { SearchOutlined, CloseOutlined } from '@ant-design/icons'
import { searchMockHisPatients } from '../mockHisPatients'
import type { HisPatientDto } from '../types'
import './SelectPatientHisModal.css'

interface SelectPatientHisModalProps {
    open: boolean
    onClose: () => void
    onSelectPatient: (patient: HisPatientDto) => void
    currentSelectedCode?: string
}

export const SelectPatientHisModal: React.FC<SelectPatientHisModalProps> = ({
    open,
    onClose,
    onSelectPatient,
    currentSelectedCode,
}) => {
    const [searchKeyword, setSearchKeyword] = useState('')
    const [appliedKeyword, setAppliedKeyword] = useState('')
    const [selectedPatient, setSelectedPatient] = useState<HisPatientDto | null>(null)

    const patientList = useMemo(() => {
        return searchMockHisPatients(appliedKeyword)
    }, [appliedKeyword])

    const handleSearch = () => {
        setAppliedKeyword(searchKeyword)
    }

    const handleConfirm = () => {
        const patientToConfirm = selectedPatient || patientList.find((p) => p.patientCode === currentSelectedCode)
        if (patientToConfirm) {
            onSelectPatient(patientToConfirm)
            onClose()
        }
    }

    return (
        <Modal
            open={open}
            onCancel={onClose}
            footer={null}
            closeIcon={<CloseOutlined className="his-modal__close-icon" />}
            width={640}
            className="his-patient-modal"
            centered
            destroyOnClose
        >
            <div className="his-modal__header">
                <span className="his-modal__badge">HIS / EMR</span>
                <h2 className="his-modal__title">Chọn bệnh nhân</h2>
                <p className="his-modal__subtitle">
                    Tìm và chọn bệnh nhân từ dữ liệu đồng bộ HIS/EMR. Cập nhật lần cuối: 10:02 hôm nay.
                </p>
            </div>

            <div className="his-modal__search-box">
                <Input
                    placeholder="Tìm tên bệnh nhân hoặc mã hồ sơ..."
                    value={searchKeyword}
                    onChange={(e) => setSearchKeyword(e.target.value)}
                    onPressEnter={handleSearch}
                    allowClear
                    className="his-modal__input"
                />
                <Button type="default" onClick={handleSearch} className="his-modal__search-btn">
                    Tìm
                </Button>
            </div>

            <div className="his-modal__list">
                {patientList.length === 0 ? (
                    <div className="his-modal__empty">
                        <SearchOutlined style={{ fontSize: 28, color: '#94a3b8', marginBottom: 8 }} />
                        <p>Không tìm thấy bệnh nhân nào phù hợp với từ khóa "{appliedKeyword}"</p>
                    </div>
                ) : (
                    patientList.map((p) => {
                        const isSelected = selectedPatient?.patientCode === p.patientCode || (!selectedPatient && currentSelectedCode === p.patientCode)

                        return (
                            <div
                                key={p.patientCode}
                                className={`his-patient-card ${isSelected ? 'is-selected' : ''}`}
                                onClick={() => setSelectedPatient(p)}
                            >
                                <div className="his-patient-card__avatar">
                                    {p.initials}
                                </div>
                                <div className="his-patient-card__info">
                                    <div className="his-patient-card__name-line">
                                        <span className="his-patient-card__name">{p.patientName}</span>
                                        <span className="his-patient-card__dot">·</span>
                                        <span className="his-patient-card__age">{p.age} tuổi</span>
                                    </div>
                                    <div className="his-patient-card__location-line">
                                        <span>{p.patientCode}</span>
                                        <span className="his-patient-card__dot">·</span>
                                        <span>{p.room}</span>
                                        <span className="his-patient-card__dot">·</span>
                                        <span>{p.bed}</span>
                                    </div>
                                    <div className="his-patient-card__diagnosis">
                                        {p.diagnosis}
                                    </div>
                                </div>
                            </div>
                        )
                    })
                )}
            </div>

            <div className="his-modal__footer">
                <Button onClick={onClose} className="his-modal__cancel-btn">
                    Hủy
                </Button>
                <Button
                    type="primary"
                    onClick={handleConfirm}
                    disabled={!selectedPatient && !currentSelectedCode}
                    className="his-modal__submit-btn"
                >
                    Chọn bệnh nhân này
                </Button>
            </div>
        </Modal>
    )
}
