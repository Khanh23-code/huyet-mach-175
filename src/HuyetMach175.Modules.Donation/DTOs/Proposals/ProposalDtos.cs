using System.ComponentModel.DataAnnotations;
using HuyetMach175.Modules.Donation.DTOs.Sites;

namespace HuyetMach175.Modules.Donation.DTOs.Proposals;

public class CreateProposalRequest
{
    [Required(ErrorMessage = "Loại đề xuất không được để trống.")]
    public string ProposalType { get; set; } = string.Empty;

    public int? SiteId { get; set; }

    [StringLength(150, ErrorMessage = "Tên điểm hiến không được vượt quá 150 ký tự.")]
    public string? SiteName { get; set; }

    public string? SiteType { get; set; }

    [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự.")]
    public string? Address { get; set; }

    [StringLength(20, ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Cần ít nhất 1 khung giờ.")]
    public List<SlotInput> Slots { get; set; } = new();
}

public class RejectProposalRequest
{
    [Required(ErrorMessage = "Vui lòng nhập lý do từ chối.")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "Lý do từ chối phải từ 5 đến 255 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}

public class ProposalFilterDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Số trang (PageNumber) phải từ 1 trở lên.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "Kích thước trang (PageSize) phải từ 1 đến 100.")]
    public int PageSize { get; set; } = 10;

    public string? Status { get; set; }
}

public class ProposalDto
{
    public int ProposalId { get; set; }
    public string ProposalCode { get; set; } = string.Empty;
    public string ProposalType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string SiteType { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public List<SlotDto> Slots { get; set; } = new();
    public int TotalCapacityPerDay { get; set; }
    public List<SlotDto> CurrentSlots { get; set; } = new();
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string? CreatedByRoleCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
}
