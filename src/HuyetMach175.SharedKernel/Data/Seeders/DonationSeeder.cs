using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Api.Data.Seeders;

public static class DonationSeeder
{
    public static async Task SeedDonationDataAsync(ApplicationDbContext context)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nowUtc = DateTime.UtcNow;

        var bloodbankId = await context.Users
            .Where(u => u.Username == "bloodbank")
            .Select(u => u.UserId)
            .FirstOrDefaultAsync();

        var managerId = await context.Users
            .Where(u => u.Username == "manager")
            .Select(u => (Guid?)u.UserId)
            .FirstOrDefaultAsync();

        var doctorId = await context.Users
            .Where(u => u.Username == "doctor")
            .Select(u => (Guid?)u.UserId)
            .FirstOrDefaultAsync();

        var donorStaffId = await context.Users
            .Where(u => u.Username == "donorstaff")
            .Select(u => (Guid?)u.UserId)
            .FirstOrDefaultAsync();

        if (bloodbankId == Guid.Empty)
        {
            return;
        }

        if (!await context.DonationCampaigns.AnyAsync())
        {
            var mainCampaign = new DonationCampaign
            {
                CampaignName = "Hiến Máu Giọt Hồng 175 - Đợt Thu Đông",
                Description = "Đợt hiến máu tập trung phục vụ dự trữ máu cấp cứu và phẫu thuật tại Bệnh viện Quân y 175.",
                LocationName = "Hội trường A - Bệnh viện Quân y 175 (786 Nguyễn Kiệm, Gò Vấp)",
                CampaignType = CampaignType.EVENT,
                StartDate = today.AddDays(-3),
                EndDate = today.AddDays(25),
                TargetDonations = 150,
                MaxCapacity = 225,
                Status = CampaignStatus.IN_PROGRESS,
                CreatedBy = bloodbankId,
                ApprovedBy = managerId,
                ApprovedAt = nowUtc.AddDays(-5),
                CreatedAt = nowUtc.AddDays(-7),
                Slots = new List<DonationCampaignSlot>
                {
                    new() { StartTime = new TimeOnly(7, 30), EndTime = new TimeOnly(9, 30), Capacity = 25 },
                    new() { StartTime = new TimeOnly(9, 30), EndTime = new TimeOnly(11, 30), Capacity = 25 },
                    new() { StartTime = new TimeOnly(13, 30), EndTime = new TimeOnly(16, 30), Capacity = 25 }
                }
            };

            var recurringCampaign = new DonationCampaign
            {
                CampaignName = "Điểm Hiến Máu Cố Định Sảnh B - Viện 175",
                Description = "Điểm tiếp nhận máu thường xuyên các ngày trong tuần tại Trung tâm Truyền máu.",
                LocationName = "Tầng 1 Khu B - Trung tâm Truyền máu Bệnh viện Quân y 175",
                CampaignType = CampaignType.RECURRING,
                OperatingDays = "MON,TUE,WED,THU,FRI,SAT",
                StartDate = today.AddDays(-15),
                EndDate = today.AddDays(60),
                TargetDonations = 300,
                MaxCapacity = 400,
                Status = CampaignStatus.IN_PROGRESS,
                CreatedBy = bloodbankId,
                ApprovedBy = managerId,
                ApprovedAt = nowUtc.AddDays(-16),
                CreatedAt = nowUtc.AddDays(-18),
                Slots = new List<DonationCampaignSlot>
                {
                    new() { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(11, 0), Capacity = 20 },
                    new() { StartTime = new TimeOnly(13, 30), EndTime = new TimeOnly(16, 0), Capacity = 20 }
                }
            };

            var pendingCampaign = new DonationCampaign
            {
                CampaignName = "Ngày Hội Xuân Hồng Quân Dân Y",
                Description = "Đề xuất phối hợp tổ chức ngày hội hiến máu tình nguyện đầu xuân.",
                LocationName = "Nhà Văn hóa Thanh Niên TP.HCM",
                CampaignType = CampaignType.EVENT,
                StartDate = today.AddDays(14),
                EndDate = today.AddDays(16),
                TargetDonations = 200,
                MaxCapacity = 240,
                Status = CampaignStatus.PENDING_APPROVAL,
                CreatedBy = bloodbankId,
                CreatedAt = nowUtc.AddDays(-1),
                Slots = new List<DonationCampaignSlot>
                {
                    new() { StartTime = new TimeOnly(7, 30), EndTime = new TimeOnly(11, 0), Capacity = 40 },
                    new() { StartTime = new TimeOnly(13, 30), EndTime = new TimeOnly(16, 30), Capacity = 40 }
                }
            };

            var approvedCampaign = new DonationCampaign
            {
                CampaignName = "Hiến Máu Tình Nguyện Khối Sinh Viên Gò Vấp",
                Description = "Đợt hiến máu lưu động đã được Ban Giám đốc phê duyệt, chờ đến ngày triển khai.",
                LocationName = "Cơ sở Nguyễn Văn Bảo - Quận Gò Vấp",
                CampaignType = CampaignType.EVENT,
                StartDate = today.AddDays(7),
                EndDate = today.AddDays(9),
                TargetDonations = 120,
                MaxCapacity = 150,
                Status = CampaignStatus.APPROVED,
                CreatedBy = bloodbankId,
                ApprovedBy = managerId,
                ApprovedAt = nowUtc.AddHours(-12),
                CreatedAt = nowUtc.AddDays(-3),
                Slots = new List<DonationCampaignSlot>
                {
                    new() { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(11, 30), Capacity = 30 },
                    new() { StartTime = new TimeOnly(13, 30), EndTime = new TimeOnly(16, 0), Capacity = 20 }
                }
            };

            var draftCampaign = new DonationCampaign
            {
                CampaignName = "Kế Hoạch Hiến Máu Lưu Động Quận Phú Nhuận",
                Description = "Bản nháp kế hoạch phối hợp Hội Chữ thập đỏ địa phương.",
                LocationName = "Trung tâm Văn hóa Quận Phú Nhuận",
                CampaignType = CampaignType.EVENT,
                StartDate = today.AddDays(21),
                EndDate = today.AddDays(22),
                TargetDonations = 100,
                MaxCapacity = 120,
                Status = CampaignStatus.DRAFT,
                CreatedBy = bloodbankId,
                CreatedAt = nowUtc.AddHours(-6),
                Slots = new List<DonationCampaignSlot>
                {
                    new() { StartTime = new TimeOnly(7, 30), EndTime = new TimeOnly(11, 0), Capacity = 30 }
                }
            };

            var rejectedCampaign = new DonationCampaign
            {
                CampaignName = "Đợt Hiến Máu Đột Xuất Khu Công Nghiệp Tân Bình",
                Description = "Đề xuất tổ chức điểm lấy máu lưu động cuối tuần.",
                LocationName = "Nhà sinh hoạt công nhân KCN Tân Bình",
                CampaignType = CampaignType.EVENT,
                StartDate = today.AddDays(5),
                EndDate = today.AddDays(5),
                TargetDonations = 80,
                MaxCapacity = 100,
                Status = CampaignStatus.REJECTED,
                CreatedBy = bloodbankId,
                ApprovedBy = managerId,
                ApprovedAt = nowUtc.AddDays(-2),
                RejectionReason = "Trùng lịch nhân sự với đợt hiến máu tập trung tại Viện, đề nghị dời sang tuần kế tiếp.",
                CreatedAt = nowUtc.AddDays(-4)
            };

            var cancelledCampaign = new DonationCampaign
            {
                CampaignName = "Ngày Chủ Nhật Đỏ Công Viên Gia Định",
                Description = "Điểm hiến máu lưu động ngoài trời.",
                LocationName = "Khu A Công viên Gia Định, Phường 3, Gò Vấp",
                CampaignType = CampaignType.EVENT,
                StartDate = today.AddDays(-2),
                EndDate = today.AddDays(-1),
                TargetDonations = 100,
                MaxCapacity = 120,
                Status = CampaignStatus.CANCELLED,
                CreatedBy = bloodbankId,
                ApprovedBy = managerId,
                ApprovedAt = nowUtc.AddDays(-6),
                CancelledBy = managerId,
                CancelledAt = nowUtc.AddDays(-2),
                CancellationReason = "Thời tiết mưa lớn kéo dài không đảm bảo điều kiện an toàn thu gom máu ngoài trời.",
                CreatedAt = nowUtc.AddDays(-8)
            };

            context.DonationCampaigns.AddRange(
                mainCampaign,
                recurringCampaign,
                pendingCampaign,
                approvedCampaign,
                draftCampaign,
                rejectedCampaign,
                cancelledCampaign);

            await context.SaveChangesAsync();
        }

