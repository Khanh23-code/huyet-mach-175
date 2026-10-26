using System.Linq.Expressions;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Donation.DTOs.Appointments;

namespace HuyetMach175.Modules.Donation.Mappings;

public static class AppointmentMappings
{
    public static readonly Expression<Func<DonationAppointment, AppointmentListItemDto>> ProjectToListItem = a => new AppointmentListItemDto
    {
        AppointmentId = a.AppointmentId,
        DonorId = a.DonorId,
        DonorName = a.Donor != null ? a.Donor.FullName : string.Empty,
        DonorCitizenId = a.Donor != null ? a.Donor.CitizenId : string.Empty,
        DonorPhoneNumber = a.Donor != null ? a.Donor.PhoneNumber : string.Empty,
        DonorDateOfBirth = a.Donor != null ? a.Donor.DateOfBirth : default,
        DonorGender = a.Donor != null ? a.Donor.Gender : string.Empty,
        DonorBloodType = a.Donor != null ? a.Donor.BloodType : null,
        DonorRhFactor = a.Donor != null ? a.Donor.RhFactor : null,
        CampaignId = a.CampaignId,
        CampaignName = a.Campaign != null ? a.Campaign.CampaignName : null,
        AppointmentDate = a.AppointmentDate,
        TimeSlot = a.TimeSlot,
        QrCodeToken = a.QrCodeToken,
        Status = a.Status,
        IsWalkIn = a.IsWalkIn,
        CheckedInAt = a.CheckedInAt,
        CreatedAt = a.CreatedAt,
        HasPreScreeningSurvey = a.PreScreeningSurvey != null,
        SurveyRiskScore = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.RiskScore : null,
        SurveyHasRisk = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.HasRisk : null
    };

    public static readonly Expression<Func<DonationAppointment, AppointmentDetailDto>> ProjectToDetail = a => new AppointmentDetailDto
    {
        AppointmentId = a.AppointmentId,
        DonorId = a.DonorId,
        DonorName = a.Donor != null ? a.Donor.FullName : string.Empty,
        DonorCitizenId = a.Donor != null ? a.Donor.CitizenId : string.Empty,
        DonorPhoneNumber = a.Donor != null ? a.Donor.PhoneNumber : string.Empty,
        DonorEmail = a.Donor != null ? a.Donor.Email : null,
        DonorDateOfBirth = a.Donor != null ? a.Donor.DateOfBirth : default,
        DonorGender = a.Donor != null ? a.Donor.Gender : string.Empty,
        DonorBloodType = a.Donor != null ? a.Donor.BloodType : null,
        DonorRhFactor = a.Donor != null ? a.Donor.RhFactor : null,
        TotalDonations = a.Donor != null ? a.Donor.TotalDonations : 0,
        LastDonationDate = a.Donor != null ? a.Donor.LastDonationDate : null,

        CampaignId = a.CampaignId,
        CampaignName = a.Campaign != null ? a.Campaign.CampaignName : null,
        CampaignLocation = a.Campaign != null ? a.Campaign.LocationName : null,

        AppointmentDate = a.AppointmentDate,
        TimeSlot = a.TimeSlot,
        QrCodeToken = a.QrCodeToken,
        Status = a.Status,
        IsWalkIn = a.IsWalkIn,
        CheckedInAt = a.CheckedInAt,
        CreatedAt = a.CreatedAt,

        HasPreScreeningSurvey = a.PreScreeningSurvey != null,
        SurveyId = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.SurveyId : null,
        SurveyRiskScore = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.RiskScore : null,
        SurveyHasRisk = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.HasRisk : null,
        SurveyAnswers = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.SurveyAnswers : null,
        SurveySubmittedAt = a.PreScreeningSurvey != null ? a.PreScreeningSurvey.SubmittedAt : null
    };

    public static AppointmentDetailDto ToDetailDto(this DonationAppointment appointment)
    {
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

            CampaignId = appointment.CampaignId,
            CampaignName = appointment.Campaign?.CampaignName,
            CampaignLocation = appointment.Campaign?.LocationName,

            AppointmentDate = appointment.AppointmentDate,
            TimeSlot = appointment.TimeSlot,
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
            SurveySubmittedAt = appointment.PreScreeningSurvey?.SubmittedAt
        };
    }
}

