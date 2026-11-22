import type { HisPatientDto } from './types'

/**
 * Mockdata bệnh nhân từ hệ thống HIS/EMR của Bệnh viện Quân y 175.
 * Được tách biệt riêng thành file mock để có thể dễ dàng thay thế bằng API thực tế
 * khi phân hệ tích hợp HIS hoàn tất.
 */
export const MOCK_HIS_PATIENTS: HisPatientDto[] = [
    {
        patientCode: 'BN-250602-0041',
        patientName: 'Trần Văn Bình',
        age: 54,
        gender: 'Nam',
        room: 'Phòng A3',
        bed: 'Giường 12',
        diagnosis: 'Xuất huyết tiêu hóa cấp do loét dạ dày',
        bloodType: 'O',
        rhFactor: 'POSITIVE',
        bloodGroupDisplay: 'O+',
        initials: 'VB',
    },
    {
        patientCode: 'BN-250602-0038',
        patientName: 'Nguyễn Thị Lan',
        age: 42,
        gender: 'Nữ',
        room: 'Phòng A1',
        bed: 'Giường 05',
        diagnosis: 'Phẫu thuật bướu giáp đa nhân kích thước lớn',
        bloodType: 'A',
        rhFactor: 'POSITIVE',
        bloodGroupDisplay: 'A+',
        initials: 'TL',
    },
    {
        patientCode: 'BN-250601-0927',
        patientName: 'Lê Hoàng Phúc',
        age: 29,
        gender: 'Nam',
        room: 'ICU',
        bed: 'Giường 03',
        diagnosis: 'Sốc mất máu nặng sau chấn thương đa cơ quan',
        bloodType: 'B',
        rhFactor: 'POSITIVE',
        bloodGroupDisplay: 'B+',
        initials: 'HP',
    },
    {
        patientCode: 'BN-250602-0112',
        patientName: 'Vũ Thị Mai',
        age: 36,
        gender: 'Nữ',
        room: 'Phòng C2',
        bed: 'Giường 08',
        diagnosis: 'Băng huyết thứ phát sau phẫu thuật mổ đẻ',
        bloodType: 'O',
        rhFactor: 'NEGATIVE',
        bloodGroupDisplay: 'O-',
        initials: 'TM',
    },
    {
        patientCode: 'BN-250602-0085',
        patientName: 'Hoàng Đức Anh',
        age: 61,
        gender: 'Nam',
        room: 'Phòng Tim mạch',
        bed: 'Giường 16',
        diagnosis: 'Hội chứng suy tủy xương / Thiếu máu nặng',
        bloodType: 'AB',
        rhFactor: 'POSITIVE',
        bloodGroupDisplay: 'AB+',
        initials: 'ĐA',
    },
    {
        patientCode: 'BN-250601-0814',
        patientName: 'Đặng Minh Tuấn',
        age: 48,
        gender: 'Nam',
        room: 'Phòng Cấp cứu A',
        bed: 'Giường 02',
        diagnosis: 'Chấn thương bụng kín, vỡ lách độ 4',
        bloodType: 'A',
        rhFactor: 'NEGATIVE',
        bloodGroupDisplay: 'A-',
        initials: 'MT',
    },
    {
        patientCode: 'BN-250602-0145',
        patientName: 'Phạm Thu Hương',
        age: 23,
        gender: 'Nữ',
        room: 'Phòng Huyết học',
        bed: 'Giường 07',
        diagnosis: 'Xuất huyết giảm tiểu cầu miễn dịch vô căn (ITP)',
        bloodType: 'B',
        rhFactor: 'NEGATIVE',
        bloodGroupDisplay: 'B-',
        initials: 'TH',
    },
]

/** Hàm tìm kiếm bệnh nhân mock HIS theo từ khóa tên hoặc mã hồ sơ */
export function searchMockHisPatients(keyword: string): HisPatientDto[] {
    const cleanKw = keyword.trim().toLowerCase()
    if (!cleanKw) return MOCK_HIS_PATIENTS

    return MOCK_HIS_PATIENTS.filter(
        (p) =>
            p.patientName.toLowerCase().includes(cleanKw) ||
            p.patientCode.toLowerCase().includes(cleanKw) ||
            p.diagnosis.toLowerCase().includes(cleanKw),
    )
}
