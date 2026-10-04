using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuyetMach175.Api.Data.Entities;

#region NHÓM A: AUTH & RBAC

[Table("departments")]
public class Department
{
    [Key]
    [Column("department_id")]
    public int DepartmentId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("department_code")]
    public string DepartmentCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("department_name")]
    public string DepartmentName { get; set; } = string.Empty;

    [Column("is_blood_bank")]
    public bool IsBloodBank { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<BloodRequest> BloodRequests { get; set; } = new List<BloodRequest>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

[Table("roles")]
public class Role
{
    [Key]
    [Column("role_id")]
    public int RoleId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("role_code")]
    public string RoleCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("role_name")]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("description")]
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

[Table("users")]
public class User
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("department_id")]
    public int DepartmentId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(15)]
    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    [Column("email")]
    public string? Email { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(DepartmentId))]
    public Department? Department { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<DonationCampaign> CreatedCampaigns { get; set; } = new List<DonationCampaign>();
    public ICollection<DonationCampaign> ApprovedCampaigns { get; set; } = new List<DonationCampaign>();
    public ICollection<DonationSession> DoctorSessions { get; set; } = new List<DonationSession>();
    public ICollection<DonationSession> PhlebotomistSessions { get; set; } = new List<DonationSession>();
    public ICollection<BloodTestResult> TestResults { get; set; } = new List<BloodTestResult>();
    public ICollection<BloodRequest> DoctorRequests { get; set; } = new List<BloodRequest>();
    public ICollection<BloodAllocation> ReservedAllocations { get; set; } = new List<BloodAllocation>();
    public ICollection<BloodAllocation> IssuedAllocations { get; set; } = new List<BloodAllocation>();
    public ICollection<BloodAllocation> ReceivedAllocations { get; set; } = new List<BloodAllocation>();
    public ICollection<BloodReturn> ReturnedReturns { get; set; } = new List<BloodReturn>();
    public ICollection<BloodReturn> ReceivedReturns { get; set; } = new List<BloodReturn>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<Notification> ReceivedNotifications { get; set; } = new List<Notification>();
}

[Table("user_roles")]
public class UserRole
{
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }
}

[Table("permissions")]
public class Permission
{
    [Key]
    [Column("permission_id")]
    public int PermissionId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("permission_code")]
    public string PermissionCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("permission_name")]
    public string PermissionName { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    [Column("module")]
    public string Module { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("description")]
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

[Table("role_permissions")]
public class RolePermission
{
    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("permission_id")]
    public int PermissionId { get; set; }

    [Column("granted_at")]
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }

    [ForeignKey(nameof(PermissionId))]
    public Permission? Permission { get; set; }
}

#endregion

#region NHÓM B: DONATION DOMAIN

[Table("donation_campaigns")]
public class DonationCampaign
{
    [Key]
    [Column("campaign_id")]
    public int CampaignId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("campaign_name")]
    public string CampaignName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("location_name")]
    public string LocationName { get; set; } = string.Empty;

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly EndDate { get; set; }

    [Column("target_donations")]
    public int TargetDonations { get; set; } = 100;

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "DRAFT";

    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("approved_by")]
    public int? ApprovedBy { get; set; }

    [MaxLength(255)]
    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(CreatedBy))]
    public User? CreatedByUser { get; set; }

    [ForeignKey(nameof(ApprovedBy))]
    public User? ApprovedByUser { get; set; }

    public ICollection<DonationAppointment> Appointments { get; set; } = new List<DonationAppointment>();
}

[Table("donors")]
public class Donor
{
    [Key]
    [Column("donor_id")]
    public int DonorId { get; set; }

