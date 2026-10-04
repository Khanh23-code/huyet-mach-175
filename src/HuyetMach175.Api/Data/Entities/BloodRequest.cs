using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

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

    [Column("doctor_id")]
    public int? DoctorId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("patient_code")]
    public string PatientCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("patient_name")]
    public string PatientName { get; set; } = string.Empty;

    [Column("patient_blood_type")]
    public BloodType PatientBloodType { get; set; }

    [Column("patient_rh")]
    public RhFactor PatientRh { get; set; }

    [Column("urgency_level")]
    public UrgencyLevel UrgencyLevel { get; set; }

    [MaxLength(255)]
    [Column("diagnosis")]
    public string? Diagnosis { get; set; }

    [Column("status")]
    public RequestStatus Status { get; set; } = RequestStatus.PENDING;

    [MaxLength(255)]
    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(DoctorId))]
    public User? Doctor { get; set; }

    public ICollection<BloodRequestItem> Items { get; set; } = new List<BloodRequestItem>();
    public ICollection<BloodAllocation> Allocations { get; set; } = new List<BloodAllocation>();
}
