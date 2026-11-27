using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Api.Data.Seeders;

public static class InventorySeeder
{
    public static async Task SeedInventoryDataAsync(ApplicationDbContext context)
    {
        await SeedStorageLocationsAsync(context);
        await SeedBloodComponentTypesAndBagsAsync(context);
    }

    public static async Task SeedStorageLocationsAsync(ApplicationDbContext context)
    {
        var defaultLocations = new[]
        {
            new StorageLocation
            {
                RefrigeratorName = "Tủ lạnh bảo quản máu toàn phần 1",
                ShelfNumber = "Ngăn A1",
                TargetTemperature = 4.0m,
                Zone = StorageZone.RELEASED,
                IsFull = false,
                IsActive = true
            },
            new StorageLocation
            {
                RefrigeratorName = "Tủ lạnh bảo quản máu toàn phần 1",
                ShelfNumber = "Ngăn A2",
                TargetTemperature = 4.0m,
                Zone = StorageZone.RELEASED,
                IsFull = false,
                IsActive = true
            },
            new StorageLocation
            {
                RefrigeratorName = "Tủ lạnh bảo quản khối hồng cầu 1",
                ShelfNumber = "Ngăn B1",
                TargetTemperature = 4.0m,
                Zone = StorageZone.RELEASED,
                IsFull = false,
                IsActive = true
            },
            new StorageLocation
            {
                RefrigeratorName = "Tủ đông huyết tương 1",
                ShelfNumber = "Ngăn C1",
                TargetTemperature = -25.0m,
                Zone = StorageZone.RELEASED,
                IsFull = false,
                IsActive = true
            },
            new StorageLocation
            {
                RefrigeratorName = "Tủ lắc tiểu cầu 1",
                ShelfNumber = "Kệ T1",
                TargetTemperature = 22.0m,
                Zone = StorageZone.RELEASED,
                IsFull = false,
                IsActive = true
            },
            new StorageLocation
            {
                RefrigeratorName = "Tủ lắc tiểu cầu 1",
                ShelfNumber = "Kệ T2",
                TargetTemperature = 22.0m,
                Zone = StorageZone.RELEASED,
                IsFull = false,
                IsActive = false
            },
            new StorageLocation
            {
                RefrigeratorName = "Tủ biệt trữ chờ xét nghiệm 1",
                ShelfNumber = "Ngăn BT1",
                TargetTemperature = 4.0m,
                Zone = StorageZone.QUARANTINE,
                IsFull = false,
                IsActive = true
            },
            new StorageLocation
            {
                RefrigeratorName = "Tủ cách ly chờ hủy 1",
                ShelfNumber = "Ngăn CH1",
                TargetTemperature = 4.0m,
                Zone = StorageZone.DISPOSAL_HOLD,
                IsFull = false,
                IsActive = true
            }
        };

        var existingLocations = await context.StorageLocations.ToListAsync();
        var added = false;

        foreach (var loc in defaultLocations)
        {
            var exists = existingLocations.Any(e =>
                e.RefrigeratorName == loc.RefrigeratorName &&
                e.ShelfNumber == loc.ShelfNumber);

            if (!exists)
            {
                context.StorageLocations.Add(loc);
                added = true;
            }
        }

        if (added)
        {
            await context.SaveChangesAsync();
        }
    }

