using HuyetMach175.Modules.Inventory.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HuyetMach175.Modules.Inventory;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddScoped<IStorageLocationsService, StorageLocationsService>();

        services.AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly);

        return services;
    }
}