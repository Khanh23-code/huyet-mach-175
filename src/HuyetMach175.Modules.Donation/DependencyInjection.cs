using HuyetMach175.Modules.Donation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HuyetMach175.Modules.Donation;

public static class DependencyInjection
{
    public static IServiceCollection AddDonationModule(this IServiceCollection services)
    {
        services.AddScoped<ICampaignsService, CampaignsService>();
        services.AddScoped<IDonorsService, DonorsService>();
        services.AddScoped<ISitesService, SitesService>();
        services.AddScoped<IProposalsService, ProposalsService>();

        services.AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly);

        return services;
    }
}
