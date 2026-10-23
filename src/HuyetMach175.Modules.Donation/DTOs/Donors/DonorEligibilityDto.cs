using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Donors;

public class DonorEligibilityDto
{
    public string CitizenId { get; set; } = string.Empty;
    public int? DonorId { get; set; }
    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public BloodType? BloodType { get; set; }
    public RhFactor? RhFactor { get; set; }
    public int TotalDonations { get; set; }
    public bool HasDonatedBefore { get; set; }
    public DateOnly? LastDonationDate { get; set; }
    public DateOnly? NextEligibleDate { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsEligible { get; set; }
    public bool IsAgeQualified { get; set; } = true;
    public string Message { get; set; } = string.Empty;
}