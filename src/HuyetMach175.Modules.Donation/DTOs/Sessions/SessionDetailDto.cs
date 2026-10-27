using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Donation.DTOs.Sessions;

public class SessionDetailDto
{
    [JsonPropertyName("sessionId")]
    public int SessionId { get; set; }

    [JsonPropertyName("appointmentId")]
    public int AppointmentId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("targetVolumeMl")]
    public string TargetVolumeMl { get; set; } = string.Empty;

    [JsonPropertyName("actualVolumeMl")]
    public int? ActualVolumeMl { get; set; }

    [JsonPropertyName("collectionIncident")]
    public string? CollectionIncident { get; set; }

    [JsonPropertyName("phlebotomistId")]
    public int? PhlebotomistId { get; set; }

    [JsonPropertyName("phlebotomistName")]
    public string? PhlebotomistName { get; set; }

    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("bloodBagBarcode")]
    public string? BloodBagBarcode { get; set; }

    [JsonPropertyName("donor")]
    public SessionDonorDto Donor { get; set; } = new();

    [JsonPropertyName("screeningSummary")]
    public SessionScreeningSummaryDto ScreeningSummary { get; set; } = new();
}

public class SessionDonorDto
{
    [JsonPropertyName("donorId")]
    public int DonorId { get; set; }

    [JsonPropertyName("donorCode")]
    public string DonorCode { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("citizenId")]
    public string CitizenId { get; set; } = string.Empty;

    [JsonPropertyName("gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonPropertyName("bloodType")]
    public string? BloodType { get; set; }

    [JsonPropertyName("rhFactor")]
    public string? RhFactor { get; set; }
}

public class SessionScreeningSummaryDto
{
    [JsonPropertyName("weightKg")]
    public decimal? WeightKg { get; set; }

    [JsonPropertyName("bloodPressure")]
    public string? BloodPressure { get; set; }

    [JsonPropertyName("pulse")]
    public int? Pulse { get; set; }

    [JsonPropertyName("hemoglobinLevel")]
    public decimal? HemoglobinLevel { get; set; }

    [JsonPropertyName("clinicalNotes")]
    public string? ClinicalNotes { get; set; }

    [JsonPropertyName("doctorName")]
    public string? DoctorName { get; set; }

    [JsonPropertyName("screenedAt")]
    public DateTime? ScreenedAt { get; set; }
}
