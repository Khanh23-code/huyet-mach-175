using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Donation.DTOs.Campaigns;

namespace HuyetMach175.Modules.Donation.Mappings;

public static class CampaignSummaryCalculator
{
    public static CampaignSummaryDto CalculateSummary(DonationCampaign campaign, List<DonationAppointment> appointments)
    {
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

        var totalVolumeMl = completedSessions.Sum(s => s.ActualVolumeMl ?? ResolveTargetVolumeMl(s.TargetVolumeMl));

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

        var incidents = sessions
            .Where(s => s.Status == SessionStatus.ABORTED || !string.IsNullOrWhiteSpace(s.CollectionIncident))
            .Select(s => new CampaignIncidentDto
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
            })
            .ToList();

        var volumeBreakdown = BuildVolumeBreakdown(completedSessions);
        var bloodTypeBreakdown = BuildBloodTypeBreakdown(completedSessions, totalCollectedUnits);
        var rhBreakdown = BuildRhBreakdown(completedSessions, totalCollectedUnits);
        var slotsSummary = BuildSlotsSummary(campaign.Slots, appointments);

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

    private static int ResolveTargetVolumeMl(TargetVolume volume)
    {
        return volume switch
        {
            TargetVolume.VOL_250 => 250,
            TargetVolume.VOL_350 => 350,
            TargetVolume.VOL_450 => 450,
            _ => 350
        };
    }

    private static List<CampaignVolumeStatDto> BuildVolumeBreakdown(List<DonationSession> completedSessions)
    {
        var volumeGroups = completedSessions
            .GroupBy(s => s.TargetVolumeMl)
            .ToDictionary(g => g.Key, g => g.ToList());

        var volumeBreakdown = new List<CampaignVolumeStatDto>();
        foreach (TargetVolume volEnum in Enum.GetValues<TargetVolume>())
        {
            var count = volumeGroups.TryGetValue(volEnum, out var list) ? list.Count : 0;
            var mlVal = ResolveTargetVolumeMl(volEnum);
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
        return volumeBreakdown;
    }

    private static List<CampaignBloodTypeStatDto> BuildBloodTypeBreakdown(List<DonationSession> completedSessions, int totalCollectedUnits)
    {
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
        return bloodTypeBreakdown;
    }

    private static List<CampaignRhStatDto> BuildRhBreakdown(List<DonationSession> completedSessions, int totalCollectedUnits)
    {
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
        return rhBreakdown;
    }

    private static List<CampaignSlotSummaryDto> BuildSlotsSummary(IEnumerable<DonationCampaignSlot> slots, List<DonationAppointment> appointments)
    {
        return slots
            .OrderBy(s => s.StartTime)
            .Select(s =>
            {
                var slotApps = appointments.Where(a => a.SlotId == s.SlotId).ToList();
                var slotCollected = slotApps.Where(a => a.DonationSession != null && a.DonationSession.Status == SessionStatus.COMPLETED).ToList();
                var slotVolume = slotCollected.Sum(a => a.DonationSession!.ActualVolumeMl ?? ResolveTargetVolumeMl(a.DonationSession.TargetVolumeMl));

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
    }
}