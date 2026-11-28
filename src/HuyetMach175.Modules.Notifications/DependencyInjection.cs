using HuyetMach175.Modules.Notifications.Email;
using HuyetMach175.Modules.Notifications.Options;
using HuyetMach175.Modules.Notifications.Services;
using HuyetMach175.Modules.Notifications.Workers;
using HuyetMach175.SharedKernel.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HuyetMach175.Modules.Notifications;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddSingleton<IValidateOptions<SmtpOptions>, SmtpOptionsValidator>();
        services.AddOptions<SmtpOptions>().ValidateOnStart();

        services.Configure<NotificationDeliveryOptions>(
            configuration.GetSection(NotificationDeliveryOptions.SectionName));

        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<INotificationEmailTemplateRenderer, NotificationEmailTemplateRenderer>();

        services.AddScoped<INotificationIngestionService, NotificationIngestionService>();
        services.AddScoped<INotificationDeliveryQueueStore, EfNotificationDeliveryQueueStore>();
        services.AddScoped<INotificationsInboxService, NotificationsInboxService>();

        services.AddHostedService<NotificationDeliveryWorker>();

        services.AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly);

        return services;
    }
}
