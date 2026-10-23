using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Donors;

public class DonorDetailDto
{
    public int DonorId { get; set; }
    public string CitizenId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public BloodType? BloodType { get; set; }
    public RhFactor? RhFactor { get; set; }
    public int TotalDonations { get; set; }
    public DateOnly? LastDonationDate { get; set; }
    public List<DonorHistoryItemDto> DonationHistory { get; set; } = new();
}