    [Required]
    [MaxLength(12)]
    [Column("citizen_id")]
    public string CitizenId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("date_of_birth")]
    public DateOnly DateOfBirth { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("gender")]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [MaxLength(15)]
    [Column("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("email")]
    public string? Email { get; set; }

    [MaxLength(5)]
    [Column("blood_type")]
    public string? BloodType { get; set; }

    [MaxLength(5)]
    [Column("rh_factor")]
    public string? RhFactor { get; set; }

    [Column("total_donations")]
    public int TotalDonations { get; set; } = 0;

    [Column("last_donation_date")]
    public DateOnly? LastDonationDate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<DonationAppointment> Appointments { get; set; } = new List<DonationAppointment>();
}

[Table("donation_appointments")]
public class DonationAppointment
{
    [Key]
    [Column("appointment_id")]
    public int AppointmentId { get; set; }

    [Column("donor_id")]
    public int DonorId { get; set; }

    [Column("campaign_id")]
    public int? CampaignId { get; set; }

    [Column("appointment_date")]
    public DateOnly AppointmentDate { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("time_slot")]
    public string TimeSlot { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("qr_code_token")]
    public string QrCodeToken { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "BOOKED";

    [Column("is_walk_in")]
    public bool IsWalkIn { get; set; } = false;

    [Column("checked_in_at")]
    public DateTime? CheckedInAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(DonorId))]
    public Donor? Donor { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public DonationCampaign? Campaign { get; set; }

    public PreScreeningSurvey? PreScreeningSurvey { get; set; }
    public DonationSession? DonationSession { get; set; }
}

[Table("pre_screening_surveys")]
public class PreScreeningSurvey
{
    [Key]
    [Column("survey_id")]
    public int SurveyId { get; set; }

    [Column("appointment_id")]
    public int AppointmentId { get; set; }

    [Required]
    [Column("survey_answers", TypeName = "jsonb")]
    public string SurveyAnswers { get; set; } = "{}";

    [Column("risk_score")]
    public int RiskScore { get; set; } = 0;

    [Column("has_risk")]
    public bool HasRisk { get; set; } = false;

    [Column("confirmed_by_donor")]
    public bool ConfirmedByDonor { get; set; } = true;

    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(AppointmentId))]
    public DonationAppointment? Appointment { get; set; }
}

[Table("donation_sessions")]
public class DonationSession
{
    [Key]
    [Column("session_id")]
    public int SessionId { get; set; }

    [Column("appointment_id")]
    public int AppointmentId { get; set; }

    [Column("doctor_id")]
    public int DoctorId { get; set; }

    [Column("phlebotomist_id")]
    public int? PhlebotomistId { get; set; }

    [Column("weight_kg")]
    public decimal WeightKg { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("blood_pressure")]
    public string BloodPressure { get; set; } = string.Empty;

    [Column("hemoglobin_level")]
    public decimal? HemoglobinLevel { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("screening_status")]
    public string ScreeningStatus { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("deferral_reason")]
    public string? DeferralReason { get; set; }

    [Column("next_eligible_date")]
    public DateOnly? NextEligibleDate { get; set; }

    [Column("target_volume_ml")]
    public int? TargetVolumeMl { get; set; }

    [Column("actual_volume_ml")]
    public int? ActualVolumeMl { get; set; }

    [MaxLength(255)]
    [Column("collection_incident")]
    public string? CollectionIncident { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "PENDING_SCREENING";

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(AppointmentId))]
    public DonationAppointment? Appointment { get; set; }

    [ForeignKey(nameof(DoctorId))]
    public User? Doctor { get; set; }

    [ForeignKey(nameof(PhlebotomistId))]
    public User? Phlebotomist { get; set; }

    public ICollection<BloodBag> BloodBags { get; set; } = new List<BloodBag>();
}

#endregion

#region NHÓM C: BLOOD BANK INVENTORY

[Table("blood_component_types")]
public class BloodComponentType
{
    [Key]
    [Column("component_type_id")]
    public int ComponentTypeId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("type_code")]
    public string TypeCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("type_name")]
    public string TypeName { get; set; } = string.Empty;

    [Column("default_shelf_life_days")]
    public int DefaultShelfLifeDays { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("storage_temperature_range")]
    public string StorageTemperatureRange { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("description")]
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<BloodBag> BloodBags { get; set; } = new List<BloodBag>();
    public ICollection<BloodRequestItem> RequestItems { get; set; } = new List<BloodRequestItem>();
}

[Table("storage_locations")]
public class StorageLocation
{
    [Key]
    [Column("location_id")]
    public int LocationId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("storage_code")]
    public string StorageCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("refrigerator_name")]
    public string RefrigeratorName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("shelf_number")]
    public string ShelfNumber { get; set; } = string.Empty;

    [Column("target_temperature")]
    public decimal TargetTemperature { get; set; }

    [Column("is_full")]
    public bool IsFull { get; set; } = false;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<BloodBag> BloodBags { get; set; } = new List<BloodBag>();
}

