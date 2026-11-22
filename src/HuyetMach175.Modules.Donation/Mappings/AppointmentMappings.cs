using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Donation.DTOs.Appointments;

namespace HuyetMach175.Modules.Donation.Mappings;

public static class AppointmentMappings
{
    public static AppointmentListItemDto ToListItemDto(this DonationAppointment a)
    {
        var slot = a.CampaignSlot;
        var campaign = slot?.Campaign;

        return new AppointmentListItemDto
        {
            AppointmentId = a.AppointmentId,
            DonorId = a.DonorId,
            DonorName = a.Donor?.FullName ?? string.Empty,
            DonorCitizenId = a.Donor?.CitizenId ?? string.Empty,
            DonorPhoneNumber = a.Donor?.PhoneNumber ?? string.Empty,
            DonorDateOfBirth = a.Donor?.DateOfBirth ?? default,
            DonorGender = a.Donor?.Gender ?? string.Empty,
            DonorBloodType = a.Donor?.BloodType,
            DonorRhFactor = a.Donor?.RhFactor,
            CampaignId = slot?.CampaignId,
            CampaignName = campaign?.CampaignName,
            SlotId = a.SlotId,
            AppointmentDate = a.AppointmentDate,
            TimeSlot = slot != null ? $"{slot.StartTime:HH\\:mm}-{slot.EndTime:HH\\:mm}" : string.Empty,
            QrCodeToken = a.QrCodeToken,
            Status = a.Status,
            IsWalkIn = a.IsWalkIn,
            CheckedInAt = a.CheckedInAt,
            CreatedAt = a.CreatedAt,
            HasPreScreeningSurvey = a.PreScreeningSurvey != null,
            SurveyRiskScore = a.PreScreeningSurvey?.RiskScore,
            SurveyHasRisk = a.PreScreeningSurvey?.HasRisk
        };
    }

    public static AppointmentDetailDto ToDetailDto(this DonationAppointment appointment)
    {
        var slot = appointment.CampaignSlot;
        var campaign = slot?.Campaign;

        return new AppointmentDetailDto
        {
            AppointmentId = appointment.AppointmentId,
            DonorId = appointment.DonorId,
            DonorName = appointment.Donor != null ? appointment.Donor.FullName : string.Empty,
            DonorCitizenId = appointment.Donor != null ? appointment.Donor.CitizenId : string.Empty,
            DonorPhoneNumber = appointment.Donor != null ? appointment.Donor.PhoneNumber : string.Empty,
            DonorEmail = appointment.Donor?.Email,
            DonorDateOfBirth = appointment.Donor != null ? appointment.Donor.DateOfBirth : default,
            DonorGender = appointment.Donor != null ? appointment.Donor.Gender : string.Empty,
            DonorBloodType = appointment.Donor?.BloodType,
            DonorRhFactor = appointment.Donor?.RhFactor,
            TotalDonations = appointment.Donor?.TotalDonations ?? 0,
            LastDonationDate = appointment.Donor?.LastDonationDate,

            CampaignId = slot?.CampaignId,
            CampaignName = campaign?.CampaignName,
            CampaignLocation = campaign?.LocationName,
            SlotId = appointment.SlotId,

            AppointmentDate = appointment.AppointmentDate,
            TimeSlot = slot != null ? $"{slot.StartTime:HH\\:mm}-{slot.EndTime:HH\\:mm}" : string.Empty,
            QrCodeToken = appointment.QrCodeToken,
            Status = appointment.Status,
            IsWalkIn = appointment.IsWalkIn,
            CheckedInAt = appointment.CheckedInAt,
            CreatedAt = appointment.CreatedAt,

            HasPreScreeningSurvey = appointment.PreScreeningSurvey != null,
            SurveyId = appointment.PreScreeningSurvey?.SurveyId,
            SurveyRiskScore = appointment.PreScreeningSurvey?.RiskScore,
            SurveyHasRisk = appointment.PreScreeningSurvey?.HasRisk,
            SurveyAnswers = appointment.PreScreeningSurvey?.SurveyAnswers,
            SurveySubmittedAt = appointment.PreScreeningSurvey?.SurveySubmittedAt
        };
    }
}
