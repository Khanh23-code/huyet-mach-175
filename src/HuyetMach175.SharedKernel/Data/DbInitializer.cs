using HuyetMach175.Api.Data.Seeders;
using HuyetMach175.SharedKernel.Services;

namespace HuyetMach175.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        await SchemaSeeder.EnsureSchemaUpdatedAsync(context);
        await AuthSeeder.SeedAuthDataAsync(context, passwordHasher);
        await DonationSeeder.SeedAppointmentsAndSurveysAsync(context);
        await InventorySeeder.SeedInventoryDataAsync(context);
    }
}