[Table("blood_bags")]
public class BloodBag
{
    [Key]
    [Column("bag_id")]
    public int BagId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [Column("parent_bag_id")]
    public int? ParentBagId { get; set; }

    [Column("session_id")]
    public int? SessionId { get; set; }

    [Column("component_type_id")]
    public int ComponentTypeId { get; set; }

    [Column("location_id")]
    public int? LocationId { get; set; }

    [Required]
    [MaxLength(5)]
    [Column("blood_type")]
    public string BloodType { get; set; } = string.Empty;

    [Required]
    [MaxLength(5)]
    [Column("rh_factor")]
    public string RhFactor { get; set; } = string.Empty;

    [Column("volume_ml")]
    public int VolumeMl { get; set; }

    [Column("collected_at")]
    public DateTime CollectedAt { get; set; }

    [Column("expired_at")]
    public DateTime ExpiredAt { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "NEWLY_COLLECTED";

    [MaxLength(255)]
    [Column("discard_reason")]
    public string? DiscardReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ParentBagId))]
    public BloodBag? ParentBag { get; set; }

    [ForeignKey(nameof(SessionId))]
    public DonationSession? Session { get; set; }

    [ForeignKey(nameof(ComponentTypeId))]
    public BloodComponentType? ComponentType { get; set; }

    [ForeignKey(nameof(LocationId))]
    public StorageLocation? Location { get; set; }

    public ICollection<BloodBag> DerivedBags { get; set; } = new List<BloodBag>();
    public ICollection<BloodTestResult> TestResults { get; set; } = new List<BloodTestResult>();
    public ICollection<BloodAllocation> Allocations { get; set; } = new List<BloodAllocation>();
}

[Table("blood_test_results")]
public class BloodTestResult
{
    [Key]
    [Column("test_id")]
    public int TestId { get; set; }

    [Column("bag_id")]
    public int BagId { get; set; }

    [Column("technician_id")]
    public int TechnicianId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("hiv_result")]
    public string HivResult { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("hbv_result")]
    public string HbvResult { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("hcv_result")]
    public string HcvResult { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("syphilis_result")]
    public string SyphilisResult { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("irregular_antibody")]
    public string IrregularAntibody { get; set; } = "NEGATIVE";

    [Required]
    [MaxLength(5)]
    [Column("confirmed_blood_type")]
    public string ConfirmedBloodType { get; set; } = string.Empty;

    [Required]
    [MaxLength(5)]
    [Column("confirmed_rh")]
    public string ConfirmedRh { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("overall_conclusion")]
    public string OverallConclusion { get; set; } = string.Empty;

    [Column("tested_at")]
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(BagId))]
    public BloodBag? Bag { get; set; }

    [ForeignKey(nameof(TechnicianId))]
    public User? Technician { get; set; }
}

#endregion

#region NHÓM D: CLINICAL ALLOCATION

[Table("blood_requests")]
public class BloodRequest
{
    [Key]
    [Column("request_id")]
    public int RequestId { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("request_code")]
    public string RequestCode { get; set; } = string.Empty;

    [Column("department_id")]
    public int DepartmentId { get; set; }

    [Column("doctor_id")]
    public int DoctorId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("patient_code")]
    public string PatientCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("patient_name")]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    [MaxLength(5)]
    [Column("patient_blood_type")]
    public string PatientBloodType { get; set; } = string.Empty;

    [Required]
    [MaxLength(5)]
    [Column("patient_rh")]
    public string PatientRh { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("urgency_level")]
    public string UrgencyLevel { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("diagnosis")]
    public string? Diagnosis { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "PENDING";

    [MaxLength(255)]
    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(DepartmentId))]
    public Department? Department { get; set; }

    [ForeignKey(nameof(DoctorId))]
    public User? Doctor { get; set; }

    public ICollection<BloodRequestItem> Items { get; set; } = new List<BloodRequestItem>();
    public ICollection<BloodAllocation> Allocations { get; set; } = new List<BloodAllocation>();
}

[Table("blood_request_items")]
public class BloodRequestItem
{
    [Key]
    [Column("request_item_id")]
    public int RequestItemId { get; set; }

    [Column("request_id")]
    public int RequestId { get; set; }