        if (!await context.Donors.AnyAsync())
        {
            var donors = new List<Donor>
            {
                new() { CitizenId = "079095001001", FullName = "Nguyễn Văn An", DateOfBirth = new DateOnly(1995, 4, 12), Gender = "Nam", PhoneNumber = "0901234001", Email = "nguyenvanan@example.com", BloodType = BloodType.O, RhFactor = RhFactor.POSITIVE, TotalDonations = 3, LastDonationDate = today.AddDays(-120) },
                new() { CitizenId = "079098001002", FullName = "Trần Thị Bích", DateOfBirth = new DateOnly(1998, 8, 25), Gender = "Nữ", PhoneNumber = "0901234002", Email = "tranthibich@example.com", BloodType = BloodType.A, RhFactor = RhFactor.POSITIVE, TotalDonations = 2, LastDonationDate = today.AddDays(-150) },
                new() { CitizenId = "079092001003", FullName = "Lê Hoàng Long", DateOfBirth = new DateOnly(1992, 11, 3), Gender = "Nam", PhoneNumber = "0901234003", Email = "lehoanglong@example.com", BloodType = BloodType.B, RhFactor = RhFactor.POSITIVE, TotalDonations = 4, LastDonationDate = today.AddDays(-100) },
                new() { CitizenId = "079100001004", FullName = "Phạm Quỳnh Như", DateOfBirth = new DateOnly(2000, 2, 19), Gender = "Nữ", PhoneNumber = "0901234004", Email = "phamquynhnhu@example.com", BloodType = BloodType.AB, RhFactor = RhFactor.NEGATIVE, TotalDonations = 1, LastDonationDate = today.AddDays(-180) },
                new() { CitizenId = "079094001005", FullName = "Võ Minh Trí", DateOfBirth = new DateOnly(1994, 7, 30), Gender = "Nam", PhoneNumber = "0901234005", Email = "vominhtri@example.com", BloodType = BloodType.O, RhFactor = RhFactor.NEGATIVE, TotalDonations = 5, LastDonationDate = today.AddDays(-95) },
                new() { CitizenId = "079099001006", FullName = "Đặng Thu Hà", DateOfBirth = new DateOnly(1999, 5, 14), Gender = "Nữ", PhoneNumber = "0901234006", Email = "dangthuha@example.com", BloodType = BloodType.A, RhFactor = RhFactor.NEGATIVE, TotalDonations = 1, LastDonationDate = today.AddDays(-200) },
                new() { CitizenId = "079096001007", FullName = "Hoàng Gia Bảo", DateOfBirth = new DateOnly(1996, 9, 8), Gender = "Nam", PhoneNumber = "0901234007", Email = "hoanggiabao@example.com", BloodType = BloodType.B, RhFactor = RhFactor.NEGATIVE, TotalDonations = 2, LastDonationDate = today.AddDays(-140) },
                new() { CitizenId = "079101001008", FullName = "Bùi Thị Mai", DateOfBirth = new DateOnly(2001, 12, 22), Gender = "Nữ", PhoneNumber = "0901234008", Email = "buithimai@example.com", BloodType = BloodType.AB, RhFactor = RhFactor.POSITIVE, TotalDonations = 1, LastDonationDate = today.AddDays(-160) },
                new() { CitizenId = "079102001009", FullName = "Đỗ Văn Hùng", DateOfBirth = new DateOnly(2002, 3, 17), Gender = "Nam", PhoneNumber = "0901234009", Email = "dovanhung@example.com", BloodType = null, RhFactor = null, TotalDonations = 0, LastDonationDate = null },
                new() { CitizenId = "079103001010", FullName = "Lý Kim Ngân", DateOfBirth = new DateOnly(2003, 6, 9), Gender = "Nữ", PhoneNumber = "0901234010", Email = "lykimngan@example.com", BloodType = null, RhFactor = null, TotalDonations = 0, LastDonationDate = null }
            };

            context.Donors.AddRange(donors);
            await context.SaveChangesAsync();
        }

