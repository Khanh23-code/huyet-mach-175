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
        if (await context.StorageLocations.AnyAsync())
        {
            return;
        }

        var locations = new[]
        {
            new StorageLocation
            {
                RefrigeratorName = "Tủ lạnh bảo quản máu toàn phần 1",
                ShelfNumber = "Ngăn A1",
                TargetTemperature = 4.0m,
                IsFull = false,
                IsActive = true
            },
            new StorageLocation
            {
                RefrigeratorName = "Tủ lạnh bảo quản máu toàn phần 1",
                ShelfNumber = "Ngăn A2",
                TargetTemperature = 4.0m,
                IsFull = false,
                IsActive = true
            },
            new StorageLocation
            {
                RefrigeratorName = "Tủ lạnh bảo quản khối hồng cầu 1",
                ShelfNumber = "Ngăn B1",
                TargetTemperature = 4.0m,
                IsFull = false,
                IsActive = true
            },
            new StorageLocation
            {
                RefrigeratorName = "Tủ đông huyết tương 1",
                ShelfNumber = "Ngăn C1",
                TargetTemperature = -25.0m,
                IsFull = false,
                IsActive = true
            },
            new StorageLocation
            {
                RefrigeratorName = "Tủ lắc tiểu cầu 1",
                ShelfNumber = "Kệ T1",
                TargetTemperature = 22.0m,
                IsFull = false,
                IsActive = true
            }
        };

        context.StorageLocations.AddRange(locations);
        await context.SaveChangesAsync();
    }

    public static async Task SeedBloodComponentTypesAndBagsAsync(ApplicationDbContext context)
    {
        var wb = await context.BloodComponentTypes.FirstOrDefaultAsync(ct => ct.TypeCode == ComponentCode.WB);
        if (wb == null)
        {
            wb = new BloodComponentType
            {
                TypeCode = ComponentCode.WB,
                TypeName = "Máu toàn phần",
                LifeDays = 35,
                StorageTemperature = "2°C - 6°C",
                Description = "Máu toàn phần thu gom từ người hiến máu tình nguyện"
            };
            context.BloodComponentTypes.Add(wb);
        }

        if (!await context.BloodComponentTypes.AnyAsync(ct => ct.TypeCode == ComponentCode.PRBC))
        {
            context.BloodComponentTypes.Add(new BloodComponentType
            {
                TypeCode = ComponentCode.PRBC,
                TypeName = "Khối hồng cầu",
                LifeDays = 42,
                StorageTemperature = "2°C - 6°C",
                Description = "Khối hồng cầu sau khi tách huyết tương"
            });
        }

        if (!await context.BloodComponentTypes.AnyAsync(ct => ct.TypeCode == ComponentCode.FFP))
        {
            context.BloodComponentTypes.Add(new BloodComponentType
            {
                TypeCode = ComponentCode.FFP,
                TypeName = "Huyết tương tươi đông lạnh",
                LifeDays = 365,
                StorageTemperature = "-25°C hoặc thấp hơn",
                Description = "Huyết tương tươi đông lạnh giàu yếu tố đông máu"
            });
        }

        if (!await context.BloodComponentTypes.AnyAsync(ct => ct.TypeCode == ComponentCode.PLT))
        {
            context.BloodComponentTypes.Add(new BloodComponentType
            {
                TypeCode = ComponentCode.PLT,
                TypeName = "Khối tiểu cầu",
                LifeDays = 5,
                StorageTemperature = "20°C - 24°C (lắc liên tục)",
                Description = "Khối tiểu cầu từ máu toàn phần hoặc gạn tách"
            });
        }

        await context.SaveChangesAsync();

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