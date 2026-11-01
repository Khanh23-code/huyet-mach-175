using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Donation.DTOs.Campaigns;
using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.Mappings;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Donation.Services;

public class CampaignsService : ICampaignsService
{
    private readonly ApplicationDbContext _context;
    private readonly ICampaignValidator _validator;

    public CampaignsService(ApplicationDbContext context, ICampaignValidator validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<PagedResult<CampaignListItemDto>> GetCampaignsAsync(CampaignFilterDto filter, int? createdBy = null)
    {
        if (filter.PageNumber < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (PageNumber) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.PageSize < 1 || filter.PageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (PageSize) phải nằm trong khoảng từ 1 đến 100.");
        }

        if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate.Value > filter.ToDate.Value)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                "Khoảng thời gian không hợp lệ: 'Từ ngày' (FromDate) phải nhỏ hơn hoặc bằng 'Đến ngày' (ToDate).");
        }

        var query = _context.DonationCampaigns.AsNoTracking();

        if (createdBy.HasValue)
        {
            query = query.Where(c => c.CreatedBy == createdBy.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(c => c.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(c => c.EndDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(c => c.StartDate <= filter.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim();
            query = query.Where(c => EF.Functions.ILike(c.CampaignName, $"%{kw}%") || EF.Functions.ILike(c.LocationName, $"%{kw}%"));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(CampaignMappings.ProjectToListItem)
            .ToListAsync();

        return new PagedResult<CampaignListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public Task<PagedResult<CampaignListItemDto>> GetMyCampaignsAsync(CampaignFilterDto filter, int userId)
    {
        if (userId <= 0)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Người dùng không hợp lệ.");
        }

        return GetCampaignsAsync(filter, createdBy: userId);
    }

    public async Task<CampaignDetailDto> GetCampaignByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns
            .AsNoTracking()
            .Include(c => c.CreatedByUser)
            .Include(c => c.ApprovedByUser)
            .Include(c => c.CancelledByUser)
            .Include(c => c.Slots)
                .ThenInclude(s => s.Appointments)
                    .ThenInclude(a => a.DonationSession)
            .FirstOrDefaultAsync(c => c.CampaignId == id);

        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        return campaign.ToDetailDto();
    }

    public async Task<CampaignSummaryDto> GetCampaignSummaryAsync(int id)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns
            .AsNoTracking()
            .Include(c => c.CreatedByUser)
            .Include(c => c.Slots)
            .FirstOrDefaultAsync(c => c.CampaignId == id);

        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        _validator.ValidateSummarizable(campaign);

        var appointments = await _context.DonationAppointments
            .AsNoTracking()
            .Include(a => a.Donor)
            .Include(a => a.PreScreeningSurvey)
            .Include(a => a.DonationSession)
            .Include(a => a.CampaignSlot)
            .Where(a => a.CampaignSlot != null && a.CampaignSlot.CampaignId == id)
            .ToListAsync();

        var sessions = appointments
            .Where(a => a.DonationSession != null)
            .Select(a => a.DonationSession!)
            .ToList();

        var completedSessions = sessions
            .Where(s => s.Status == SessionStatus.COMPLETED)
            .ToList();

        var totalCollectedUnits = completedSessions.Count;
        var shortfallUnits = Math.Max(0, campaign.TargetDonations - totalCollectedUnits);
        var exceededUnits = Math.Max(0, totalCollectedUnits - campaign.TargetDonations);
        var completionRate = campaign.TargetDonations > 0
            ? Math.Round((double)totalCollectedUnits / campaign.TargetDonations * 100, 2)
            : 0;

        var totalVolumeMl = completedSessions.Sum(s => s.ActualVolumeMl ?? (s.TargetVolumeMl == TargetVolume.VOL_250 ? 250 : s.TargetVolumeMl == TargetVolume.VOL_350 ? 350 : 450));

        var totalRegistered = appointments.Count(a => a.Status != AppointmentStatus.CANCELLED);
        var totalCheckedIn = appointments.Count(a => a.CheckedInAt != null || a.Status == AppointmentStatus.CHECKED_IN);
        var scheduledCheckedIn = appointments.Count(a => !a.IsWalkIn && (a.CheckedInAt != null || a.Status == AppointmentStatus.CHECKED_IN));
        var walkInCheckedIn = appointments.Count(a => a.IsWalkIn && (a.CheckedInAt != null || a.Status == AppointmentStatus.CHECKED_IN));
        var pendingArrival = appointments.Count(a => a.Status == AppointmentStatus.BOOKED && a.CheckedInAt == null);
        var noShowCount = appointments.Count(a => a.Status == AppointmentStatus.NO_SHOW);
        var cancelledCount = appointments.Count(a => a.Status == AppointmentStatus.CANCELLED);
        var attendanceRate = totalRegistered > 0
            ? Math.Round((double)totalCheckedIn / totalRegistered * 100, 2)
            : 0;

        var surveys = appointments
            .Where(a => a.PreScreeningSurvey != null)
            .Select(a => a.PreScreeningSurvey!)
            .ToList();

        var totalScreened = surveys.Count(s => s.ScreeningStatus != ScreeningStatus.PENDING);
        var qualifiedCount = surveys.Count(s => s.ScreeningStatus == ScreeningStatus.QUALIFIED);
        var deferredCount = surveys.Count(s => s.ScreeningStatus == ScreeningStatus.DEFERRED);
        var unqualifiedCount = surveys.Count(s => s.ScreeningStatus == ScreeningStatus.UNQUALIFIED);
        var pendingScreeningCount = appointments.Count(a => (a.CheckedInAt != null || a.Status == AppointmentStatus.CHECKED_IN) && (a.PreScreeningSurvey == null || a.PreScreeningSurvey.ScreeningStatus == ScreeningStatus.PENDING));

        var topDeferralReasons = surveys
            .Where(s => s.ScreeningStatus == ScreeningStatus.DEFERRED && !string.IsNullOrWhiteSpace(s.DeferralReason))
            .GroupBy(s => s.DeferralReason!.Trim())
            .Select(g => new CampaignDeferralReasonStatDto
            {
                Reason = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        var totalSessions = sessions.Count;
        var abortedSessions = sessions.Count(s => s.Status == SessionStatus.ABORTED);
        var waitingCollectionSessions = sessions.Count(s => s.Status == SessionStatus.WAITING_COLLECTION);

        var incidentSessions = sessions
            .Where(s => s.Status == SessionStatus.ABORTED || !string.IsNullOrWhiteSpace(s.CollectionIncident))
            .ToList();

        var incidents = incidentSessions.Select(s => new CampaignIncidentDto
        {
            SessionId = s.SessionId,
            AppointmentId = s.AppointmentId,
            DonorName = s.Appointment?.Donor?.FullName ?? string.Empty,
            DonorCitizenId = s.Appointment?.Donor?.CitizenId ?? string.Empty,
            TargetVolumeMl = s.TargetVolumeMl.ToString(),
            ActualVolumeMl = s.ActualVolumeMl,
            IncidentDescription = !string.IsNullOrWhiteSpace(s.CollectionIncident)
                ? s.CollectionIncident!
                : (s.Status == SessionStatus.ABORTED ? "Phiên lấy máu bị hủy giữa chừng" : string.Empty),
            CompletedAt = s.CompletedAt ?? s.CreatedAt
        }).ToList();

        var volumeGroups = completedSessions
            .GroupBy(s => s.TargetVolumeMl)
            .ToDictionary(g => g.Key, g => g.ToList());

        var volumeBreakdown = new List<CampaignVolumeStatDto>();
        foreach (TargetVolume volEnum in Enum.GetValues<TargetVolume>())
        {
            var count = volumeGroups.TryGetValue(volEnum, out var list) ? list.Count : 0;
            var mlVal = volEnum == TargetVolume.VOL_250 ? 250 : volEnum == TargetVolume.VOL_350 ? 350 : 450;
            var totalMl = volumeGroups.TryGetValue(volEnum, out var mlList)
                ? mlList.Sum(s => s.ActualVolumeMl ?? mlVal)
                : 0;

            volumeBreakdown.Add(new CampaignVolumeStatDto
            {
                VolumeType = volEnum.ToString(),
                VolumeMl = mlVal,
                Count = count,
                TotalMl = totalMl
            });
        }

        var bloodTypeBreakdown = new List<CampaignBloodTypeStatDto>();
        var bloodTypeGroups = completedSessions
            .GroupBy(s => s.Appointment?.Donor?.BloodType)
            .ToList();

        foreach (var bg in bloodTypeGroups)
        {
            var typeName = bg.Key.HasValue ? bg.Key.Value.ToString() : "Chưa xác định";
            var count = bg.Count();
            var pct = totalCollectedUnits > 0 ? Math.Round((double)count / totalCollectedUnits * 100, 2) : 0;
            bloodTypeBreakdown.Add(new CampaignBloodTypeStatDto
            {
                BloodType = typeName,
                Count = count,
                Percentage = pct
            });
        }

        var rhBreakdown = new List<CampaignRhStatDto>();
        var rhGroups = completedSessions
            .GroupBy(s => s.Appointment?.Donor?.RhFactor)
            .ToList();

        foreach (var rg in rhGroups)
        {
            var rhName = rg.Key.HasValue ? rg.Key.Value.ToString() : "Chưa xác định";
            var count = rg.Count();
            var pct = totalCollectedUnits > 0 ? Math.Round((double)count / totalCollectedUnits * 100, 2) : 0;
            rhBreakdown.Add(new CampaignRhStatDto
            {
                RhFactor = rhName,
                Count = count,
                Percentage = pct
            });
        }

        var slotsSummary = campaign.Slots
            .OrderBy(s => s.StartTime)
            .Select(s =>
            {
                var slotApps = appointments.Where(a => a.SlotId == s.SlotId).ToList();
                var slotCollected = slotApps.Where(a => a.DonationSession != null && a.DonationSession.Status == SessionStatus.COMPLETED).ToList();
                var slotVolume = slotCollected.Sum(a => a.DonationSession!.ActualVolumeMl ?? (a.DonationSession.TargetVolumeMl == TargetVolume.VOL_250 ? 250 : a.DonationSession.TargetVolumeMl == TargetVolume.VOL_350 ? 350 : 450));

                return new CampaignSlotSummaryDto
                {
                    SlotId = s.SlotId,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Capacity = s.Capacity,
                    TotalRegistered = slotApps.Count(a => a.Status != AppointmentStatus.CANCELLED),
                    TotalCheckedIn = slotApps.Count(a => a.CheckedInAt != null || a.Status == AppointmentStatus.CHECKED_IN),
                    TotalCollected = slotCollected.Count,
                    TotalVolumeMl = slotVolume
                };
            })
            .ToList();

        return new CampaignSummaryDto
        {
            CampaignId = campaign.CampaignId,
            CampaignName = campaign.CampaignName,
            Description = campaign.Description,
            LocationName = campaign.LocationName,
            CampaignType = campaign.CampaignType,
            OperatingDays = campaign.OperatingDays,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            Status = campaign.Status,
            CreatedByName = campaign.CreatedByUser != null ? campaign.CreatedByUser.FullName : string.Empty,

            TargetDonations = campaign.TargetDonations,
            TotalCollectedUnits = totalCollectedUnits,
            ShortfallUnits = shortfallUnits,
            ExceededUnits = exceededUnits,
            CompletionRate = completionRate,
            TotalVolumeMl = totalVolumeMl,

            TotalRegistered = totalRegistered,
            TotalCheckedIn = totalCheckedIn,
            ScheduledCheckedIn = scheduledCheckedIn,
            WalkInCheckedIn = walkInCheckedIn,
            PendingArrival = pendingArrival,
            NoShowCount = noShowCount,
            CancelledCount = cancelledCount,
            AttendanceRate = attendanceRate,

            TotalScreened = totalScreened,
            QualifiedCount = qualifiedCount,
            DeferredCount = deferredCount,
            UnqualifiedCount = unqualifiedCount,
            PendingScreeningCount = pendingScreeningCount,
            TopDeferralReasons = topDeferralReasons,

            TotalSessions = totalSessions,
            CompletedSessions = completedSessions.Count,
            AbortedSessions = abortedSessions,
            WaitingCollectionSessions = waitingCollectionSessions,
            IncidentCount = incidents.Count,
            Incidents = incidents,

            VolumeBreakdown = volumeBreakdown,
            BloodTypeBreakdown = bloodTypeBreakdown,
            RhBreakdown = rhBreakdown,
            Slots = slotsSummary
        };
    }

    public async Task<CampaignDetailDto> CreateCampaignAsync(CreateCampaignRequest request, int userId)
    {
        _validator.ValidateDates(request.StartDate, request.EndDate, isNew: true);
        _validator.ValidateTargetDonations(request.TargetDonations);

        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Người dùng không hợp lệ hoặc đã bị vô hiệu hóa.");
        }

        var campaign = new DonationCampaign
        {
            CampaignName = request.CampaignName.Trim(),
            Description = request.Description?.Trim(),
            LocationName = request.LocationName.Trim(),
            CampaignType = request.CampaignType,
            OperatingDays = request.CampaignType == CampaignType.RECURRING ? request.OperatingDays?.Trim() : null,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TargetDonations = request.TargetDonations,
            MaxCapacity = request.MaxCapacity,
            Status = CampaignStatus.DRAFT,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        if (request.Slots != null && request.Slots.Any())
        {
            foreach (var slot in request.Slots)
            {
                campaign.Slots.Add(new DonationCampaignSlot
                {
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    Capacity = slot.Capacity > 0 ? slot.Capacity : 20
                });
            }
        }

        _context.DonationCampaigns.Add(campaign);
        await _context.SaveChangesAsync();

        return await GetCampaignByIdAsync(campaign.CampaignId);
    }

    public async Task<CampaignDetailDto> UpdateCampaignAsync(int id, UpdateCampaignRequest request)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns
            .Include(c => c.Slots)
            .FirstOrDefaultAsync(c => c.CampaignId == id);

        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        _validator.ValidateEditable(campaign);
        _validator.ValidateDates(request.StartDate, request.EndDate, isNew: false);
        _validator.ValidateTargetDonations(request.TargetDonations);

        campaign.CampaignName = request.CampaignName.Trim();
        campaign.Description = request.Description?.Trim();
        campaign.LocationName = request.LocationName.Trim();
        campaign.CampaignType = request.CampaignType;
        campaign.OperatingDays = request.CampaignType == CampaignType.RECURRING ? request.OperatingDays?.Trim() : null;
        campaign.StartDate = request.StartDate;
        campaign.EndDate = request.EndDate;
        campaign.TargetDonations = request.TargetDonations;
        campaign.MaxCapacity = request.MaxCapacity;

        if (request.Slots != null)
        {
            _context.DonationCampaignSlots.RemoveRange(campaign.Slots);
            foreach (var slot in request.Slots)
            {
                campaign.Slots.Add(new DonationCampaignSlot
                {
                    CampaignId = campaign.CampaignId,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    Capacity = slot.Capacity > 0 ? slot.Capacity : 20
                });
            }
        }

        await _context.SaveChangesAsync();

        return await GetCampaignByIdAsync(campaign.CampaignId);
    }

    public async Task<CampaignDetailDto> SubmitForApprovalAsync(int id)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns.FindAsync(id);
        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        _validator.ValidateSubmittable(campaign);

        campaign.Status = CampaignStatus.PENDING_APPROVAL;
        campaign.RejectionReason = null;

        await _context.SaveChangesAsync();

        return await GetCampaignByIdAsync(campaign.CampaignId);
    }

    public async Task<CampaignDetailDto> ApproveCampaignAsync(int id, ApproveCampaignRequest request, int userId)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns.FindAsync(id);
        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        _validator.ValidateApprovable(campaign, request.IsApproved, request.RejectionReason);

        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Người duyệt không hợp lệ hoặc đã bị vô hiệu hóa.");
        }

        if (request.IsApproved)
        {
            campaign.Status = CampaignStatus.APPROVED;
            campaign.ApprovedBy = userId;
            campaign.ApprovedAt = DateTime.UtcNow;
            campaign.RejectionReason = null;
        }
        else
        {
            campaign.Status = CampaignStatus.REJECTED;
            campaign.ApprovedBy = userId;
            campaign.ApprovedAt = DateTime.UtcNow;
            campaign.RejectionReason = request.RejectionReason!.Trim();
        }

        await _context.SaveChangesAsync();

        return await GetCampaignByIdAsync(campaign.CampaignId);
    }

    public async Task<CampaignDetailDto> CancelCampaignAsync(int id, CancelCampaignRequest request, int userId)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns.FindAsync(id);
        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        _validator.ValidateCancellable(campaign, request.Reason);

        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Người thực hiện không hợp lệ hoặc đã bị vô hiệu hóa.");
        }

        campaign.Status = CampaignStatus.CANCELLED;
        campaign.CancelledBy = userId;
        campaign.CancelledAt = DateTime.UtcNow;
        campaign.CancellationReason = request.Reason.Trim();

        await _context.SaveChangesAsync();

        return await GetCampaignByIdAsync(campaign.CampaignId);
    }

    public async Task<CampaignDetailDto> UpdateCampaignStatusAsync(int id, UpdateCampaignStatusRequest request, int userId)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns.FindAsync(id);
        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        _validator.ValidateStatusTransition(campaign, request.Status);

        campaign.Status = request.Status;

        await _context.SaveChangesAsync();

        return await GetCampaignByIdAsync(campaign.CampaignId);
    }

    public async Task DeleteCampaignAsync(int id)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns
            .Include(c => c.Slots)
                .ThenInclude(s => s.Appointments)
            .FirstOrDefaultAsync(c => c.CampaignId == id);

        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        _validator.ValidateDeletable(campaign);

        _context.DonationCampaignSlots.RemoveRange(campaign.Slots);
        _context.DonationCampaigns.Remove(campaign);
        await _context.SaveChangesAsync();
    }
}