        if (await context.DonationAppointments.AnyAsync(da => da.QrCodeToken.StartsWith("HM175-SEED-")))
        {
            return;
        }

        var donorMap = await context.Donors.ToDictionaryAsync(d => d.CitizenId);
        var mainSlots = await context.DonationCampaignSlots
            .Include(s => s.Campaign)
            .Where(s => s.Campaign != null && s.Campaign.CampaignName == "Hiến Máu Giọt Hồng 175 - Đợt Thu Đông")
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        var secondarySlot = await context.DonationCampaignSlots
            .Include(s => s.Campaign)
            .Where(s => s.Campaign != null && s.Campaign.CampaignName == "Điểm Hiến Máu Cố Định Sảnh B - Viện 175")
            .OrderBy(s => s.StartTime)
            .FirstOrDefaultAsync();

        if (mainSlots.Count == 0 || donorMap.Count < 10)
        {
            return;
        }

        const string safeSurveyJson = @"{""q1_previous_donation"":true,""q2_chronic_diseases"":false,""q3_last_12_months"":false,""q4_last_6_months"":false,""q5_last_14_days"":false,""q6_last_7_days"":false,""q7_infectious_risk"":false,""q8_unexplained_symptoms"":false,""q9_female_specific"":false}";
        const string riskSurveyJson = @"{""q1_previous_donation"":true,""q2_chronic_diseases"":false,""q3_last_12_months"":false,""q4_last_6_months"":false,""q5_last_14_days"":true,""q6_last_7_days"":false,""q7_infectious_risk"":false,""q8_unexplained_symptoms"":false,""q9_female_specific"":false}";

