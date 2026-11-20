import { ClinicalReturnCreatePanel } from '../components/ClinicalReturnCreatePanel'
import './ClinicalBloodRequestsPage.css'

export default function ClinicalReturnCreatePage() {
    return (
        <div className="clinical-page-container">
            <div className="clinical-header">
                <div>
                    <div className="clinical-breadcrumb">Yêu cầu máu Lâm sàng / W3.3 · Lập phiếu hoàn trả</div>
                    <h1 className="clinical-title">Lập phiếu hoàn trả túi máu</h1>
                    <p className="clinical-subtitle">
                        Chọn túi máu đủ điều kiện hoàn trả tại khoa, xác nhận 4 điều kiện bảo quản và gửi phiếu về Ngân hàng máu.
                    </p>
                </div>
            </div>

            <ClinicalReturnCreatePanel />
        </div>
    )
}
