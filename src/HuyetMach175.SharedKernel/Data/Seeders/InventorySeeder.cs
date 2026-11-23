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
        if (!await context.StorageLocations.AnyAsync())
        {
            var locations = new[]
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
                }
            };

            context.StorageLocations.AddRange(locations);
            await context.SaveChangesAsync();
        }

        var hasQuarantine = await context.StorageLocations.AnyAsync(l => l.Zone == StorageZone.QUARANTINE);
        if (!hasQuarantine)
        {
            context.StorageLocations.Add(new StorageLocation
            {
                RefrigeratorName = "Tủ biệt trữ chờ xét nghiệm 1",
                ShelfNumber = "Ngăn BT1",
                TargetTemperature = 4.0m,
                Zone = StorageZone.QUARANTINE,
                IsFull = false,
                IsActive = true
            });
        }

        var hasDisposalHold = await context.StorageLocations.AnyAsync(l => l.Zone == StorageZone.DISPOSAL_HOLD);
        if (!hasDisposalHold)
        {
            context.StorageLocations.Add(new StorageLocation
            {
                RefrigeratorName = "Tủ cách ly chờ hủy 1",
                ShelfNumber = "Ngăn CH1",
                TargetTemperature = 4.0m,
                Zone = StorageZone.DISPOSAL_HOLD,
                IsFull = false,
                IsActive = true
            });
        }

        if (!hasQuarantine || !hasDisposalHold)
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

        BloodComponentType? wb = null;
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

            if (item.TypeCode == ComponentCode.WB)
            {
                wb = entity;
            }
        }

        await context.SaveChangesAsync();

        if (wb == null)
        {
            wb = await context.BloodComponentTypes.FirstAsync(ct => ct.TypeCode == ComponentCode.WB);
        }

        if (await context.BloodBags.AnyAsync())
        {
            return;
        }

        var completedSession = await context.DonationSessions
            .FirstOrDefaultAsync(s => s.Status == SessionStatus.COMPLETED);

        var now = DateTime.UtcNow;

        var sampleBags = new[]
        {
            new BloodBag
            {
                Barcode = "BAG-20261102-00501",
                SessionId = completedSession?.SessionId,
                ComponentTypeId = wb.ComponentTypeId,
                LocationId = null,
                BloodType = BloodType.O,
                RhFactor = RhFactor.POSITIVE,
                VolumeMl = 350,
                CollectedAt = now.AddHours(-3),
                ExpiredAt = now.AddDays(35),
                Status = BagStatus.NEWLY_COLLECTED,
                CreatedAt = now.AddHours(-3)
            },
            new BloodBag
            {
                Barcode = "BAG-20261102-00502",
                SessionId = null,
                ComponentTypeId = wb.ComponentTypeId,
                LocationId = null,
                BloodType = BloodType.A,
                RhFactor = RhFactor.POSITIVE,
                VolumeMl = 450,
                CollectedAt = now.AddHours(-2),
                ExpiredAt = now.AddDays(35),
                Status = BagStatus.NEWLY_COLLECTED,
                CreatedAt = now.AddHours(-2)
            },
            new BloodBag
            {
                Barcode = "BAG-20261102-00503",
                SessionId = null,
                ComponentTypeId = wb.ComponentTypeId,
                LocationId = null,
                BloodType = BloodType.B,
                RhFactor = RhFactor.POSITIVE,
                VolumeMl = 250,
                CollectedAt = now.AddHours(-2),
                ExpiredAt = now.AddDays(35),
                Status = BagStatus.NEWLY_COLLECTED,
                CreatedAt = now.AddHours(-2)
            },
            new BloodBag
            {
                Barcode = "BAG-20261102-00504",
                SessionId = null,
                ComponentTypeId = wb.ComponentTypeId,
                LocationId = null,
                BloodType = BloodType.AB,
                RhFactor = RhFactor.POSITIVE,
                VolumeMl = 350,
                CollectedAt = now.AddHours(-1),
                ExpiredAt = now.AddDays(35),
                Status = BagStatus.NEWLY_COLLECTED,
                CreatedAt = now.AddHours(-1)
            },
            new BloodBag
            {
                Barcode = "BAG-20261102-00505",
                SessionId = null,
                ComponentTypeId = wb.ComponentTypeId,
                LocationId = null,
                BloodType = BloodType.O,
                RhFactor = RhFactor.NEGATIVE,
                VolumeMl = 450,
                CollectedAt = now.AddMinutes(-30),
                ExpiredAt = now.AddDays(35),
                Status = BagStatus.NEWLY_COLLECTED,
                CreatedAt = now.AddMinutes(-30)
            },
            new BloodBag
            {
                Barcode = "BAG-20261101-00401",
                SessionId = null,
                ComponentTypeId = wb.ComponentTypeId,
                LocationId = 1,
                BloodType = BloodType.O,
                RhFactor = RhFactor.POSITIVE,
                VolumeMl = 350,
                CollectedAt = now.AddDays(-1),
                ExpiredAt = now.AddDays(34),
                Status = BagStatus.READY_TESTING,
                CreatedAt = now.AddDays(-1)
            },
            new BloodBag
            {
                Barcode = "BAG-20261101-00402",
                SessionId = null,
                ComponentTypeId = wb.ComponentTypeId,
                LocationId = 2,
                BloodType = BloodType.A,
                RhFactor = RhFactor.POSITIVE,
                VolumeMl = 450,
                CollectedAt = now.AddDays(-1),
                ExpiredAt = now.AddDays(34),
                Status = BagStatus.READY_TESTING,
                CreatedAt = now.AddDays(-1)
            },
            new BloodBag
            {
                Barcode = "BAG-20261030-00301",
                SessionId = null,
                ComponentTypeId = wb.ComponentTypeId,
                LocationId = 1,
                BloodType = BloodType.B,
                RhFactor = RhFactor.POSITIVE,
                VolumeMl = 350,
                CollectedAt = now.AddDays(-3),
                ExpiredAt = now.AddDays(32),
                Status = BagStatus.TESTED_PASSED,
                CreatedAt = now.AddDays(-3)
            },
            new BloodBag
            {
                Barcode = "BAG-20261028-00201",
                SessionId = null,
                ComponentTypeId = wb.ComponentTypeId,
                LocationId = 1,
                BloodType = BloodType.O,
                RhFactor = RhFactor.POSITIVE,
                VolumeMl = 450,
                CollectedAt = now.AddDays(-5),
                ExpiredAt = now.AddDays(30),
                Status = BagStatus.AVAILABLE,
                CreatedAt = now.AddDays(-5)
            },
            new BloodBag
            {
                Barcode = "BAG-20261028-00202",
                SessionId = null,
                ComponentTypeId = wb.ComponentTypeId,
                LocationId = 2,
                BloodType = BloodType.A,
                RhFactor = RhFactor.POSITIVE,
                VolumeMl = 350,
                CollectedAt = now.AddDays(-5),
                ExpiredAt = now.AddDays(30),
                Status = BagStatus.AVAILABLE,
                CreatedAt = now.AddDays(-5)
            }
        };

        context.BloodBags.AddRange(sampleBags);
        await context.SaveChangesAsync();
    }
}