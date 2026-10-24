using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

/// <summary>Đề xuất tạo điểm hiến mới / đổi lịch &amp; sức chứa, chờ Quản lý (MGT) duyệt.</summary>
[Table("site_proposals")]
public class SiteProposal
{
    [Key]
    [Column("proposal_id")]
    public int ProposalId { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("proposal_code")]
    public string ProposalCode { get; set; } = string.Empty;

    [Column("proposal_type")]
    public ProposalType ProposalType { get; set; }

    [Column("status")]
    public ProposalStatus Status { get; set; } = ProposalStatus.PENDING;

    /// <summary>NEW_SITE: null cho tới khi duyệt. UPDATE_SCHEDULE: điểm hiến được đổi lịch.</summary>
    [Column("site_id")]
    public int? SiteId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("site_name")]
    public string SiteName { get; set; } = string.Empty;

    [Column("site_type")]
    public SiteType SiteType { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("address")]
    public string Address { get; set; } = string.Empty;

    [Required]
    [MaxLength(15)]
    [Column("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Cấu hình slot đề xuất, JSON: [{StartTime,EndTime,Capacity}].</summary>
    [Required]
    [Column("slots_json", TypeName = "jsonb")]
    public string SlotsJson { get; set; } = "[]";

    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("reviewed_by")]
    public int? ReviewedBy { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [MaxLength(255)]
    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }

    [ForeignKey(nameof(SiteId))]
    public DonationSite? Site { get; set; }

    [ForeignKey(nameof(CreatedBy))]
    public User? CreatedByUser { get; set; }

    [ForeignKey(nameof(ReviewedBy))]
    public User? ReviewedByUser { get; set; }
}
