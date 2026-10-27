using System.Text.RegularExpressions;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Donors;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Donation.Services;

public class DonorsService : IDonorsService
{
    private readonly ApplicationDbContext _context;

    public DonorsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<DonorListItemDto>> GetDonorsAsync(DonorFilterDto filter)
    {
        if (filter.Page < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (page) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.PageSize < 1 || filter.PageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.");
        }

        var query = _context.Donors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(d =>
                EF.Functions.ILike(d.CitizenId, $"%{kw}%") ||
                EF.Functions.ILike(d.FullName, $"%{kw}%") ||
                EF.Functions.ILike(d.PhoneNumber, $"%{kw}%"));
        }

        if (filter.BloodType.HasValue)
        {
            query = query.Where(d => d.BloodType == filter.BloodType.Value);
        }

        if (filter.RhFactor.HasValue)
        {
            query = query.Where(d => d.RhFactor == filter.RhFactor.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(d => d.DonorId)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(d => new DonorListItemDto
            {
                DonorId = d.DonorId,
                CitizenId = d.CitizenId,
                FullName = d.FullName,
                DateOfBirth = d.DateOfBirth,
                Gender = d.Gender,
                PhoneNumber = d.PhoneNumber,
                Email = d.Email,
                BloodType = d.BloodType,
                RhFactor = d.RhFactor,
                TotalDonations = d.TotalDonations,
                LastDonationDate = d.LastDonationDate
            })
            .ToListAsync();

        return new PagedResult<DonorListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DonorDetailDto> GetDonorByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID người hiến máu không hợp lệ.");
        }

        var donor = await _context.Donors
            .AsNoTracking()
            .Where(d => d.DonorId == id)
            .Select(d => new DonorDetailDto
            {
                DonorId = d.DonorId,
                CitizenId = d.CitizenId,
                FullName = d.FullName,
                DateOfBirth = d.DateOfBirth,
                Gender = d.Gender,
                PhoneNumber = d.PhoneNumber,
                Email = d.Email,
                BloodType = d.BloodType,
                RhFactor = d.RhFactor,
                TotalDonations = d.TotalDonations,
                LastDonationDate = d.LastDonationDate,
                DonationHistory = d.Appointments
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenByDescending(a => a.AppointmentId)
                    .Select(a => new DonorHistoryItemDto
                    {
                        AppointmentId = a.AppointmentId,
                        AppointmentDate = a.AppointmentDate,
                        TimeSlot = a.TimeSlot,
                        CampaignId = a.CampaignId,
                        CampaignName = a.Campaign != null ? a.Campaign.CampaignName : "Hiến tại bệnh viện",
                        Status = a.Status,
                        IsWalkIn = a.IsWalkIn,
                        ActualVolumeMl = a.DonationSession != null ? a.DonationSession.ActualVolumeMl : null,
                        SessionStatus = a.DonationSession != null ? a.DonationSession.Status : null,
                        ScreeningStatus = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.ScreeningStatus : null,
                        DeferralReason = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.DeferralReason : null,
                        CompletedAt = a.DonationSession != null ? a.DonationSession.CompletedAt : null
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (donor == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy người hiến máu với mã #{id}.");
        }

        return donor;
    }

    public async Task<DonorEligibilityDto> CheckEligibilityAsync(string citizenId)
    {
        if (string.IsNullOrWhiteSpace(citizenId))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Vui lòng cung cấp số CCCD cần kiểm tra.");
        }

        var trimmedCitizenId = citizenId.Trim();
        if (!Regex.IsMatch(trimmedCitizenId, @"^[0-9]{12}$"))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số CCCD không hợp lệ. Số CCCD phải gồm đúng 12 chữ số.");
        }

        var donor = await _context.Donors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.CitizenId == trimmedCitizenId);

        if (donor == null)
        {
            return new DonorEligibilityDto
            {
                CitizenId = trimmedCitizenId,
                DonorId = null,
                FullName = null,
                DateOfBirth = null,
                Age = null,
                Gender = null,
                PhoneNumber = null,
                BloodType = null,
                RhFactor = null,
                TotalDonations = 0,
                HasDonatedBefore = false,
                LastDonationDate = null,
                NextEligibleDate = null,
                DaysRemaining = 0,
                IsEligible = true,
                IsAgeQualified = true,
                Message = "Người hiến máu mới, chưa có hồ sơ trong hệ thống. Đủ điều kiện đăng ký hiến máu."
            };
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        int? age = null;
        bool isAgeQualified = true;
        if (donor.DateOfBirth != default)
        {
            age = today.Year - donor.DateOfBirth.Year;
            if (donor.DateOfBirth > today.AddYears(-age.Value))
            {
                age--;
            }
        }

        if (age < 18 || age > 60)
        {
            isAgeQualified = false;
        }

        bool hasDonatedBefore = donor.TotalDonations > 0 && donor.LastDonationDate.HasValue;
        DateOnly? nextEligibleDate = null;
        int daysRemaining = 0;
        bool isDateEligible = true;

        if (hasDonatedBefore)
        {
            nextEligibleDate = donor.LastDonationDate!.Value.AddDays(84);
            var diff = nextEligibleDate.Value.DayNumber - today.DayNumber;
            if (diff > 0)
            {
                daysRemaining = diff;
                isDateEligible = false;
            }
        }

        bool isEligible = isAgeQualified && isDateEligible;

        string message;
        if (!isAgeQualified)
        {
            message = $"Người hiến máu hiện tại {age} tuổi, không nằm trong độ tuổi quy định hiến máu (18 - 60 tuổi theo Thông tư 26/2013/TT-BYT).";
        }
        else if (!isDateEligible)
        {
            message = $"Chưa đủ khoảng cách 84 ngày kể từ lần hiến gần nhất ({donor.LastDonationDate!.Value:dd/MM/yyyy}). Cần chờ thêm {daysRemaining} ngày (ngày sớm nhất có thể hiến là {nextEligibleDate!.Value:dd/MM/yyyy}).";
        }
        else
        {
            message = hasDonatedBefore
                ? $"Đã đủ khoảng cách 84 ngày kể từ lần hiến gần nhất ({donor.LastDonationDate!.Value:dd/MM/yyyy}). Đủ điều kiện hiến máu."
                : "Người hiến máu chưa có lịch sử hiến máu trước đây. Đủ điều kiện hiến máu.";
        }

        return new DonorEligibilityDto
        {
            CitizenId = donor.CitizenId,
            DonorId = donor.DonorId,
            FullName = donor.FullName,
            DateOfBirth = donor.DateOfBirth,
            Age = age,
            Gender = donor.Gender,
            PhoneNumber = donor.PhoneNumber,
            BloodType = donor.BloodType,
            RhFactor = donor.RhFactor,
            TotalDonations = donor.TotalDonations,
            HasDonatedBefore = hasDonatedBefore,
            LastDonationDate = donor.LastDonationDate,
            NextEligibleDate = nextEligibleDate,
            DaysRemaining = daysRemaining,
            IsEligible = isEligible,
            IsAgeQualified = isAgeQualified,
            Message = message
        };
    }

    public async Task<DonorDetailDto> CreateDonorAsync(CreateDonorRequest request)
    {
        var citizenId = request.CitizenId.Trim();
        if (!Regex.IsMatch(citizenId, @"^[0-9]{12}$"))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số CCCD không hợp lệ. Số CCCD phải gồm đúng 12 chữ số.");
        }

        var phone = request.PhoneNumber.Trim();
        if (!Regex.IsMatch(phone, @"^0\d{9}$"))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số điện thoại không hợp lệ (phải gồm 10 chữ số, bắt đầu bằng 0).");
        }

        if (await _context.Donors.AnyAsync(d => d.CitizenId == citizenId))
        {
            throw new HttpException(StatusCodes.Status409Conflict, "Số CCCD này đã tồn tại trong hệ thống.");
        }

        if (await _context.Donors.AnyAsync(d => d.PhoneNumber == phone))
        {
            throw new HttpException(StatusCodes.Status409Conflict, "Số điện thoại này đã được sử dụng bởi người hiến khác.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - request.DateOfBirth.Year;
        if (request.DateOfBirth > today.AddYears(-age))
        {
            age--;
        }

        if (age < 18 || age > 60)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                $"Người hiến máu phải trong độ tuổi từ 18 đến 60 theo quy định (hiện tại {age} tuổi).");
        }

        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        var donor = new Donor
        {
            CitizenId = citizenId,
            FullName = request.FullName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender.Trim(),
            PhoneNumber = phone,
            Email = email,
            BloodType = null,
            RhFactor = null,
            TotalDonations = 0,
            LastDonationDate = null
        };

        _context.Donors.Add(donor);
        await _context.SaveChangesAsync();

        return await GetDonorByIdAsync(donor.DonorId);
    }