        // 1. BOOKED tại đợt chính (sẵn sàng test quét QR Check-in)
        context.DonationAppointments.Add(new DonationAppointment
        {
            DonorId = donorMap["079095001001"].DonorId,
            SlotId = mainSlots[0].SlotId,
            AppointmentDate = today,
            QrCodeToken = "HM175-SEED-APT-001",
            Status = AppointmentStatus.BOOKED,
            IsWalkIn = false,
            CreatedAt = nowUtc.AddHours(-18)
        });

        // 2. BOOKED tại đợt phụ Sảnh B (test cảnh báo quét nhầm đợt hiến máu)
        if (secondarySlot != null)
        {
            context.DonationAppointments.Add(new DonationAppointment
            {
                DonorId = donorMap["079098001002"].DonorId,
                SlotId = secondarySlot.SlotId,
                AppointmentDate = today,
                QrCodeToken = "HM175-SEED-APT-002",
                Status = AppointmentStatus.BOOKED,
                IsWalkIn = false,
                CreatedAt = nowUtc.AddHours(-16)
            });
        }

        // 3. CANCELLED
        context.DonationAppointments.Add(new DonationAppointment
        {
            DonorId = donorMap["079092001003"].DonorId,
            SlotId = mainSlots[0].SlotId,
            AppointmentDate = today,
            QrCodeToken = "HM175-SEED-APT-003",
            Status = AppointmentStatus.CANCELLED,
            IsWalkIn = false,
            CreatedAt = nowUtc.AddDays(-1)
        });

        // 4. NO_SHOW
        context.DonationAppointments.Add(new DonationAppointment
        {
            DonorId = donorMap["079100001004"].DonorId,
            SlotId = mainSlots[0].SlotId,
            AppointmentDate = today,
            QrCodeToken = "HM175-SEED-APT-004",
            Status = AppointmentStatus.NO_SHOW,
            IsWalkIn = false,
            CreatedAt = nowUtc.AddDays(-1)
        });

        // 5. CHECKED_IN chưa nộp bảng hỏi (test điền khảo sát)
        context.DonationAppointments.Add(new DonationAppointment
        {
            DonorId = donorMap["079094001005"].DonorId,
            SlotId = mainSlots[1 % mainSlots.Count].SlotId,
            AppointmentDate = today,
            QrCodeToken = "HM175-SEED-APT-005",
            Status = AppointmentStatus.CHECKED_IN,
            IsWalkIn = false,
            CheckedInAt = nowUtc.AddMinutes(-50),
            CreatedAt = nowUtc.AddDays(-1)
        });

        // 6. CHECKED_IN + Khảo sát an toàn chờ khám lâm sàng (PENDING, HasRisk = false)
        var aptPendingSafe = new DonationAppointment
        {
            DonorId = donorMap["079099001006"].DonorId,
            SlotId = mainSlots[1 % mainSlots.Count].SlotId,
            AppointmentDate = today,
            QrCodeToken = "HM175-SEED-APT-006",
            Status = AppointmentStatus.CHECKED_IN,
            IsWalkIn = false,
            CheckedInAt = nowUtc.AddMinutes(-45),
            CreatedAt = nowUtc.AddDays(-1)
        };
        context.DonationAppointments.Add(aptPendingSafe);
        context.PreScreeningSurveys.Add(new PreScreeningSurvey
        {
            AppointmentId = aptPendingSafe.AppointmentId,
            SurveyAnswers = safeSurveyJson,
            RiskScore = 0,
            HasRisk = false,
            SurveySubmittedAt = nowUtc.AddMinutes(-40),
            ScreeningStatus = ScreeningStatus.PENDING
        });

