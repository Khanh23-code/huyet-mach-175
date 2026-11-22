using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Api.Data.Seeders;

public static class DonationSeeder
{
    public static async Task SeedAppointmentsAndSurveysAsync(ApplicationDbContext context)
    {
        if (await context.DonationAppointments.AnyAsync(da => da.QrCodeToken.StartsWith("HM175-SEED-")))
        {
            return;
        }

        var donorIds = await context.Donors
            .Where(d => d.DonorId >= 1 && d.DonorId <= 13)
            .OrderBy(d => d.DonorId)
            .Select(d => d.DonorId)
            .ToListAsync();

        var slotIds = await context.DonationCampaignSlots
            .Where(s => s.SlotId >= 3 && s.SlotId <= 8)
            .OrderBy(s => s.SlotId)
            .Select(s => s.SlotId)
            .ToListAsync();

        if (donorIds.Count == 0 || slotIds.Count == 0)
        {
            return;
        }

        var doctorId = await context.Users
            .Where(u => u.Username == "doctor")
            .Select(u => (int?)u.UserId)
            .FirstOrDefaultAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var nowUtc = DateTime.UtcNow;

        const string safeSurveyJson = @"{""q1_previous_donation"":true,""q2_chronic_diseases"":false,""q3_last_12_months"":false,""q4_last_6_months"":false,""q5_last_14_days"":false,""q6_last_7_days"":false,""q7_infectious_risk"":false,""q8_unexplained_symptoms"":false,""q9_female_specific"":false}";
        const string riskSurveyJson1 = @"{""q1_previous_donation"":true,""q2_chronic_diseases"":false,""q3_last_12_months"":false,""q4_last_6_months"":false,""q5_last_14_days"":true,""q6_last_7_days"":false,""q7_infectious_risk"":false,""q8_unexplained_symptoms"":false,""q9_female_specific"":false}";
        const string riskSurveyJson2 = @"{""q1_previous_donation"":false,""q2_chronic_diseases"":false,""q3_last_12_months"":false,""q4_last_6_months"":true,""q5_last_14_days"":false,""q6_last_7_days"":true,""q7_infectious_risk"":false,""q8_unexplained_symptoms"":false,""q9_female_specific"":false}";

        for (int i = 0; i < donorIds.Count; i++)
        {
            var donorId = donorIds[i];
            var slotId = slotIds[i % slotIds.Count];
            var index1Based = i + 1;

            var status = AppointmentStatus.CHECKED_IN;
            var isWalkIn = false;
            DateTime? checkedInAt = nowUtc.AddMinutes(-90 + i * 5);

            if (index1Based == 1)
            {
                status = AppointmentStatus.BOOKED;
                checkedInAt = null;
            }
            else if (index1Based == 2)
            {
                status = AppointmentStatus.CANCELLED;
                checkedInAt = null;
            }
            else if (index1Based == 3)
            {
                status = AppointmentStatus.NO_SHOW;
                checkedInAt = null;
            }
            else if (index1Based == 7)
            {
                isWalkIn = true;
            }

            var appointment = new DonationAppointment
            {
                DonorId = donorId,
                SlotId = slotId,
                AppointmentDate = today,
                Status = status,
                IsWalkIn = isWalkIn,
                QrCodeToken = $"HM175-SEED-{donorId:D4}-{slotId:D2}",
                CheckedInAt = checkedInAt,
                CreatedAt = nowUtc.AddDays(-1)
            };

            context.DonationAppointments.Add(appointment);
            await context.SaveChangesAsync();

            if (status != AppointmentStatus.CHECKED_IN)
            {
                continue;
            }

            if (index1Based == 4)
            {
                context.PreScreeningSurveys.Add(new PreScreeningSurvey
                {
                    AppointmentId = appointment.AppointmentId,
                    SurveyAnswers = safeSurveyJson,
                    RiskScore = 0,
                    HasRisk = false,
                    SurveySubmittedAt = nowUtc.AddHours(-2),
                    ScreeningStatus = ScreeningStatus.PENDING
                });
            }
            else if (index1Based == 5)
            {
                context.PreScreeningSurveys.Add(new PreScreeningSurvey
                {
                    AppointmentId = appointment.AppointmentId,
                    SurveyAnswers = riskSurveyJson1,
                    RiskScore = 1,
                    HasRisk = true,
                    SurveySubmittedAt = nowUtc.AddHours(-2),
                    ScreeningStatus = ScreeningStatus.PENDING
                });
            }
            else if (index1Based == 6)
            {
                context.PreScreeningSurveys.Add(new PreScreeningSurvey
                {
                    AppointmentId = appointment.AppointmentId,
                    SurveyAnswers = riskSurveyJson2,
                    RiskScore = 2,
                    HasRisk = true,
                    SurveySubmittedAt = nowUtc.AddHours(-2),
                    ScreeningStatus = ScreeningStatus.PENDING
                });
            }
            else if (index1Based == 9)
            {
                context.PreScreeningSurveys.Add(new PreScreeningSurvey
                {
                    AppointmentId = appointment.AppointmentId,
                    SurveyAnswers = safeSurveyJson,
                    RiskScore = 0,
                    HasRisk = false,
                    SurveySubmittedAt = nowUtc.AddHours(-2),
                    DoctorId = doctorId,
                    WeightKg = 68.5m,
                    SystolicBloodPressure = 120,
                    DiastolicBloodPressure = 78,
                    Pulse = 74,
                    HemoglobinLevel = 142.0m,
                    BodyTemp = 36.6m,
                    RapidHbsAgNegative = true,
                    ClinicalNotes = "Tỉnh táo, huyết động ổn định, đủ điều kiện hiến máu 350ml.",
                    ScreeningStatus = ScreeningStatus.QUALIFIED,
                    TargetVolumeMl = TargetVolume.VOL_350,
                    ScreenedAt = nowUtc.AddMinutes(-20)
                });

                context.DonationSessions.Add(new DonationSession
                {
                    AppointmentId = appointment.AppointmentId,
                    TargetVolumeMl = TargetVolume.VOL_350,
                    Status = SessionStatus.WAITING_COLLECTION,
                    CreatedAt = nowUtc.AddMinutes(-20)
                });
            }
            else if (index1Based == 10)
            {
                context.PreScreeningSurveys.Add(new PreScreeningSurvey
                {
                    AppointmentId = appointment.AppointmentId,
                    SurveyAnswers = riskSurveyJson1,
                    RiskScore = 1,
                    HasRisk = true,
                    SurveySubmittedAt = nowUtc.AddHours(-2),
                    DoctorId = doctorId,
                    WeightKg = 58.0m,
                    SystolicBloodPressure = 152,
                    DiastolicBloodPressure = 96,
                    Pulse = 92,
                    HemoglobinLevel = 130.0m,
                    BodyTemp = 36.9m,
                    RapidHbsAgNegative = true,
                    ClinicalNotes = "Huyết áp cao tại thời điểm khám, mới khỏi cảm cúm.",
                    ScreeningStatus = ScreeningStatus.DEFERRED,
                    DeferralReason = "Huyết áp tâm thu cao (152/96 mmHg) và mới khỏi cảm cúm trong 14 ngày.",
                    ScreenedAt = nowUtc.AddMinutes(-15)
                });
            }

            await context.SaveChangesAsync();
        }
    }
}