using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuyetMach175.Api.Data.Entities;

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