        // 7. CHECKED_IN + Khảo sát có yếu tố nguy cơ chờ bác sĩ thẩm định (PENDING, HasRisk = true)
        var aptPendingRisk = new DonationAppointment
        {
            DonorId = donorMap["079096001007"].DonorId,
            SlotId = mainSlots[1 % mainSlots.Count].SlotId,
            AppointmentDate = today,
            QrCodeToken = "HM175-SEED-APT-007",
            Status = AppointmentStatus.CHECKED_IN,
            IsWalkIn = false,
            CheckedInAt = nowUtc.AddMinutes(-40),
            CreatedAt = nowUtc.AddDays(-1)
        };
        context.DonationAppointments.Add(aptPendingRisk);
        context.PreScreeningSurveys.Add(new PreScreeningSurvey
        {
            AppointmentId = aptPendingRisk.AppointmentId,
            SurveyAnswers = riskSurveyJson,
            RiskScore = 1,
            HasRisk = true,
            SurveySubmittedAt = nowUtc.AddMinutes(-35),
            ScreeningStatus = ScreeningStatus.PENDING
        });

        // 8. CHECKED_IN + Tạm hoãn sau khám sàng lọc (DEFERRED)
        var aptDeferred = new DonationAppointment
        {
            DonorId = donorMap["079101001008"].DonorId,
            SlotId = mainSlots[1 % mainSlots.Count].SlotId,
            AppointmentDate = today,
            QrCodeToken = "HM175-SEED-APT-008",
            Status = AppointmentStatus.CHECKED_IN,
            IsWalkIn = false,
            CheckedInAt = nowUtc.AddMinutes(-65),
            CreatedAt = nowUtc.AddDays(-1)
        };
        context.DonationAppointments.Add(aptDeferred);
        context.PreScreeningSurveys.Add(new PreScreeningSurvey
        {
            AppointmentId = aptDeferred.AppointmentId,
            SurveyAnswers = riskSurveyJson,
            RiskScore = 1,
            HasRisk = true,
            SurveySubmittedAt = nowUtc.AddMinutes(-60),
            DoctorId = doctorId,
            WeightKg = 56.0m,
            SystolicBloodPressure = 152,
            DiastolicBloodPressure = 96,
            Pulse = 92,
            HemoglobinLevel = 128.0m,
            BodyTemp = 36.9m,
            RapidHbsAgNegative = true,
            ClinicalNotes = "Huyết áp tăng cao tại bàn khám, mới dùng thuốc cảm cúm.",
            ScreeningStatus = ScreeningStatus.DEFERRED,
            DeferralReason = "Huyết áp tâm thu cao (152/96 mmHg) và mới điều trị cảm cúm trong 14 ngày.",
            ScreenedAt = nowUtc.AddMinutes(-50)
        });

        // 9. CHECKED_IN + Đạt sàng lọc + Đang chờ lấy máu (WAITING_COLLECTION)
        var aptWaitingCollection = new DonationAppointment
        {
            DonorId = donorMap["079102001009"].DonorId,
            SlotId = mainSlots[2 % mainSlots.Count].SlotId,
            AppointmentDate = today,
            QrCodeToken = "HM175-SEED-APT-009",
            Status = AppointmentStatus.CHECKED_IN,
            IsWalkIn = false,
            CheckedInAt = nowUtc.AddMinutes(-35),
            CreatedAt = nowUtc.AddDays(-1)
        };
        context.DonationAppointments.Add(aptWaitingCollection);
        context.PreScreeningSurveys.Add(new PreScreeningSurvey
        {
            AppointmentId = aptWaitingCollection.AppointmentId,
            SurveyAnswers = safeSurveyJson,
            RiskScore = 0,
            HasRisk = false,
            SurveySubmittedAt = nowUtc.AddMinutes(-30),
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
            AppointmentId = aptWaitingCollection.AppointmentId,
            TargetVolumeMl = TargetVolume.VOL_350,
            Status = SessionStatus.WAITING_COLLECTION,
            CreatedAt = nowUtc.AddMinutes(-20)
        });