    public static async Task SeedBloodComponentTypesAndBagsAsync(ApplicationDbContext context)
    {
        var componentDefaults = new[]
        {
            new
            {
                TypeCode = ComponentCode.WB,
                TypeName = "Máu toàn phần",
                LifeDays = 35,
                StorageTemperature = "2°C - 6°C",
                Description = "Máu toàn phần thu gom từ người hiến máu tình nguyện",
                MinTemp = 2.0m,
                MaxTemp = 6.0m,
                AllowRestock = true,
                MaxOutsideMinutesForRestock = (int?)30,
                MaxRestockCount = 1,
                CompatibilityRuleType = CompatibilityRuleType.RBC
            },
            new
            {
                TypeCode = ComponentCode.PRBC,
                TypeName = "Khối hồng cầu",
                LifeDays = 42,
                StorageTemperature = "2°C - 6°C",
                Description = "Khối hồng cầu sau khi tách huyết tương",
                MinTemp = 2.0m,
                MaxTemp = 6.0m,
                AllowRestock = true,
                MaxOutsideMinutesForRestock = (int?)30,
                MaxRestockCount = 1,
                CompatibilityRuleType = CompatibilityRuleType.RBC
            },
            new
            {
                TypeCode = ComponentCode.FFP,
                TypeName = "Huyết tương tươi đông lạnh",
                LifeDays = 365,
                StorageTemperature = "-25°C hoặc thấp hơn",
                Description = "Huyết tương tươi đông lạnh giàu yếu tố đông máu",
                MinTemp = -40.0m,
                MaxTemp = -18.0m,
                AllowRestock = false,
                MaxOutsideMinutesForRestock = (int?)null,
                MaxRestockCount = 0,
                CompatibilityRuleType = CompatibilityRuleType.PLASMA
            },
            new
            {
                TypeCode = ComponentCode.PLT,
                TypeName = "Khối tiểu cầu",
                LifeDays = 5,
                StorageTemperature = "20°C - 24°C (lắc liên tục)",
                Description = "Khối tiểu cầu từ máu toàn phần hoặc gạn tách",
                MinTemp = 20.0m,
                MaxTemp = 24.0m,
                AllowRestock = false,
                MaxOutsideMinutesForRestock = (int?)null,
                MaxRestockCount = 0,
                CompatibilityRuleType = CompatibilityRuleType.PLATELET
            }
        };

        var existingTypes = await context.BloodComponentTypes
            .ToDictionaryAsync(ct => ct.TypeCode);

        foreach (var item in componentDefaults)
        {
            if (!existingTypes.TryGetValue(item.TypeCode, out var entity))
            {
                entity = new BloodComponentType
                {
                    TypeCode = item.TypeCode,
                    TypeName = item.TypeName,
                    LifeDays = item.LifeDays,
                    StorageTemperature = item.StorageTemperature,
                    Description = item.Description,
                    MinTemp = item.MinTemp,
                    MaxTemp = item.MaxTemp,
                    AllowRestock = item.AllowRestock,
                    MaxOutsideMinutesForRestock = item.MaxOutsideMinutesForRestock,
                    MaxRestockCount = item.MaxRestockCount,
                    CompatibilityRuleType = item.CompatibilityRuleType
                };
                context.BloodComponentTypes.Add(entity);
            }
            else if (entity.MinTemp == 0m && entity.MaxTemp == 0m)
            {
                entity.MinTemp = item.MinTemp;
                entity.MaxTemp = item.MaxTemp;
                entity.AllowRestock = item.AllowRestock;
                entity.MaxOutsideMinutesForRestock = item.MaxOutsideMinutesForRestock;
                entity.MaxRestockCount = item.MaxRestockCount;
                entity.CompatibilityRuleType = item.CompatibilityRuleType;
            }
        }

        await context.SaveChangesAsync();

        if (await context.BloodBags.AnyAsync(b => b.Barcode.StartsWith("BAG-SEED-")))
        {
            return;
        }

        var componentMap = await context.BloodComponentTypes.ToDictionaryAsync(c => c.TypeCode);
        var wb = componentMap[ComponentCode.WB];
        var prbc = componentMap[ComponentCode.PRBC];
        var ffp = componentMap[ComponentCode.FFP];
        var plt = componentMap[ComponentCode.PLT];

        var locations = await context.StorageLocations.ToListAsync();
        Guid LocId(string shelf) => locations.First(l => l.ShelfNumber == shelf).LocationId;

        var shelfA1 = LocId("Ngăn A1");
        var shelfB1 = LocId("Ngăn B1");
        var shelfC1 = LocId("Ngăn C1");
        var shelfT1 = LocId("Kệ T1");
        var shelfQuarantine = LocId("Ngăn BT1");
        var shelfDisposalHold = LocId("Ngăn CH1");

        var bloodbankId = await context.Users
            .Where(u => u.Username == "bloodbank")
            .Select(u => u.UserId)
            .FirstOrDefaultAsync();

        var disposalTesterId = await context.Users
            .Where(u => u.Username == "disposal_tester")
            .Select(u => u.UserId)
            .FirstOrDefaultAsync();

        var managerId = await context.Users
            .Where(u => u.Username == "manager")
            .Select(u => (Guid?)u.UserId)
            .FirstOrDefaultAsync();

        var completedSession = await context.DonationSessions
            .FirstOrDefaultAsync(s => s.Status == SessionStatus.COMPLETED);

        var now = DateTime.UtcNow;

        // 1. NEWLY_COLLECTED (chờ tiếp nhận về kho biệt trữ)
        var bagNc1 = new BloodBag
        {
            Barcode = "BAG-SEED-NC-001",
            SessionId = completedSession?.SessionId,
            ComponentTypeId = wb.ComponentTypeId,
            LocationId = null,
            BloodType = BloodType.O,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 350,
            CollectedAt = now.AddHours(-2),
            ExpiredAt = now.AddDays(35),
            Status = BagStatus.NEWLY_COLLECTED,
            CreatedAt = now.AddHours(-2)
        };

        var bagNc2 = new BloodBag
        {
            Barcode = "BAG-SEED-NC-002",
            ComponentTypeId = wb.ComponentTypeId,
            LocationId = null,
            BloodType = BloodType.A,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 450,
            CollectedAt = now.AddHours(-1),
            ExpiredAt = now.AddDays(35),
            Status = BagStatus.NEWLY_COLLECTED,
            CreatedAt = now.AddHours(-1)
        };

        // 2. READY_TESTING (đang nằm tại Ngăn BT1 chờ nhập kết quả xét nghiệm)
        var bagRt1 = new BloodBag
        {
            Barcode = "BAG-SEED-RT-001",
            ComponentTypeId = wb.ComponentTypeId,
            LocationId = shelfQuarantine,
            BloodType = BloodType.O,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 350,
            CollectedAt = now.AddDays(-1),
            ExpiredAt = now.AddDays(34),
            Status = BagStatus.READY_TESTING,
            CreatedAt = now.AddDays(-1)
        };

        var bagRt2 = new BloodBag
        {
            Barcode = "BAG-SEED-RT-002",
            ComponentTypeId = wb.ComponentTypeId,
            LocationId = shelfQuarantine,
            BloodType = BloodType.B,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 450,
            CollectedAt = now.AddDays(-1),
            ExpiredAt = now.AddDays(34),
            Status = BagStatus.READY_TESTING,
            CreatedAt = now.AddDays(-1)
        };

        // 3. TESTED_PASSED (đạt xét nghiệm âm tính, chờ điều chế tách thành phần hoặc nhập kho sạch)
        var bagTp1 = new BloodBag
        {
            Barcode = "BAG-SEED-TP-001",
            ComponentTypeId = wb.ComponentTypeId,
            LocationId = shelfQuarantine,
            BloodType = BloodType.A,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 450,
            CollectedAt = now.AddDays(-2),
            ExpiredAt = now.AddDays(33),
            Status = BagStatus.TESTED_PASSED,
            CreatedAt = now.AddDays(-2)
        };

        var bagTpExt = new BloodBag
        {
            Barcode = "BAG-SEED-TP-EXT-002",
            ComponentTypeId = wb.ComponentTypeId,
            LocationId = shelfQuarantine,
            BloodType = BloodType.O,
            RhFactor = RhFactor.NEGATIVE,
            VolumeMl = 350,
            CollectedAt = now.AddDays(-2),
            ExpiredAt = now.AddDays(33),
            Status = BagStatus.TESTED_PASSED,
            ExternalUnitName = "Bệnh viện Truyền máu Huyết học TP.HCM",
            ExternalCode = "BVTMHH-2611-089",
            CreatedAt = now.AddDays(-2)
        };

        // 4. PROCESSED (túi mẹ đã tách thành PRBC + FFP để kiểm thử cây phả hệ túi máu)
        var bagParentProcessed = new BloodBag
        {
            Barcode = "BAG-SEED-PROC-001",
            ComponentTypeId = wb.ComponentTypeId,
            LocationId = null,
            BloodType = BloodType.O,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 450,
            CollectedAt = now.AddDays(-5),
            ExpiredAt = now.AddDays(30),
            Status = BagStatus.PROCESSED,
            CreatedAt = now.AddDays(-5)
        };

        // 5. AVAILABLE (tồn kho sạch đủ 4 chế phẩm + 2 túi cận hạn < 48h cho màn Cảnh báo hết hạn)
        var bagAvWb = new BloodBag
        {
            Barcode = "BAG-SEED-AV-WB-01",
            ComponentTypeId = wb.ComponentTypeId,
            LocationId = shelfA1,
            BloodType = BloodType.O,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 350,
            CollectedAt = now.AddDays(-6),
            ExpiredAt = now.AddDays(29),
            Status = BagStatus.AVAILABLE,
            CreatedAt = now.AddDays(-6)
        };

        var bagAvPrbcFromParent = new BloodBag
        {
            Barcode = "BAG-SEED-AV-PRBC-01",
            ParentBagId = bagParentProcessed.BagId,
            ComponentTypeId = prbc.ComponentTypeId,
            LocationId = shelfB1,
            BloodType = BloodType.O,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 250,
            CollectedAt = now.AddDays(-5),
            ExpiredAt = now.AddDays(37),
            Status = BagStatus.AVAILABLE,
            CreatedAt = now.AddDays(-5)
        };

        var bagAvPrbcA = new BloodBag
        {
            Barcode = "BAG-SEED-AV-PRBC-02",
            ComponentTypeId = prbc.ComponentTypeId,
            LocationId = shelfB1,
            BloodType = BloodType.A,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 250,
            CollectedAt = now.AddDays(-7),
            ExpiredAt = now.AddDays(35),
            Status = BagStatus.AVAILABLE,
            CreatedAt = now.AddDays(-7)
        };

        var bagAvPrbcB = new BloodBag
        {
            Barcode = "BAG-SEED-AV-PRBC-03",
            ComponentTypeId = prbc.ComponentTypeId,
            LocationId = shelfB1,
            BloodType = BloodType.B,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 250,
            CollectedAt = now.AddDays(-8),
            ExpiredAt = now.AddDays(34),
            Status = BagStatus.AVAILABLE,
            CreatedAt = now.AddDays(-8)
        };

        var bagAvPrbcONeg = new BloodBag
        {
            Barcode = "BAG-SEED-AV-PRBC-04",
            ComponentTypeId = prbc.ComponentTypeId,
            LocationId = shelfB1,
            BloodType = BloodType.O,
            RhFactor = RhFactor.NEGATIVE,
            VolumeMl = 250,
            CollectedAt = now.AddDays(-4),
            ExpiredAt = now.AddDays(38),
            Status = BagStatus.AVAILABLE,
            CreatedAt = now.AddDays(-4)
        };

        var bagAvFfpFromParent = new BloodBag
        {
            Barcode = "BAG-SEED-AV-FFP-01",
            ParentBagId = bagParentProcessed.BagId,
            ComponentTypeId = ffp.ComponentTypeId,
            LocationId = shelfC1,
            BloodType = BloodType.O,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 200,
            CollectedAt = now.AddDays(-5),
            ExpiredAt = now.AddDays(360),
            Status = BagStatus.AVAILABLE,
            CreatedAt = now.AddDays(-5)
        };

        var bagAvFfpAb = new BloodBag
        {
            Barcode = "BAG-SEED-AV-FFP-02",
            ComponentTypeId = ffp.ComponentTypeId,
            LocationId = shelfC1,
            BloodType = BloodType.AB,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 200,
            CollectedAt = now.AddDays(-10),
            ExpiredAt = now.AddDays(355),
            Status = BagStatus.AVAILABLE,
            CreatedAt = now.AddDays(-10)
        };

        // 2 túi cận hạn trong vòng 48 giờ để hiển thị ngay ở trang Cảnh báo sắp hết hạn
        var bagExpiringPlt = new BloodBag
        {
            Barcode = "BAG-SEED-AV-PLT-01",
            ComponentTypeId = plt.ComponentTypeId,
            LocationId = shelfT1,
            BloodType = BloodType.A,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 150,
            CollectedAt = now.AddDays(-4),
            ExpiredAt = now.AddHours(18),
            Status = BagStatus.AVAILABLE,
            CreatedAt = now.AddDays(-4)
        };

        var bagExpiringPrbc = new BloodBag
        {
            Barcode = "BAG-SEED-AV-PRBC-05",
            ComponentTypeId = prbc.ComponentTypeId,
            LocationId = shelfB1,
            BloodType = BloodType.AB,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 250,
            CollectedAt = now.AddDays(-40),
            ExpiredAt = now.AddHours(30),
            Status = BagStatus.AVAILABLE,
            CreatedAt = now.AddDays(-40)
        };

        // 6. Túi LOCKED độc lập chưa thuộc biên bản hủy -> hiển thị ngay ở tab "Túi chờ hủy"
        var bagLockedHiv = new BloodBag
        {
            Barcode = "BAG-SEED-LCK-HIV-01",
            ComponentTypeId = wb.ComponentTypeId,
            LocationId = shelfDisposalHold,
            BloodType = BloodType.B,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 350,
            CollectedAt = now.AddDays(-3),
            ExpiredAt = now.AddDays(32),
            Status = BagStatus.LOCKED,
            DiscardReason = "Dương tính sàng lọc HIV",
            CreatedAt = now.AddDays(-3)
        };

        var bagLockedExpired = new BloodBag
        {
            Barcode = "BAG-SEED-LCK-EXP-02",
            ComponentTypeId = plt.ComponentTypeId,
            LocationId = shelfDisposalHold,
            BloodType = BloodType.O,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 150,
            CollectedAt = now.AddDays(-6),
            ExpiredAt = now.AddHours(-12),
            Status = BagStatus.LOCKED,
            DiscardReason = "Quá hạn sử dụng chế phẩm tiểu cầu",
            CreatedAt = now.AddDays(-6)
        };

        // 7. Túi gắn với 4 biên bản tiêu hủy ở đủ 4 trạng thái (PENDING_APPROVAL, APPROVED, DISPOSED, REJECTED)
        var bagDspPending = new BloodBag
        {
            Barcode = "BAG-SEED-DSP-001",
            ComponentTypeId = prbc.ComponentTypeId,
            LocationId = shelfDisposalHold,
            BloodType = BloodType.A,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 250,
            CollectedAt = now.AddDays(-45),
            ExpiredAt = now.AddDays(-3),
            Status = BagStatus.LOCKED,
            DiscardReason = "Túi máu hết hạn bảo quản",
            CreatedAt = now.AddDays(-45)
        };

        var bagDspApproved = new BloodBag
        {
            Barcode = "BAG-SEED-DSP-002",
            ComponentTypeId = prbc.ComponentTypeId,
            LocationId = shelfDisposalHold,
            BloodType = BloodType.O,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 250,
            CollectedAt = now.AddDays(-12),
            ExpiredAt = now.AddDays(30),
            Status = BagStatus.LOCKED,
            DiscardReason = "Phát hiện huyết tán khi kiểm tra cảm quan định kỳ",
            CreatedAt = now.AddDays(-12)
        };

        var bagDspDisposed = new BloodBag
        {
            Barcode = "BAG-SEED-DSP-003",
            ComponentTypeId = wb.ComponentTypeId,
            LocationId = null,
            BloodType = BloodType.A,
            RhFactor = RhFactor.NEGATIVE,
            VolumeMl = 350,
            CollectedAt = now.AddDays(-15),
            ExpiredAt = now.AddDays(20),
            Status = BagStatus.DISCARDED,
            DiscardReason = "Dương tính sàng lọc HBV",
            CreatedAt = now.AddDays(-15)
        };

        var bagDspRejected = new BloodBag
        {
            Barcode = "BAG-SEED-DSP-004",
            ComponentTypeId = ffp.ComponentTypeId,
            LocationId = shelfDisposalHold,
            BloodType = BloodType.B,
            RhFactor = RhFactor.POSITIVE,
            VolumeMl = 200,
            CollectedAt = now.AddDays(-20),
            ExpiredAt = now.AddDays(345),
            Status = BagStatus.LOCKED,
            DiscardReason = "Nghi ngờ rạn cổ túi khi cấp đông",
            CreatedAt = now.AddDays(-20)
        };

        context.BloodBags.AddRange(
            bagNc1, bagNc2,
            bagRt1, bagRt2,
            bagTp1, bagTpExt,
            bagParentProcessed,
            bagAvWb, bagAvPrbcFromParent, bagAvPrbcA, bagAvPrbcB, bagAvPrbcONeg,
            bagAvFfpFromParent, bagAvFfpAb,
            bagExpiringPlt, bagExpiringPrbc,
            bagLockedHiv, bagLockedExpired,
            bagDspPending, bagDspApproved, bagDspDisposed, bagDspRejected);

        await context.SaveChangesAsync();

        if (bloodbankId != Guid.Empty)
        {
            context.BloodTestResults.AddRange(
                new BloodTestResult
                {
                    BagId = bagTp1.BagId,
                    TechnicianId = bloodbankId,
                    HivResult = TestResult.NEGATIVE,
                    HbvResult = TestResult.NEGATIVE,
                    HcvResult = TestResult.NEGATIVE,
                    SyphilisResult = TestResult.NEGATIVE,
                    IrregularAntibody = TestResult.NEGATIVE,
                    ConfirmedBloodType = BloodType.A,
                    ConfirmedRh = RhFactor.POSITIVE,
                    OverallConclusion = OverallConclusion.PASSED,
                    TestedAt = now.AddDays(-1)
                },
                new BloodTestResult
                {
                    BagId = bagTpExt.BagId,
                    TechnicianId = bloodbankId,
                    HivResult = TestResult.NEGATIVE,
                    HbvResult = TestResult.NEGATIVE,
                    HcvResult = TestResult.NEGATIVE,
                    SyphilisResult = TestResult.NEGATIVE,
                    IrregularAntibody = TestResult.NEGATIVE,
                    ConfirmedBloodType = BloodType.O,
                    ConfirmedRh = RhFactor.NEGATIVE,
                    OverallConclusion = OverallConclusion.PASSED,
                    TestedAt = now.AddDays(-1)
                },
                new BloodTestResult
                {
                    BagId = bagParentProcessed.BagId,
                    TechnicianId = bloodbankId,
                    HivResult = TestResult.NEGATIVE,
                    HbvResult = TestResult.NEGATIVE,
                    HcvResult = TestResult.NEGATIVE,
                    SyphilisResult = TestResult.NEGATIVE,
                    IrregularAntibody = TestResult.NEGATIVE,
                    ConfirmedBloodType = BloodType.O,
                    ConfirmedRh = RhFactor.POSITIVE,
                    OverallConclusion = OverallConclusion.PASSED,
                    TestedAt = now.AddDays(-5)
                },
                new BloodTestResult
                {
                    BagId = bagLockedHiv.BagId,
                    TechnicianId = bloodbankId,
                    HivResult = TestResult.POSITIVE,
                    HbvResult = TestResult.NEGATIVE,
                    HcvResult = TestResult.NEGATIVE,
                    SyphilisResult = TestResult.NEGATIVE,
                    IrregularAntibody = TestResult.NEGATIVE,
                    ConfirmedBloodType = BloodType.B,
                    ConfirmedRh = RhFactor.POSITIVE,
                    OverallConclusion = OverallConclusion.FAILED,
                    TestedAt = now.AddDays(-2)
                });

            var dateStamp = now.ToString("yyMMdd");
            var requester2 = disposalTesterId != Guid.Empty ? disposalTesterId : bloodbankId;

            var dspPending = new BloodDisposalRecord
            {
                DisposalCode = $"DSP-{dateStamp}-0001",
                Reason = DisposalReason.EXPIRED,
                Notes = "Đề xuất tiêu hủy túi khối hồng cầu quá hạn bảo quản tại kho.",
                Status = DisposalStatus.PENDING_APPROVAL,
                TotalBags = 1,
                RequestedBy = bloodbankId,
                RequestedAt = now.AddHours(-5),
                Items = new List<BloodDisposalItem>
                {
                    new() { BagId = bagDspPending.BagId, PreviousStatus = BagStatus.LOCKED, IsVerified = true, VerifiedAt = now.AddHours(-5) }
                }
            };

            var dspApproved = new BloodDisposalRecord
            {
                DisposalCode = $"DSP-{dateStamp}-0002",
                Reason = DisposalReason.HEMOLYSIS,
                Notes = "Túi máu có dấu hiệu huyết tán rõ, đã được Ban Giám đốc duyệt chờ bàn giao lò đốt.",
                Status = DisposalStatus.APPROVED,
                TotalBags = 1,
                RequestedBy = bloodbankId,
                RequestedAt = now.AddDays(-1),
                ApprovedBy = managerId,
                ApprovedAt = now.AddHours(-3),
                Items = new List<BloodDisposalItem>
                {
                    new() { BagId = bagDspApproved.BagId, PreviousStatus = BagStatus.LOCKED, IsVerified = true, VerifiedAt = now.AddDays(-1) }
                }
            };

            var dspDisposed = new BloodDisposalRecord
            {
                DisposalCode = $"DSP-{dateStamp}-0003",
                Reason = DisposalReason.INFECTIOUS_POSITIVE,
                Notes = "Đã hoàn tất tiêu hủy vật lý túi máu dương tính HBV theo quy trình kiểm soát nhiễm khuẩn.",
                Status = DisposalStatus.DISPOSED,
                TotalBags = 1,
                RequestedBy = bloodbankId,
                RequestedAt = now.AddDays(-4),
                ApprovedBy = managerId,
                ApprovedAt = now.AddDays(-3),
                Items = new List<BloodDisposalItem>
                {
                    new() { BagId = bagDspDisposed.BagId, PreviousStatus = BagStatus.LOCKED, IsVerified = true, VerifiedAt = now.AddDays(-4) }
                }
            };

            var dspRejected = new BloodDisposalRecord
            {
                DisposalCode = $"DSP-{dateStamp}-0004",
                Reason = DisposalReason.DAMAGED_BAG,
                Notes = "Đề xuất hủy túi huyết tương nghi ngờ rạn cổ túi.",
                Status = DisposalStatus.REJECTED,
                TotalBags = 1,
                RequestedBy = requester2,
                RequestedAt = now.AddDays(-2),
                ApprovedBy = managerId,
                ApprovedAt = now.AddDays(-1),
                RejectionReason = "Cần đính kèm biên bản kiểm tra ngoại quan của tổ bảo quản lạnh trước khi phê duyệt hủy.",
                Items = new List<BloodDisposalItem>
                {
                    new() { BagId = bagDspRejected.BagId, PreviousStatus = BagStatus.LOCKED, IsVerified = true, VerifiedAt = now.AddDays(-2) }
                }
            };

            context.BloodDisposalRecords.AddRange(dspPending, dspApproved, dspDisposed, dspRejected);
            await context.SaveChangesAsync();
        }
    }
}
