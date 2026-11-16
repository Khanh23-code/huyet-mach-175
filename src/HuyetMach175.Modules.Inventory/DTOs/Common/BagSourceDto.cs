using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.Common;

public static class BagSourceTypes
{
    public const string Campaign = "CAMPAIGN";
    public const string WalkIn = "WALK_IN";
    public const string External = "EXTERNAL";
    public const string Unknown = "UNKNOWN";

    /// <summary>Giá trị hợp lệ cho bộ lọc (rỗng = tất cả).</summary>
    public static bool IsValidFilter(string? v) =>
        string.IsNullOrWhiteSpace(v) ||
        v.Trim().ToUpperInvariant() is Campaign or WalkIn or External;
}

public class BagSourceDto
{
    [JsonPropertyName("sourceType")] public string SourceType { get; set; } = BagSourceTypes.Unknown;
    [JsonPropertyName("sourceName")] public string SourceName { get; set; } = string.Empty;
    [JsonPropertyName("campaignId")] public int? CampaignId { get; set; }
    [JsonPropertyName("campaignName")] public string? CampaignName { get; set; }
    [JsonPropertyName("campaignLocation")] public string? CampaignLocation { get; set; }
    [JsonPropertyName("externalUnitName")] public string? ExternalUnitName { get; set; }
    [JsonPropertyName("externalCode")] public string? ExternalCode { get; set; }
    [JsonPropertyName("donor")] public BagDonorDto? Donor { get; set; }
    [JsonPropertyName("collection")] public BagCollectionDto? Collection { get; set; }
}

public class BagDonorDto
{
    [JsonPropertyName("donorId")] public int DonorId { get; set; }
    [JsonPropertyName("fullName")] public string FullName { get; set; } = string.Empty;
    [JsonPropertyName("citizenId")] public string CitizenId { get; set; } = string.Empty;
    [JsonPropertyName("gender")] public string Gender { get; set; } = string.Empty;
    [JsonPropertyName("dateOfBirth")] public DateOnly DateOfBirth { get; set; }
    [JsonPropertyName("age")] public int Age { get; set; }
    [JsonPropertyName("phoneNumber")] public string PhoneNumber { get; set; } = string.Empty;
    [JsonPropertyName("declaredBloodGroup")] public string? DeclaredBloodGroup { get; set; }
    [JsonPropertyName("totalDonations")] public int TotalDonations { get; set; }
    [JsonPropertyName("lastDonationDate")] public DateOnly? LastDonationDate { get; set; }
}

public class BagCollectionDto
{
    [JsonPropertyName("sessionId")] public int SessionId { get; set; }
    [JsonPropertyName("actualVolumeMl")] public int? ActualVolumeMl { get; set; }
    [JsonPropertyName("completedAt")] public DateTime? CompletedAt { get; set; }
    [JsonPropertyName("collectionIncident")] public string? CollectionIncident { get; set; }
}

public class BagLabSummaryDto
{
    [JsonPropertyName("testId")] public int TestId { get; set; }
    [JsonPropertyName("testedAt")] public DateTime TestedAt { get; set; }
    [JsonPropertyName("technicianName")] public string TechnicianName { get; set; } = string.Empty;
    [JsonPropertyName("hivResult")] public string HivResult { get; set; } = string.Empty;
    [JsonPropertyName("hbvResult")] public string HbvResult { get; set; } = string.Empty;
    [JsonPropertyName("hcvResult")] public string HcvResult { get; set; } = string.Empty;
    [JsonPropertyName("syphilisResult")] public string SyphilisResult { get; set; } = string.Empty;
    [JsonPropertyName("irregularAntibody")] public string IrregularAntibody { get; set; } = string.Empty;
    [JsonPropertyName("confirmedBloodGroup")] public string ConfirmedBloodGroup { get; set; } = string.Empty;
    [JsonPropertyName("overallConclusion")] public string OverallConclusion { get; set; } = string.Empty;
}