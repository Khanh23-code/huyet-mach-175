using HuyetMach175.Modules.Donation.DTOs.Donors;
using Microsoft.AspNetCore.Http;

namespace HuyetMach175.Modules.Donation.Services;

public interface IDonorImportService
{
    Task<(byte[] Content, string FileName)> GenerateTemplateAsync(int campaignId);
    Task<DonorImportResultDto> ImportDonorsAsync(int campaignId, IFormFile file);
}

