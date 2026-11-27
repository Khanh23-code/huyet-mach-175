using HuyetMach175.Api.Data.Seeders;
using HuyetMach175.SharedKernel.Services;

namespace HuyetMach175.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        await AuthSeeder.SeedAuthDataAsync(context, passwordHasher);
        await DonationSeeder.SeedDonationDataAsync(context);
        await InventorySeeder.SeedInventoryDataAsync(context);
        await ClinicalSeeder.SeedClinicalDataAsync(context);
    }
}
