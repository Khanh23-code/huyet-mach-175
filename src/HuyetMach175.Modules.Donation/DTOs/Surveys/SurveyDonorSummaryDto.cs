using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Donation.DTOs.Surveys;

public class SurveyDonorSummaryDto
{
    [JsonPropertyName("donorId")]
    public int DonorId { get; set; }

    [JsonPropertyName("donorCode")]
    public string DonorCode { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("citizenId")]
    public string CitizenId { get; set; } = string.Empty;

    [JsonPropertyName("dateOfBirth")]
    public DateOnly DateOfBirth { get; set; }

    [JsonPropertyName("age")]
    public int Age { get; set; }

    [JsonPropertyName("gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("bloodType")]
    public string? BloodType { get; set; }

    [JsonPropertyName("rhFactor")]
    public string? RhFactor { get; set; }

    [JsonPropertyName("totalDonations")]
    public int TotalDonations { get; set; }

    [JsonPropertyName("lastDonationDate")]
    public DateOnly? LastDonationDate { get; set; }

    [JsonPropertyName("isEligible84Days")]
    public bool IsEligible84Days { get; set; }
}
