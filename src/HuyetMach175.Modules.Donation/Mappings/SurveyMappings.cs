using System.Text.Json;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Donation.DTOs.Surveys;

namespace HuyetMach175.Modules.Donation.Mappings;

public static class SurveyMappings
{
    public static SurveyQueueItemDto ToQueueItemDto(this DonationAppointment a)
    {
        return new SurveyQueueItemDto
        {
            AppointmentId = a.AppointmentId,
            DonorId = a.DonorId,
            DonorCode = $"NH-{a.DonorId:D6}",
            DonorName = a.Donor?.FullName ?? string.Empty,
            CheckedInAt = a.CheckedInAt,
            IsWalkIn = a.IsWalkIn,
            HasSubmittedSurvey = a.PreScreeningSurvey?.SurveySubmittedAt != null,
            HasRisk = a.PreScreeningSurvey?.HasRisk ?? false,
            ScreeningStatus = a.PreScreeningSurvey != null
                ? a.PreScreeningSurvey.ScreeningStatus.ToString()
                : nameof(ScreeningStatus.PENDING)
        };
    }

    public static SurveyDetailDto ToSurveyDetailDto(this DonationAppointment appointment)
    {
        var donor = appointment.Donor;
        var slot = appointment.CampaignSlot;
        var campaign = slot?.Campaign;
        var survey = appointment.PreScreeningSurvey;

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

        bool isEligible84Days = true;
        if (donor != null && donor.TotalDonations > 0 && donor.LastDonationDate.HasValue)
        {
            var nextEligibleDate = donor.LastDonationDate.Value.AddDays(84);
            if (nextEligibleDate > referenceDate)
            {
                isEligible84Days = false;
            }
        }

        string? bloodTypeStr = donor?.BloodType?.ToString();
        string? rhFactorStr = donor?.RhFactor switch
        {
            RhFactor.POSITIVE => "Rh+",
            RhFactor.NEGATIVE => "Rh-",
            _ => null
        };

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
            TimeSlot = slot != null ? $"{slot.StartTime:HH\\:mm} - {slot.EndTime:HH\\:mm}" : string.Empty,
            IsWalkIn = appointment.IsWalkIn,
            CheckedInAt = appointment.CheckedInAt,
            CampaignName = campaign?.CampaignName,

            Donor = new SurveyDonorSummaryDto
            {
                DonorId = appointment.DonorId,
                DonorCode = $"NH-{appointment.DonorId:D6}",
                FullName = donor?.FullName ?? string.Empty,
                CitizenId = donor?.CitizenId ?? string.Empty,
                DateOfBirth = donor?.DateOfBirth ?? default,
                Age = age,
                Gender = donor?.Gender ?? string.Empty,
                PhoneNumber = donor?.PhoneNumber ?? string.Empty,
                BloodType = bloodTypeStr,
                RhFactor = rhFactorStr,
                TotalDonations = donor?.TotalDonations ?? 0,
                LastDonationDate = donor?.LastDonationDate,
                IsEligible84Days = isEligible84Days
            },

            Survey = new SurveyOnlineDetailDto
            {
                SurveyId = survey?.SurveyId,
                HasSubmittedSurvey = hasSubmittedSurvey,
                SurveySubmittedAt = survey?.SurveySubmittedAt,
                RiskScore = survey?.RiskScore ?? 0,
                HasRisk = survey?.HasRisk ?? false,
                SurveyAnswers = parsedAnswers
            },

            Clinical = new SurveyClinicalDetailDto
            {
                WeightKg = survey?.WeightKg,
                SystolicBloodPressure = survey?.SystolicBloodPressure,
                DiastolicBloodPressure = survey?.DiastolicBloodPressure,
                Pulse = survey?.Pulse,
                HemoglobinLevel = survey?.HemoglobinLevel,
                BodyTemp = survey?.BodyTemp,
                RapidHbsAgNegative = survey?.RapidHbsAgNegative,
                ClinicalNotes = survey?.ClinicalNotes,
                ScreeningStatus = survey != null ? survey.ScreeningStatus.ToString() : nameof(ScreeningStatus.PENDING),
                DeferralReason = survey?.DeferralReason,
                TargetVolumeMl = survey?.TargetVolumeMl?.ToString(),
                DoctorId = survey?.DoctorId,
                DoctorName = survey?.Doctor?.FullName,
                ScreenedAt = survey?.ScreenedAt
            }
        };
    }
}