    public async Task<DonorDetailDto> UpdateDonorAsync(int id, UpdateDonorRequest request)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID người hiến máu không hợp lệ.");
        }

        var donor = await _context.Donors.FindAsync(id);
        if (donor == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy người hiến máu với mã #{id}.");
        }

        var phone = request.PhoneNumber.Trim();
        if (!Regex.IsMatch(phone, @"^0\d{9}$"))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số điện thoại không hợp lệ (phải gồm 10 chữ số, bắt đầu bằng 0).");
        }

        if (await _context.Donors.AnyAsync(d => d.PhoneNumber == phone && d.DonorId != id))
        {
            throw new HttpException(StatusCodes.Status409Conflict, "Số điện thoại này đã được sử dụng bởi người hiến khác.");
        }

        if (!string.IsNullOrWhiteSpace(request.CitizenId))
        {
            var trimmedCitizenId = request.CitizenId.Trim();
            if (!Regex.IsMatch(trimmedCitizenId, @"^[0-9]{12}$"))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Số CCCD không hợp lệ. Số CCCD phải gồm đúng 12 chữ số.");
            }

            if (trimmedCitizenId != donor.CitizenId)
            {
                if (await _context.Donors.AnyAsync(d => d.CitizenId == trimmedCitizenId && d.DonorId != id))
                {
                    throw new HttpException(StatusCodes.Status409Conflict, "Số CCCD này đã tồn tại trong hệ thống.");
                }
                donor.CitizenId = trimmedCitizenId;
            }
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - request.DateOfBirth.Year;
        if (request.DateOfBirth > today.AddYears(-age))
        {
            age--;
        }

        if (age < 18 || age > 60)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                $"Người hiến máu phải trong độ tuổi từ 18 đến 60 theo quy định (hiện tại {age} tuổi).");
        }

        donor.FullName = request.FullName.Trim();
        donor.DateOfBirth = request.DateOfBirth;
        donor.Gender = request.Gender.Trim();
        donor.PhoneNumber = phone;
        donor.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        await _context.SaveChangesAsync();

        return await GetDonorByIdAsync(donor.DonorId);
    }
}
