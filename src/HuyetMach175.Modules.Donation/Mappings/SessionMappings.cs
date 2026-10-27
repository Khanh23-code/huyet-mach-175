using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Donation.DTOs.Sessions;

namespace HuyetMach175.Modules.Donation.Mappings;

public static class SessionMappings
{
    public static SessionQueueItemDto ToQueueItemDto(this DonationSession session)
    {
        var appointment = session.Appointment;
        var donor = appointment?.Donor;
        var survey = appointment?.PreScreeningSurvey;
        var doctor = survey?.Doctor;

        return new SessionQueueItemDto
        {
            SessionId = session.SessionId,
            AppointmentId = session.AppointmentId,
            DonorCode = donor != null ? $"NH-{donor.DonorId:D6}" : string.Empty,
            DonorName = donor?.FullName ?? string.Empty,
            TargetVolumeMl = session.TargetVolumeMl.ToString(),
            Status = session.Status.ToString(),
            ScreenedAt = survey?.ScreenedAt,
            DoctorName = doctor?.FullName
        };
    }

    public static SessionDetailDto ToDetailDto(this DonationSession session)
    {
        var appointment = session.Appointment;
        var donor = appointment?.Donor;
        var survey = appointment?.PreScreeningSurvey;
        var doctor = survey?.Doctor;
        var phlebotomist = session.Phlebotomist;
        var bloodBag = session.BloodBags.FirstOrDefault();

        string? bloodPressure = null;
        if (survey?.SystolicBloodPressure != null && survey?.DiastolicBloodPressure != null)
        {
            bloodPressure = $"{survey.SystolicBloodPressure}/{survey.DiastolicBloodPressure}";
        }

        string? rhFactorStr = donor?.RhFactor switch
        {
            RhFactor.POSITIVE => "Rh+",
            RhFactor.NEGATIVE => "Rh-",
            _ => null
        };

        return new SessionDetailDto
        {
            SessionId = session.SessionId,
            AppointmentId = session.AppointmentId,
            Status = session.Status.ToString(),
            TargetVolumeMl = session.TargetVolumeMl.ToString(),
            ActualVolumeMl = session.ActualVolumeMl,
            CollectionIncident = session.CollectionIncident,
            PhlebotomistId = session.PhlebotomistId,
            PhlebotomistName = phlebotomist?.FullName,
            CompletedAt = session.CompletedAt,
            BloodBagBarcode = bloodBag?.Barcode,

            Donor = new SessionDonorDto
            {
                DonorId = donor?.DonorId ?? 0,
                DonorCode = donor != null ? $"NH-{donor.DonorId:D6}" : string.Empty,
                FullName = donor?.FullName ?? string.Empty,
                CitizenId = donor?.CitizenId ?? string.Empty,
                Gender = donor?.Gender ?? string.Empty,
                BloodType = donor?.BloodType?.ToString(),
                RhFactor = rhFactorStr
            },

            ScreeningSummary = new SessionScreeningSummaryDto
            {
                WeightKg = survey?.WeightKg,
                BloodPressure = bloodPressure,
                Pulse = survey?.Pulse,
                HemoglobinLevel = survey?.HemoglobinLevel,
                ClinicalNotes = survey?.ClinicalNotes,
                DoctorName = doctor?.FullName,
                ScreenedAt = survey?.ScreenedAt
            }
        };
    }
}
