-- =============================================================================
-- HỆ THỐNG QUẢN TRỊ NGÂN HÀNG MÁU "HUYẾT MẠCH 175"
-- DDL SCHEMA GENERATION SCRIPT (POSTGRESQL DIALECT)
-- =============================================================================

-- 0. DỌN DẸP NẾU ĐÃ TỒN TẠI (DROP THEO THỨ TỰ TỬ NGOẠI ĐẾN NỘI TẠI)
DROP TABLE IF EXISTS audit_logs CASCADE;
DROP TABLE IF EXISTS blood_returns CASCADE;
DROP TABLE IF EXISTS blood_allocations CASCADE;
DROP TABLE IF EXISTS blood_request_items CASCADE;
DROP TABLE IF EXISTS blood_requests CASCADE;
DROP TABLE IF EXISTS blood_test_results CASCADE;
DROP TABLE IF EXISTS blood_bags CASCADE;
DROP TABLE IF EXISTS storage_locations CASCADE;
DROP TABLE IF EXISTS blood_component_types CASCADE;
DROP TABLE IF EXISTS donation_sessions CASCADE;
DROP TABLE IF EXISTS pre_screening_surveys CASCADE;
DROP TABLE IF EXISTS donation_appointments CASCADE;
DROP TABLE IF EXISTS donors CASCADE;
DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS roles CASCADE;
DROP TABLE IF EXISTS user_roles CASCADE;
DROP TABLE IF EXISTS departments CASCADE;

-- =============================================================================
-- NHÓM A: NGƯỜI DÙNG, PHÂN QUYỀN & TỔ CHỨC (AUTH & RBAC)
-- =============================================================================

