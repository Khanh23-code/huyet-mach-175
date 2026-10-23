using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Donors;

namespace HuyetMach175.Modules.Donation.Services;

public interface IDonorsService
{
    Task<PagedResult<DonorListItemDto>> GetDonorsAsync(DonorFilterDto filter);
}