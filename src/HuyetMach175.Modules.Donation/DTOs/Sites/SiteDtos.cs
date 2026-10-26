using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Donation.DTOs.Sites;

public class SlotDto
{
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int Capacity { get; set; }
}

public class SlotInput
{
    [Required(ErrorMessage = "Giờ bắt đầu không được để trống.")]
    public string StartTime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Giờ kết thúc không được để trống.")]
    public string EndTime { get; set; } = string.Empty;

    [Range(1, 500, ErrorMessage = "Sức chứa mỗi slot phải từ 1 đến 500 người.")]
    public int Capacity { get; set; }
}

public class SiteDto
{
    public int SiteId { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string SiteType { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int TotalCapacityPerDay { get; set; }
    public List<SlotDto> Slots { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