CREATE TABLE departments (
    department_id SERIAL PRIMARY KEY,
    department_code VARCHAR(20) UNIQUE NOT NULL,
    department_name VARCHAR(100) NOT NULL,
    is_blood_bank BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE roles (
    role_id SERIAL PRIMARY KEY,
    role_code VARCHAR(20) UNIQUE NOT NULL,
    role_name VARCHAR(50) NOT NULL,
    description VARCHAR(255)
);

CREATE TABLE users (
    user_id SERIAL PRIMARY KEY,
    department_id INT NOT NULL,
    username VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    phone_number VARCHAR(15),
    email VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_users_department FOREIGN KEY (department_id) 
        REFERENCES departments(department_id) ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_users_role FOREIGN KEY (role_id) 
        REFERENCES roles(role_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE user_roles (
    user_id INT NOT NULL,
    role_id INT NOT NULL,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_user_roles PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) 
        REFERENCES users(user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) 
        REFERENCES roles(role_id) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE INDEX idx_user_roles_role_id ON user_roles(role_id);

-- =============================================================================
-- NHÓM B: NGƯỜI HIẾN MÁU & PHIÊN TIẾP NHẬN (DONATION DOMAIN)
-- =============================================================================

CREATE TABLE donors (
    donor_id SERIAL PRIMARY KEY,
    citizen_id VARCHAR(12) UNIQUE NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    date_of_birth DATE NOT NULL,
    gender VARCHAR(10) NOT NULL CHECK (gender IN ('Nam', 'Nữ', 'Khác')),
    phone_number VARCHAR(15) UNIQUE NOT NULL,
    email VARCHAR(100),
    blood_type VARCHAR(5) CHECK (blood_type IN ('A', 'B', 'AB', 'O')),
    rh_factor VARCHAR(5) CHECK (rh_factor IN ('+', '-')),
    total_donations INT NOT NULL DEFAULT 0 CHECK (total_donations >= 0),
    last_donation_date DATE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE donation_appointments (
    appointment_id SERIAL PRIMARY KEY,
    donor_id INT NOT NULL,
    appointment_date DATE NOT NULL,
    time_slot VARCHAR(20) NOT NULL,
    qr_code_token VARCHAR(100) UNIQUE NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'BOOKED' 
        CHECK (status IN ('BOOKED', 'CHECKED_IN', 'CANCELLED', 'NO_SHOW')),
    is_walk_in BOOLEAN NOT NULL DEFAULT FALSE,
    checked_in_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_appointments_donor FOREIGN KEY (donor_id) 
        REFERENCES donors(donor_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE pre_screening_surveys (
    survey_id SERIAL PRIMARY KEY,
    appointment_id INT UNIQUE NOT NULL,
    survey_answers JSONB NOT NULL,
    risk_score INT NOT NULL DEFAULT 0 CHECK (risk_score >= 0),
    has_risk BOOLEAN NOT NULL DEFAULT FALSE,
    confirmed_by_donor BOOLEAN NOT NULL DEFAULT TRUE,
    submitted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_surveys_appointment FOREIGN KEY (appointment_id) 
        REFERENCES donation_appointments(appointment_id) ON DELETE CASCADE
);

CREATE TABLE donation_sessions (
    session_id SERIAL PRIMARY KEY,
    appointment_id INT UNIQUE NOT NULL,
    doctor_id INT NOT NULL,
    phlebotomist_id INT,
    weight_kg NUMERIC(5,2) NOT NULL CHECK (weight_kg >= 35.0 AND weight_kg <= 200.0),
    blood_pressure VARCHAR(20) NOT NULL,
    hemoglobin_level NUMERIC(4,1) CHECK (hemoglobin_level >= 0),
    screening_status VARCHAR(30) NOT NULL CHECK (screening_status IN ('QUALIFIED', 'DEFERRED')),
    deferral_reason VARCHAR(255),
    next_eligible_date DATE,
    target_volume_ml INT CHECK (target_volume_ml IN (250, 350, 450)),
    actual_volume_ml INT CHECK (actual_volume_ml > 0),
    collection_incident VARCHAR(255),
    status VARCHAR(30) NOT NULL DEFAULT 'PENDING_SCREENING'
        CHECK (status IN ('PENDING_SCREENING', 'QUALIFIED', 'COLLECTING', 'ABORTED', 'COMPLETED')),
    completed_at TIMESTAMP,
    CONSTRAINT fk_sessions_appointment FOREIGN KEY (appointment_id) 
        REFERENCES donation_appointments(appointment_id) ON DELETE RESTRICT,
    CONSTRAINT fk_sessions_doctor FOREIGN KEY (doctor_id) 
        REFERENCES users(user_id) ON DELETE RESTRICT,
    CONSTRAINT fk_sessions_phlebotomist FOREIGN KEY (phlebotomist_id) 
        REFERENCES users(user_id) ON DELETE RESTRICT
);

-- =============================================================================
-- NHÓM C: QUẢN LÝ ĐƠN VỊ MÁU, ĐIỀU CHẾ & LƯU KHO (BLOOD BANK INVENTORY)
-- =============================================================================

CREATE TABLE blood_component_types (
    component_type_id SERIAL PRIMARY KEY,
    type_code VARCHAR(20) UNIQUE NOT NULL,
    type_name VARCHAR(100) NOT NULL,
    default_shelf_life_days INT NOT NULL CHECK (default_shelf_life_days > 0),
    storage_temperature_range VARCHAR(50) NOT NULL,
    description VARCHAR(255)
);

CREATE TABLE storage_locations (
    location_id SERIAL PRIMARY KEY,
    storage_code VARCHAR(50) UNIQUE NOT NULL,
    refrigerator_name VARCHAR(50) NOT NULL,
    shelf_number VARCHAR(20) NOT NULL,
    target_temperature NUMERIC(4,1) NOT NULL,
    is_full BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE blood_bags (
    bag_id SERIAL PRIMARY KEY,
    barcode VARCHAR(50) UNIQUE NOT NULL,
    parent_bag_id INT,
    session_id INT,
    component_type_id INT NOT NULL,
    location_id INT,
    blood_type VARCHAR(5) NOT NULL CHECK (blood_type IN ('A', 'B', 'AB', 'O')),
    rh_factor VARCHAR(5) NOT NULL CHECK (rh_factor IN ('+', '-')),
    volume_ml INT NOT NULL CHECK (volume_ml > 0),
    collected_at TIMESTAMP NOT NULL,
    expired_at TIMESTAMP NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'NEWLY_COLLECTED'
        CHECK (status IN ('NEWLY_COLLECTED', 'QUARANTINE_TESTING', 'TESTED_PASSED', 
                          'AVAILABLE', 'LOCKED', 'RESERVED', 'ISSUED', 'TRANSFUSED', 'DISCARDED')),
    discard_reason VARCHAR(255),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_bag_expiry CHECK (expired_at > collected_at),
    CONSTRAINT fk_bags_parent FOREIGN KEY (parent_bag_id) 
        REFERENCES blood_bags(bag_id) ON DELETE RESTRICT,
    CONSTRAINT fk_bags_session FOREIGN KEY (session_id) 
        REFERENCES donation_sessions(session_id) ON DELETE RESTRICT,
    CONSTRAINT fk_bags_component_type FOREIGN KEY (component_type_id) 
        REFERENCES blood_component_types(component_type_id) ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_bags_location FOREIGN KEY (location_id) 
        REFERENCES storage_locations(location_id) ON DELETE SET NULL
);

CREATE TABLE blood_test_results (
    test_id SERIAL PRIMARY KEY,
    bag_id INT NOT NULL,
    technician_id INT NOT NULL,
    hiv_result VARCHAR(20) NOT NULL CHECK (hiv_result IN ('NEGATIVE', 'POSITIVE', 'INDETERMINATE')),
    hbv_result VARCHAR(20) NOT NULL CHECK (hbv_result IN ('NEGATIVE', 'POSITIVE', 'INDETERMINATE')),
    hcv_result VARCHAR(20) NOT NULL CHECK (hcv_result IN ('NEGATIVE', 'POSITIVE', 'INDETERMINATE')),
    syphilis_result VARCHAR(20) NOT NULL CHECK (syphilis_result IN ('NEGATIVE', 'POSITIVE', 'INDETERMINATE')),
    irregular_antibody VARCHAR(20) NOT NULL DEFAULT 'NEGATIVE',
    confirmed_blood_type VARCHAR(5) NOT NULL CHECK (confirmed_blood_type IN ('A', 'B', 'AB', 'O')),
    confirmed_rh VARCHAR(5) NOT NULL CHECK (confirmed_rh IN ('+', '-')),
    overall_conclusion VARCHAR(20) NOT NULL CHECK (overall_conclusion IN ('PASSED', 'FAILED')),
    tested_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_tests_bag FOREIGN KEY (bag_id) 
        REFERENCES blood_bags(bag_id) ON DELETE RESTRICT,
    CONSTRAINT fk_tests_technician FOREIGN KEY (technician_id) 
        REFERENCES users(user_id) ON DELETE RESTRICT
);

-- =============================================================================
-- NHÓM D: YÊU CẦU LÂM SÀNG, GIỮ CHỖ & CẤP PHÁT (CLINICAL ALLOCATION)
-- =============================================================================

CREATE TABLE blood_requests (
    request_id SERIAL PRIMARY KEY,
    request_code VARCHAR(30) UNIQUE NOT NULL,
    department_id INT NOT NULL,
    doctor_id INT NOT NULL,
    patient_code VARCHAR(50) NOT NULL,
    patient_name VARCHAR(100) NOT NULL,
    patient_blood_type VARCHAR(5) NOT NULL CHECK (patient_blood_type IN ('A', 'B', 'AB', 'O')),
    patient_rh VARCHAR(5) NOT NULL CHECK (patient_rh IN ('+', '-')),
    urgency_level VARCHAR(20) NOT NULL CHECK (urgency_level IN ('ROUTINE', 'URGENT')),
    diagnosis VARCHAR(255),
    status VARCHAR(30) NOT NULL DEFAULT 'PENDING'
        CHECK (status IN ('PENDING', 'REJECTED', 'READY_FOR_PICKUP', 'IN_TRANSIT', 'COMPLETED', 'RETURNED')),
    rejection_reason VARCHAR(255),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_requests_department FOREIGN KEY (department_id) 
        REFERENCES departments(department_id) ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_requests_doctor FOREIGN KEY (doctor_id) 
        REFERENCES users(user_id) ON DELETE RESTRICT
);

CREATE TABLE blood_request_items (
    request_item_id SERIAL PRIMARY KEY,
    request_id INT NOT NULL,
    component_type_id INT NOT NULL,
    requested_units INT NOT NULL CHECK (requested_units > 0),
    allocated_units INT NOT NULL DEFAULT 0 CHECK (allocated_units >= 0),
    CONSTRAINT chk_allocated_not_exceed CHECK (allocated_units <= requested_units),
    CONSTRAINT fk_items_request FOREIGN KEY (request_id) 
        REFERENCES blood_requests(request_id) ON DELETE CASCADE,
    CONSTRAINT fk_items_component_type FOREIGN KEY (component_type_id) 
        REFERENCES blood_component_types(component_type_id) ON DELETE RESTRICT
);

CREATE TABLE blood_allocations (
    allocation_id SERIAL PRIMARY KEY,
    request_id INT NOT NULL,
    bag_id INT NOT NULL,
    reserved_by INT NOT NULL,
    reserved_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    issued_by INT,
    received_by INT,
    issued_at TIMESTAMP,
    status VARCHAR(30) NOT NULL DEFAULT 'RESERVED'
        CHECK (status IN ('RESERVED', 'ISSUED', 'COMPLETED', 'RETURNED')),
    CONSTRAINT fk_allocations_request FOREIGN KEY (request_id) 
        REFERENCES blood_requests(request_id) ON DELETE RESTRICT,
    CONSTRAINT fk_allocations_bag FOREIGN KEY (bag_id) 
        REFERENCES blood_bags(bag_id) ON DELETE RESTRICT,
    CONSTRAINT fk_allocations_reserved_by FOREIGN KEY (reserved_by) 
        REFERENCES users(user_id) ON DELETE RESTRICT,
    CONSTRAINT fk_allocations_issued_by FOREIGN KEY (issued_by) 
        REFERENCES users(user_id) ON DELETE RESTRICT,
    CONSTRAINT fk_allocations_received_by FOREIGN KEY (received_by) 
        REFERENCES users(user_id) ON DELETE RESTRICT
);

CREATE TABLE blood_returns (
    return_id SERIAL PRIMARY KEY,
    allocation_id INT UNIQUE NOT NULL,
    returned_by INT NOT NULL,
    received_by INT NOT NULL,
    return_reason VARCHAR(255) NOT NULL,
    cold_chain_qualified BOOLEAN NOT NULL,
    final_action VARCHAR(30) NOT NULL CHECK (final_action IN ('RESTOCK', 'DISCARD')),
    processed_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_returns_allocation FOREIGN KEY (allocation_id) 
        REFERENCES blood_allocations(allocation_id) ON DELETE RESTRICT,
    CONSTRAINT fk_returns_returned_by FOREIGN KEY (returned_by) 
        REFERENCES users(user_id) ON DELETE RESTRICT,
    CONSTRAINT fk_returns_received_by FOREIGN KEY (received_by) 
        REFERENCES users(user_id) ON DELETE RESTRICT
);

-- =============================================================================
-- NHÓM E: KIỂM TOÁN, GIÁM SÁT & BẤT BIẾN (AUDIT & COMPLIANCE)
-- =============================================================================

CREATE TABLE audit_logs (
    log_id BIGSERIAL PRIMARY KEY,
    table_name VARCHAR(50) NOT NULL,
    record_id INT NOT NULL,
    action_type VARCHAR(20) NOT NULL CHECK (action_type IN ('INSERT', 'UPDATE', 'DELETE', 'STATUS_CHANGE')),
    old_state JSONB,
    new_state JSONB NOT NULL,
    performed_by INT,
    ip_address VARCHAR(45),
    user_agent VARCHAR(255),
    change_reason VARCHAR(255),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_audit_user FOREIGN KEY (performed_by) 
        REFERENCES users(user_id) ON DELETE SET NULL
);

-- =============================================================================
-- BỘ CHỈ MỤC (INDEXES) TỐI ƯU HÓA HIỆU NĂNG & RÀNG BUỘC ĐẶC BIỆT
-- =============================================================================

-- 1. Chống đặt lịch trùng lặp trong cùng 1 ngày
CREATE UNIQUE INDEX uq_active_appointment_per_day 
ON donation_appointments (donor_id, appointment_date) 
WHERE status = 'BOOKED';

-- 2. Chống giữ chỗ đồng thời 1 túi máu cho nhiều yêu cầu (Double Booking Prevention)
CREATE UNIQUE INDEX uq_active_allocation_per_bag 
ON blood_allocations (bag_id) 
WHERE status IN ('RESERVED', 'ISSUED');

-- 3. Tối ưu hóa truy vấn tra cứu kho máu theo nhóm máu, chế phẩm và trạng thái
CREATE INDEX idx_blood_bags_lookup 
ON blood_bags (blood_type, rh_factor, component_type_id, status, expired_at);

-- 4. Tối ưu hóa tra cứu quét mã Barcode túi máu
CREATE INDEX idx_blood_bags_barcode 
ON blood_bags (barcode);

-- 5. Tối ưu hóa tra cứu danh sách yêu cầu máu theo khoa và trạng thái xử lý
CREATE INDEX idx_blood_requests_dept_status 
ON blood_requests (department_id, status);

-- 6. Tối ưu hóa tra cứu lịch sử kiểm toán theo thời gian và đối tượng
CREATE INDEX idx_audit_logs_record 
ON audit_logs (table_name, record_id, created_at DESC);