    [Column("component_type_id")]
    public int ComponentTypeId { get; set; }

    [Column("requested_units")]
    public int RequestedUnits { get; set; }

    [Column("allocated_units")]
    public int AllocatedUnits { get; set; } = 0;

    // Navigation properties
    [ForeignKey(nameof(RequestId))]
    public BloodRequest? Request { get; set; }

    [ForeignKey(nameof(ComponentTypeId))]
    public BloodComponentType? ComponentType { get; set; }
}

[Table("blood_allocations")]
public class BloodAllocation
{
    [Key]
    [Column("allocation_id")]
    public int AllocationId { get; set; }

    [Column("request_id")]
    public int RequestId { get; set; }

    [Column("bag_id")]
    public int BagId { get; set; }

    [Column("reserved_by")]
    public int ReservedBy { get; set; }

    [Column("reserved_at")]
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

    [Column("issued_by")]
    public int? IssuedBy { get; set; }

    [Column("received_by")]
    public int? ReceivedBy { get; set; }

    [Column("issued_at")]
    public DateTime? IssuedAt { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "RESERVED";

    // Navigation properties
    [ForeignKey(nameof(RequestId))]
    public BloodRequest? Request { get; set; }

    [ForeignKey(nameof(BagId))]
    public BloodBag? Bag { get; set; }

    [ForeignKey(nameof(ReservedBy))]
    public User? ReservedByUser { get; set; }

    [ForeignKey(nameof(IssuedBy))]
    public User? IssuedByUser { get; set; }

    [ForeignKey(nameof(ReceivedBy))]
    public User? ReceivedByUser { get; set; }

    public BloodReturn? BloodReturn { get; set; }
}

[Table("blood_returns")]
public class BloodReturn
{
    [Key]
    [Column("return_id")]
    public int ReturnId { get; set; }

    [Column("allocation_id")]
    public int AllocationId { get; set; }

    [Column("returned_by")]
    public int ReturnedBy { get; set; }

    [Column("received_by")]
    public int ReceivedBy { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("return_reason")]
    public string ReturnReason { get; set; } = string.Empty;

    [Column("cold_chain_qualified")]
    public bool ColdChainQualified { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("final_action")]
    public string FinalAction { get; set; } = string.Empty;

    [Column("processed_at")]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(AllocationId))]
    public BloodAllocation? Allocation { get; set; }

    [ForeignKey(nameof(ReturnedBy))]
    public User? ReturnedByUser { get; set; }

    [ForeignKey(nameof(ReceivedBy))]
    public User? ReceivedByUser { get; set; }
}

#endregion

#region NHÓM E: AUDIT & COMPLIANCE

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("log_id")]
    public long LogId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("table_name")]
    public string TableName { get; set; } = string.Empty;

    [Column("record_id")]
    public int RecordId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("action_type")]
    public string ActionType { get; set; } = string.Empty;

    [Column("old_state", TypeName = "jsonb")]
    public string? OldState { get; set; }

    [Required]
    [Column("new_state", TypeName = "jsonb")]
    public string NewState { get; set; } = "{}";

    [Column("performed_by")]
    public int? PerformedBy { get; set; }

    [MaxLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [MaxLength(255)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [MaxLength(255)]
    [Column("change_reason")]
    public string? ChangeReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(PerformedBy))]
    public User? PerformedByUser { get; set; }
}

[Table("notifications")]
public class Notification
{
    [Key]
    [Column("notification_id")]
    public long NotificationId { get; set; }

    [Column("recipient_user_id")]
    public int? RecipientUserId { get; set; }

    [Column("target_role_id")]
    public int? TargetRoleId { get; set; }

    [Column("target_department_id")]
    public int? TargetDepartmentId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    [Column("type")]
    public string Type { get; set; } = "INFO";

    [MaxLength(50)]
    [Column("reference_type")]
    public string? ReferenceType { get; set; }

    [Column("reference_id")]
    public int? ReferenceId { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(RecipientUserId))]
    public User? RecipientUser { get; set; }

    [ForeignKey(nameof(TargetRoleId))]
    public Role? TargetRole { get; set; }

    [ForeignKey(nameof(TargetDepartmentId))]
    public Department? TargetDepartment { get; set; }
}

#endregion
