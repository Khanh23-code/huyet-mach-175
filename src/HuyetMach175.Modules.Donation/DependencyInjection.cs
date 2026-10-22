using Microsoft.Extensions.DependencyInjection;

namespace HuyetMach175.Modules.Donation;

public static class DependencyInjection
{
    public static IServiceCollection AddDonationModule(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly);

        return services;
    }
}
