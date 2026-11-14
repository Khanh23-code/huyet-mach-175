using HuyetMach175.Modules.Clinical.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HuyetMach175.Modules.Clinical;

public static class DependencyInjection
{
    public static IServiceCollection AddClinicalModule(this IServiceCollection services)
    {
        services.AddScoped<IBloodRequestsService, BloodRequestsService>();
        services.AddScoped<IBloodCoordinationService, BloodCoordinationService>();

        services.AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly);

        return services;
    }
}
