using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Inventory.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Inventory.Mappings;

public static class BloodBagSourceExtensions
{
    public static IQueryable<BloodBag> IncludeSource(this IQueryable<BloodBag> query) =>
        query
            .Include(b => b.Session)
                .ThenInclude(s => s!.Appointment)
                    .ThenInclude(a => a!.Donor)
            .Include(b => b.Session)
                .ThenInclude(s => s!.Appointment)
                    .ThenInclude(a => a!.CampaignSlot)
                        .ThenInclude(sl => sl!.Campaign);

    /// <summary>Lọc theo nguồn. Điều kiện phải khớp với ToSourceDto.</summary>
    public static IQueryable<BloodBag> ApplySourceFilter(this IQueryable<BloodBag> query, string? sourceType)
    {
        switch (sourceType?.Trim().ToUpperInvariant())
        {
            case BagSourceTypes.External:
                return query.Where(b => b.ExternalUnitName != null);
            case BagSourceTypes.Campaign:
                return query.Where(b => b.ExternalUnitName == null &&
                    b.Session != null && b.Session.Appointment != null &&
                    !b.Session.Appointment.IsWalkIn &&
                    b.Session.Appointment.CampaignSlot != null);
            case BagSourceTypes.WalkIn:
                return query.Where(b => b.ExternalUnitName == null &&
                    b.Session != null && b.Session.Appointment != null &&
                    (b.Session.Appointment.IsWalkIn || b.Session.Appointment.CampaignSlot == null));
            default:
                return query;
        }
    }

    public static IQueryable<BloodBag> ApplyQueueSearch(this IQueryable<BloodBag> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        var kw = $"%{search.Trim()}%";
        return query.Where(b =>
            EF.Functions.ILike(b.Barcode, kw) ||
            (b.ExternalUnitName != null && EF.Functions.ILike(b.ExternalUnitName, kw)) ||
            (b.ExternalCode != null && EF.Functions.ILike(b.ExternalCode, kw)) ||
            (b.Session != null && b.Session.Appointment != null && b.Session.Appointment.Donor != null &&
                (EF.Functions.ILike(b.Session.Appointment.Donor.FullName, kw) ||
                 EF.Functions.ILike(b.Session.Appointment.Donor.CitizenId, kw))));
    }

    /// <summary>Cần IncludeSource() trước đó.</summary>
    public static BagSourceDto ToSourceDto(this BloodBag b)
    {
        var session = b.Session;
        var appt = session?.Appointment;
        var donor = appt?.Donor;
        var campaign = appt?.CampaignSlot?.Campaign;

        var dto = new BagSourceDto();

        if (!string.IsNullOrEmpty(b.ExternalUnitName))
        {
            dto.SourceType = BagSourceTypes.External;
            dto.SourceName = b.ExternalUnitName;
            dto.ExternalUnitName = b.ExternalUnitName;
            dto.ExternalCode = b.ExternalCode;
        }
        else if (appt == null)
        {
            dto.SourceType = BagSourceTypes.Unknown;
            dto.SourceName = "Không xác định nguồn";
        }
        else if (!appt.IsWalkIn && campaign != null)
        {
            dto.SourceType = BagSourceTypes.Campaign;
            dto.SourceName = campaign.CampaignName;
            dto.CampaignId = campaign.CampaignId;
            dto.CampaignName = campaign.CampaignName;
            dto.CampaignLocation = campaign.LocationName;
        }
        else
        {
            dto.SourceType = BagSourceTypes.WalkIn;
            dto.SourceName = "Hiến tại viện";
        }

        if (donor != null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var age = today.Year - donor.DateOfBirth.Year;
            if (donor.DateOfBirth > today.AddYears(-age)) age--;

            dto.Donor = new BagDonorDto
            {
                DonorId = donor.DonorId,
                FullName = donor.FullName,
                CitizenId = donor.CitizenId,
                Gender = donor.Gender,
                DateOfBirth = donor.DateOfBirth,
                Age = age,
                PhoneNumber = donor.PhoneNumber,
                DeclaredBloodGroup = donor.BloodType.HasValue && donor.RhFactor.HasValue
                    ? BloodBagMappings.FormatBloodGroup(donor.BloodType.Value, donor.RhFactor.Value)
                    : null,
                TotalDonations = donor.TotalDonations,
                LastDonationDate = donor.LastDonationDate
            };
        }

        if (session != null)
        {
            dto.Collection = new BagCollectionDto
            {
                SessionId = session.SessionId,
                ActualVolumeMl = session.ActualVolumeMl,
                CompletedAt = session.CompletedAt,
                CollectionIncident = session.CollectionIncident
            };
        }

        return dto;
    }

    /// <summary>Cần Include(TestResults).ThenInclude(Technician).</summary>
    public static BagLabSummaryDto? ToLabSummary(this BloodBag b)
    {
        var t = b.TestResults.OrderByDescending(x => x.TestedAt).FirstOrDefault();
        if (t == null) return null;

        return new BagLabSummaryDto
        {
            TestId = t.TestId,
            TestedAt = t.TestedAt,
            TechnicianName = t.Technician?.FullName ?? $"KTV #{t.TechnicianId}",
            HivResult = t.HivResult.ToString(),
            HbvResult = t.HbvResult.ToString(),
            HcvResult = t.HcvResult.ToString(),
            SyphilisResult = t.SyphilisResult.ToString(),
            IrregularAntibody = t.IrregularAntibody.ToString(),
            ConfirmedBloodGroup = BloodBagMappings.FormatBloodGroup(t.ConfirmedBloodType, t.ConfirmedRh),
            OverallConclusion = t.OverallConclusion.ToString()
        };
    }
}