        // 10. Walk-in + Đã hoàn tất thu gom máu (COMPLETED) -> gắn với túi máu NEWLY_COLLECTED
        var aptCompleted = new DonationAppointment
        {
            DonorId = donorMap["079103001010"].DonorId,
            SlotId = mainSlots[0].SlotId,
            AppointmentDate = today,
            QrCodeToken = "HM175-SEED-APT-010",
            Status = AppointmentStatus.CHECKED_IN,
            IsWalkIn = true,
            CheckedInAt = nowUtc.AddHours(-3),
            CreatedAt = nowUtc.AddHours(-3)
        };
        context.DonationAppointments.Add(aptCompleted);
        context.PreScreeningSurveys.Add(new PreScreeningSurvey
        {
            AppointmentId = aptCompleted.AppointmentId,
            SurveyAnswers = safeSurveyJson,
            RiskScore = 0,
            HasRisk = false,
            SurveySubmittedAt = nowUtc.AddHours(-3).AddMinutes(5),
            DoctorId = doctorId,
            WeightKg = 59.0m,
            SystolicBloodPressure = 118,
            DiastolicBloodPressure = 75,
            Pulse = 72,
            HemoglobinLevel = 136.0m,
            BodyTemp = 36.5m,
            RapidHbsAgNegative = true,
            ClinicalNotes = "Tình nguyện viên vãng lai đủ điều kiện hiến 350ml.",
            ScreeningStatus = ScreeningStatus.QUALIFIED,
            TargetVolumeMl = TargetVolume.VOL_350,
            ScreenedAt = nowUtc.AddHours(-2).AddMinutes(-40)
        });
        context.DonationSessions.Add(new DonationSession
        {
            AppointmentId = aptCompleted.AppointmentId,
            PhlebotomistId = donorStaffId,
            TargetVolumeMl = TargetVolume.VOL_350,
            ActualVolumeMl = 350,
            Status = SessionStatus.COMPLETED,
            CreatedAt = nowUtc.AddHours(-2).AddMinutes(-35),
            CompletedAt = nowUtc.AddHours(-2).AddMinutes(-15)
        });

        // 11. Phiên lấy máu ngừng giữa chừng do sự cố (ABORTED - ngày hôm qua)
        var aptAborted = new DonationAppointment
        {
            DonorId = donorMap["079092001003"].DonorId,
            SlotId = mainSlots[0].SlotId,
            AppointmentDate = today.AddDays(-1),
            QrCodeToken = "HM175-SEED-APT-011",
            Status = AppointmentStatus.CHECKED_IN,
            IsWalkIn = false,
            CheckedInAt = nowUtc.AddDays(-1).AddHours(-3),
            CreatedAt = nowUtc.AddDays(-2)
        };
        context.DonationAppointments.Add(aptAborted);
        context.PreScreeningSurveys.Add(new PreScreeningSurvey
        {
            AppointmentId = aptAborted.AppointmentId,
            SurveyAnswers = safeSurveyJson,
            RiskScore = 0,
            HasRisk = false,
            SurveySubmittedAt = nowUtc.AddDays(-1).AddHours(-3),
            DoctorId = doctorId,
            WeightKg = 64.0m,
            SystolicBloodPressure = 115,
            DiastolicBloodPressure = 72,
            Pulse = 76,
            HemoglobinLevel = 138.0m,
            BodyTemp = 36.6m,
            RapidHbsAgNegative = true,
            ClinicalNotes = "Đủ điều kiện hiến 350ml.",
            ScreeningStatus = ScreeningStatus.QUALIFIED,
            TargetVolumeMl = TargetVolume.VOL_350,
            ScreenedAt = nowUtc.AddDays(-1).AddHours(-2).AddMinutes(-45)
        });
        context.DonationSessions.Add(new DonationSession
        {
            AppointmentId = aptAborted.AppointmentId,
            PhlebotomistId = donorStaffId,
            TargetVolumeMl = TargetVolume.VOL_350,
            ActualVolumeMl = 120,
            CollectionIncident = "Người hiến máu choáng nhẹ và đổ mồ hôi ở phút thứ 4; đã rút kim ngưng thu gom và theo dõi hồi sức ổn định.",
            Status = SessionStatus.ABORTED,
            CreatedAt = nowUtc.AddDays(-1).AddHours(-2).AddMinutes(-40),
            CompletedAt = nowUtc.AddDays(-1).AddHours(-2).AddMinutes(-30)
        });

        await context.SaveChangesAsync();
    }
}
