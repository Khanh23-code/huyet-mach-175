using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HuyetMach175.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blood_component_types",
                columns: table => new
                {
                    component_type_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type_code = table.Column<string>(type: "text", nullable: false),
                    type_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    life_days = table.Column<int>(type: "integer", nullable: false),
                    storage_temperature = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blood_component_types", x => x.component_type_id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    department_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    department_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    department_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.department_id);
                });

            migrationBuilder.CreateTable(
                name: "donors",
                columns: table => new
                {
                    donor_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    citizen_id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    blood_type = table.Column<string>(type: "text", nullable: true),
                    rh_factor = table.Column<string>(type: "text", nullable: true),
                    total_donations = table.Column<int>(type: "integer", nullable: false),
                    last_donation_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donors", x => x.donor_id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    permission_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    permission_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    permission_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.permission_id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_code = table.Column<string>(type: "text", nullable: false),
                    role_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "storage_locations",
                columns: table => new
                {
                    location_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    refrigerator_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    shelf_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_temperature = table.Column<decimal>(type: "numeric", nullable: false),
                    is_full = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage_locations", x => x.location_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    department_id = table.Column<int>(type: "integer", nullable: false),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_users_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "department_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    permission_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "permission_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    log_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    table_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    record_id = table.Column<int>(type: "integer", nullable: false),
                    action_type = table.Column<string>(type: "text", nullable: false),
                    old_state = table.Column<string>(type: "jsonb", nullable: true),
                    new_state = table.Column<string>(type: "jsonb", nullable: false),
                    performed_by = table.Column<int>(type: "integer", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    change_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.log_id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_performed_by",
                        column: x => x.performed_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "blood_requests",
                columns: table => new
                {
                    request_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    request_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    doctor_id = table.Column<int>(type: "integer", nullable: true),
                    patient_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    patient_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    patient_blood_type = table.Column<string>(type: "text", nullable: false),
                    patient_rh = table.Column<string>(type: "text", nullable: false),
                    urgency_level = table.Column<string>(type: "text", nullable: false),
                    diagnosis = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blood_requests", x => x.request_id);
                    table.ForeignKey(
                        name: "FK_blood_requests_users_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "donation_campaigns",
                columns: table => new
                {
                    campaign_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    campaign_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    location_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    target_donations = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    approved_by = table.Column<int>(type: "integer", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donation_campaigns", x => x.campaign_id);
                    table.ForeignKey(
                        name: "FK_donation_campaigns_users_approved_by",
                        column: x => x.approved_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_donation_campaigns_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    notification_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    recipient_user_id = table.Column<int>(type: "integer", nullable: true),
                    target_role_id = table.Column<int>(type: "integer", nullable: true),
                    target_department_id = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    reference_type = table.Column<string>(type: "text", nullable: true),
                    reference_id = table.Column<int>(type: "integer", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification", x => x.notification_id);
                    table.ForeignKey(
                        name: "FK_notification_departments_target_department_id",
                        column: x => x.target_department_id,
                        principalTable: "departments",
                        principalColumn: "department_id");
                    table.ForeignKey(
                        name: "FK_notification_roles_target_role_id",
                        column: x => x.target_role_id,
                        principalTable: "roles",
                        principalColumn: "role_id");
                    table.ForeignKey(
                        name: "FK_notification_users_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "blood_request_items",
                columns: table => new
                {
                    request_item_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    request_id = table.Column<int>(type: "integer", nullable: false),
                    component_type_id = table.Column<int>(type: "integer", nullable: false),
                    requested_units = table.Column<int>(type: "integer", nullable: false),
                    allocated_units = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blood_request_items", x => x.request_item_id);
                    table.ForeignKey(
                        name: "FK_blood_request_items_blood_component_types_component_type_id",
                        column: x => x.component_type_id,
                        principalTable: "blood_component_types",
                        principalColumn: "component_type_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_blood_request_items_blood_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "blood_requests",
                        principalColumn: "request_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "donation_appointments",
                columns: table => new
                {
                    appointment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    donor_id = table.Column<int>(type: "integer", nullable: false),
                    campaign_id = table.Column<int>(type: "integer", nullable: true),
                    appointment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    time_slot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    qr_code_token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    is_walk_in = table.Column<bool>(type: "boolean", nullable: false),
                    checked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donation_appointments", x => x.appointment_id);
                    table.ForeignKey(
                        name: "FK_donation_appointments_donation_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "donation_campaigns",
                        principalColumn: "campaign_id");
                    table.ForeignKey(
                        name: "FK_donation_appointments_donors_donor_id",
                        column: x => x.donor_id,
                        principalTable: "donors",
                        principalColumn: "donor_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "donation_sessions",
                columns: table => new
                {
                    session_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    appointment_id = table.Column<int>(type: "integer", nullable: false),
                    doctor_id = table.Column<int>(type: "integer", nullable: false),
                    phlebotomist_id = table.Column<int>(type: "integer", nullable: true),
                    weight_kg = table.Column<decimal>(type: "numeric", nullable: false),
                    blood_pressure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    hemoglobin_level = table.Column<decimal>(type: "numeric", nullable: true),
                    screening_status = table.Column<string>(type: "text", nullable: false),
                    deferral_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    target_volume_ml = table.Column<string>(type: "text", nullable: true),
                    actual_volume_ml = table.Column<int>(type: "integer", nullable: true),
                    collection_incident = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donation_sessions", x => x.session_id);
                    table.ForeignKey(
                        name: "FK_donation_sessions_donation_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "donation_appointments",
                        principalColumn: "appointment_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_donation_sessions_users_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_donation_sessions_users_phlebotomist_id",
                        column: x => x.phlebotomist_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pre_screening_surveys",
                columns: table => new
                {
                    survey_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    appointment_id = table.Column<int>(type: "integer", nullable: false),
                    survey_answers = table.Column<string>(type: "jsonb", nullable: false),
                    risk_score = table.Column<int>(type: "integer", nullable: false),
                    has_risk = table.Column<bool>(type: "boolean", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pre_screening_surveys", x => x.survey_id);
                    table.ForeignKey(
                        name: "FK_pre_screening_surveys_donation_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "donation_appointments",
                        principalColumn: "appointment_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "blood_bags",
                columns: table => new
                {
                    bag_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    parent_bag_id = table.Column<int>(type: "integer", nullable: true),
                    session_id = table.Column<int>(type: "integer", nullable: true),
                    component_type_id = table.Column<int>(type: "integer", nullable: false),
                    location_id = table.Column<int>(type: "integer", nullable: true),
                    blood_type = table.Column<string>(type: "text", nullable: false),
                    rh_factor = table.Column<string>(type: "text", nullable: false),
                    volume_ml = table.Column<int>(type: "integer", nullable: false),
                    collected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    discard_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blood_bags", x => x.bag_id);
                    table.ForeignKey(
                        name: "FK_blood_bags_blood_bags_parent_bag_id",
                        column: x => x.parent_bag_id,
                        principalTable: "blood_bags",
                        principalColumn: "bag_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_blood_bags_blood_component_types_component_type_id",
                        column: x => x.component_type_id,
                        principalTable: "blood_component_types",
                        principalColumn: "component_type_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_blood_bags_donation_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "donation_sessions",
                        principalColumn: "session_id");
                    table.ForeignKey(
                        name: "FK_blood_bags_storage_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "storage_locations",
                        principalColumn: "location_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "blood_allocations",
                columns: table => new
                {
                    allocation_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    request_id = table.Column<int>(type: "integer", nullable: false),
                    bag_id = table.Column<int>(type: "integer", nullable: false),
                    reserved_by = table.Column<int>(type: "integer", nullable: false),
                    reserved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    issued_by = table.Column<int>(type: "integer", nullable: true),
                    received_by = table.Column<int>(type: "integer", nullable: true),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blood_allocations", x => x.allocation_id);
                    table.ForeignKey(
                        name: "FK_blood_allocations_blood_bags_bag_id",
                        column: x => x.bag_id,
                        principalTable: "blood_bags",
                        principalColumn: "bag_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_blood_allocations_blood_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "blood_requests",
                        principalColumn: "request_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_blood_allocations_users_issued_by",
                        column: x => x.issued_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_blood_allocations_users_received_by",
                        column: x => x.received_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_blood_allocations_users_reserved_by",
                        column: x => x.reserved_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "blood_test_results",
                columns: table => new
                {
                    test_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bag_id = table.Column<int>(type: "integer", nullable: false),
                    technician_id = table.Column<int>(type: "integer", nullable: false),
                    hiv_result = table.Column<string>(type: "text", nullable: false),
                    hbv_result = table.Column<string>(type: "text", nullable: false),
                    hcv_result = table.Column<string>(type: "text", nullable: false),
                    syphilis_result = table.Column<string>(type: "text", nullable: false),
                    irregular_antibody = table.Column<string>(type: "text", nullable: false),
                    confirmed_blood_type = table.Column<string>(type: "text", nullable: false),
                    confirmed_rh = table.Column<string>(type: "text", nullable: false),
                    overall_conclusion = table.Column<string>(type: "text", nullable: false),
                    tested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blood_test_results", x => x.test_id);
                    table.ForeignKey(
                        name: "FK_blood_test_results_blood_bags_bag_id",
                        column: x => x.bag_id,
                        principalTable: "blood_bags",
                        principalColumn: "bag_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_blood_test_results_users_technician_id",
                        column: x => x.technician_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "blood_returns",
                columns: table => new
                {
                    return_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    allocation_id = table.Column<int>(type: "integer", nullable: false),
                    returned_by = table.Column<int>(type: "integer", nullable: false),
                    received_by = table.Column<int>(type: "integer", nullable: false),
                    return_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    qualified = table.Column<bool>(type: "boolean", nullable: false),
                    final_action = table.Column<string>(type: "text", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blood_returns", x => x.return_id);
                    table.ForeignKey(
                        name: "FK_blood_returns_blood_allocations_allocation_id",
                        column: x => x.allocation_id,
                        principalTable: "blood_allocations",
                        principalColumn: "allocation_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_blood_returns_users_received_by",
                        column: x => x.received_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_blood_returns_users_returned_by",
                        column: x => x.returned_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_record",
                table: "audit_logs",
                columns: new[] { "table_name", "record_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_performed_by",
                table: "audit_logs",
                column: "performed_by");

            migrationBuilder.CreateIndex(
                name: "IX_blood_allocations_issued_by",
                table: "blood_allocations",
                column: "issued_by");

            migrationBuilder.CreateIndex(
                name: "IX_blood_allocations_received_by",
                table: "blood_allocations",
                column: "received_by");

            migrationBuilder.CreateIndex(
                name: "IX_blood_allocations_request_id",
                table: "blood_allocations",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_allocations_reserved_by",
                table: "blood_allocations",
                column: "reserved_by");

            migrationBuilder.CreateIndex(
                name: "uq_active_allocation_per_bag",
                table: "blood_allocations",
                column: "bag_id",
                unique: true,
                filter: "status IN ('RESERVED', 'ISSUED')");

            migrationBuilder.CreateIndex(
                name: "idx_blood_bags_lookup",
                table: "blood_bags",
                columns: new[] { "blood_type", "rh_factor", "component_type_id", "status", "expired_at" });

            migrationBuilder.CreateIndex(
                name: "IX_blood_bags_barcode",
                table: "blood_bags",
                column: "barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_blood_bags_component_type_id",
                table: "blood_bags",
                column: "component_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_bags_location_id",
                table: "blood_bags",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_bags_parent_bag_id",
                table: "blood_bags",
                column: "parent_bag_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_bags_session_id",
                table: "blood_bags",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_component_types_type_code",
                table: "blood_component_types",
                column: "type_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_blood_request_items_component_type_id",
                table: "blood_request_items",
                column: "component_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_request_items_request_id",
                table: "blood_request_items",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_requests_doctor_id",
                table: "blood_requests",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_requests_request_code",
                table: "blood_requests",
                column: "request_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_blood_returns_allocation_id",
                table: "blood_returns",
                column: "allocation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_blood_returns_received_by",
                table: "blood_returns",
                column: "received_by");

            migrationBuilder.CreateIndex(
                name: "IX_blood_returns_returned_by",
                table: "blood_returns",
                column: "returned_by");

            migrationBuilder.CreateIndex(
                name: "IX_blood_test_results_bag_id",
                table: "blood_test_results",
                column: "bag_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_test_results_technician_id",
                table: "blood_test_results",
                column: "technician_id");

            migrationBuilder.CreateIndex(
                name: "IX_departments_department_code",
                table: "departments",
                column: "department_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_donation_appointments_campaign_id",
                table: "donation_appointments",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "IX_donation_appointments_qr_code_token",
                table: "donation_appointments",
                column: "qr_code_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_active_appointment_per_day",
                table: "donation_appointments",
                columns: new[] { "donor_id", "appointment_date" },
                unique: true,
                filter: "status = 'BOOKED'");

            migrationBuilder.CreateIndex(
                name: "IX_donation_campaigns_approved_by",
                table: "donation_campaigns",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_donation_campaigns_created_by",
                table: "donation_campaigns",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_donation_sessions_appointment_id",
                table: "donation_sessions",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_donation_sessions_doctor_id",
                table: "donation_sessions",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_donation_sessions_phlebotomist_id",
                table: "donation_sessions",
                column: "phlebotomist_id");

            migrationBuilder.CreateIndex(
                name: "IX_donors_citizen_id",
                table: "donors",
                column: "citizen_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_donors_phone_number",
                table: "donors",
                column: "phone_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_notifications_recipient_unread",
                table: "notification",
                columns: new[] { "recipient_user_id", "is_read" },
                filter: "is_read = FALSE");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_target_role",
                table: "notification",
                column: "target_role_id",
                filter: "target_role_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_notification_target_department_id",
                table: "notification",
                column: "target_department_id");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_permission_code",
                table: "permissions",
                column: "permission_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pre_screening_surveys_appointment_id",
                table: "pre_screening_surveys",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_role_code",
                table: "roles",
                column: "role_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_department_id",
                table: "users",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "blood_request_items");

            migrationBuilder.DropTable(
                name: "blood_returns");

            migrationBuilder.DropTable(
                name: "blood_test_results");

            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "pre_screening_surveys");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "blood_allocations");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "blood_bags");

            migrationBuilder.DropTable(
                name: "blood_requests");

            migrationBuilder.DropTable(
                name: "blood_component_types");

            migrationBuilder.DropTable(
                name: "donation_sessions");

            migrationBuilder.DropTable(
                name: "storage_locations");

            migrationBuilder.DropTable(
                name: "donation_appointments");

            migrationBuilder.DropTable(
                name: "donation_campaigns");

            migrationBuilder.DropTable(
                name: "donors");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "departments");
        }
    }
}
