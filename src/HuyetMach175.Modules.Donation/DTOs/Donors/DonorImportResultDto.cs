namespace HuyetMach175.Modules.Donation.DTOs.Donors;

public class DonorImportResultDto
{
    public int CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int CreatedDonorsCount { get; set; }
    public int ReusedDonorsCount { get; set; }
    public int CreatedAppointmentsCount { get; set; }
    public int SurveysIncludedCount { get; set; }
    public int ErrorsCount { get; set; }
    public List<DonorImportRowErrorDto> Errors { get; set; } = new List<DonorImportRowErrorDto>();
}

public class DonorImportRowErrorDto
{
    public int RowNumber { get; set; }
    public int Stt { get; set; }
    public string? CitizenId { get; set; }
    public string? FullName { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}