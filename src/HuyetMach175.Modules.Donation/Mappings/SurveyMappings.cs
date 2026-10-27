using System.Text.Json;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Donation.DTOs.Surveys;

namespace HuyetMach175.Modules.Donation.Mappings;

public static class SurveyMappings
{
    public static SurveyQueueItemDto ToQueueItemDto(this DonationAppointment a)
    {
        var slot = a.CampaignSlot;
        var campaign = slot?.Campaign;

        return new SurveyQueueItemDto
        {
            AppointmentId = a.AppointmentId,
            AppointmentDate = a.AppointmentDate,
            SlotId = a.SlotId,
            TimeSlot = slot != null ? $"{slot.StartTime:HH\\:mm}-{slot.EndTime:HH\\:mm}" : string.Empty,
            QrCodeToken = a.QrCodeToken,
            IsWalkIn = a.IsWalkIn,
            CheckedInAt = a.CheckedInAt,

            CampaignId = slot?.CampaignId,
            CampaignName = campaign?.CampaignName,

            DonorId = a.DonorId,
            DonorCode = $"NH{a.DonorId:D5}",
            DonorName = a.Donor != null ? a.Donor.FullName : string.Empty,
            DonorCitizenId = a.Donor != null ? a.Donor.CitizenId : string.Empty,
            DonorPhoneNumber = a.Donor != null ? a.Donor.PhoneNumber : string.Empty,
            DonorDateOfBirth = a.Donor != null ? a.Donor.DateOfBirth : default,
            DonorGender = a.Donor != null ? a.Donor.Gender : string.Empty,
            DonorBloodType = a.Donor != null ? a.Donor.BloodType : null,
            DonorRhFactor = a.Donor != null ? a.Donor.RhFactor : null,
            TotalDonations = a.Donor != null ? a.Donor.TotalDonations : 0,
            LastDonationDate = a.Donor != null ? a.Donor.LastDonationDate : null,

            SurveyId = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.SurveyId : null,
            HasSubmittedSurvey = a.PreScreeningSurvey != null && a.PreScreeningSurvey.SurveySubmittedAt != null,
            RiskScore = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.RiskScore : 0,
            HasRisk = a.PreScreeningSurvey != null && a.PreScreeningSurvey.HasRisk,
            SurveySubmittedAt = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.SurveySubmittedAt : null,
            ScreeningStatus = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.ScreeningStatus : ScreeningStatus.PENDING,
            TargetVolumeMl = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.TargetVolumeMl : null,
            ScreenedAt = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.ScreenedAt : null
        };
    }

    public static SurveyDetailDto ToSurveyDetailDto(this DonationAppointment appointment)
    {
        var donor = appointment.Donor;
        var slot = appointment.CampaignSlot;
        var campaign = slot?.Campaign;
        var survey = appointment.PreScreeningSurvey;
        var session = appointment.DonationSession;

        var referenceDate = appointment.AppointmentDate != default
            ? appointment.AppointmentDate
            : DateOnly.FromDateTime(DateTime.UtcNow);

        int age = 0;
        if (donor != null && donor.DateOfBirth != default)
        {
            age = referenceDate.Year - donor.DateOfBirth.Year;
            if (donor.DateOfBirth > referenceDate.AddYears(-age))
            {
                age--;
            }
        }

        DateOnly? nextEligibleDate = null;
        int daysRemaining = 0;
        bool isEligible84Days = true;

        if (donor != null && donor.TotalDonations > 0 && donor.LastDonationDate.HasValue)
        {
            nextEligibleDate = donor.LastDonationDate.Value.AddDays(84);
            var diff = nextEligibleDate.Value.DayNumber - referenceDate.DayNumber;
            if (diff > 0)
            {
                daysRemaining = diff;
                isEligible84Days = false;
            }
        }

        JsonElement? parsedAnswers = null;
        bool hasSubmittedSurvey = false;

        if (survey != null && !string.IsNullOrWhiteSpace(survey.SurveyAnswers) && survey.SurveyAnswers != "{}")
        {
            try
            {
                using var doc = JsonDocument.Parse(survey.SurveyAnswers);
                parsedAnswers = doc.RootElement.Clone();
                hasSubmittedSurvey = true;
            }
            catch
            {
                parsedAnswers = null;
            }
        }
        else if (survey?.SurveySubmittedAt != null)
        {
            hasSubmittedSurvey = true;
        }

        return new SurveyDetailDto
        {
            AppointmentId = appointment.AppointmentId,
            AppointmentDate = appointment.AppointmentDate,
            SlotId = appointment.SlotId,
            TimeSlot = slot != null ? $"{slot.StartTime:HH\\:mm}-{slot.EndTime:HH\\:mm}" : string.Empty,
            QrCodeToken = appointment.QrCodeToken,
            AppointmentStatus = appointment.Status,
            IsWalkIn = appointment.IsWalkIn,
            CheckedInAt = appointment.CheckedInAt,
            CampaignId = slot?.CampaignId,
            CampaignName = campaign?.CampaignName,
            CampaignLocation = campaign?.LocationName,

            Donor = new SurveyDonorSummaryDto
            {
                DonorId = appointment.DonorId,
                DonorCode = $"NH{appointment.DonorId:D5}",
                FullName = donor?.FullName ?? string.Empty,
                CitizenId = donor?.CitizenId ?? string.Empty,
                DateOfBirth = donor?.DateOfBirth ?? default,
                Age = age,
                Gender = donor?.Gender ?? string.Empty,
                PhoneNumber = donor?.PhoneNumber ?? string.Empty,
                Email = donor?.Email,
                BloodType = donor?.BloodType,
                RhFactor = donor?.RhFactor,
                TotalDonations = donor?.TotalDonations ?? 0,
                LastDonationDate = donor?.LastDonationDate,
                NextEligibleDate = nextEligibleDate,
                DaysRemaining = daysRemaining,
                IsEligible84Days = isEligible84Days
            },

            SurveyId = survey?.SurveyId,

            HasSubmittedSurvey = hasSubmittedSurvey,
            SurveyAnswers = parsedAnswers,
            RiskScore = survey?.RiskScore ?? 0,
            HasRisk = survey?.HasRisk ?? false,
            SurveySubmittedAt = survey?.SurveySubmittedAt,

            WeightKg = survey?.WeightKg,
            SystolicBloodPressure = survey?.SystolicBloodPressure,
            DiastolicBloodPressure = survey?.DiastolicBloodPressure,
            Pulse = survey?.Pulse,
            HemoglobinLevel = survey?.HemoglobinLevel,
            BodyTemp = survey?.BodyTemp,
            RapidHbsAgNegative = survey?.RapidHbsAgNegative,
            ClinicalNotes = survey?.ClinicalNotes,

            DoctorId = survey?.DoctorId,
            DoctorName = survey?.Doctor?.FullName,
            ScreeningStatus = survey?.ScreeningStatus ?? ScreeningStatus.PENDING,
            DeferralReason = survey?.DeferralReason,
            TargetVolumeMl = survey?.TargetVolumeMl,
            ScreenedAt = survey?.ScreenedAt,

            SessionId = session?.SessionId,
            SessionStatus = session?.Status
        };
    }
}