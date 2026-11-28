using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HuyetMach175.Modules.Notifications.Options;

public enum SmtpSecurityMode
{
    None,
    StartTls,
    SslOnConnect
}

public sealed class SmtpOptions
{
    public const string SectionName = "Notifications:Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Hệ thống Huyết Mạch 175";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public SmtpSecurityMode Security { get; set; } = SmtpSecurityMode.StartTls;
    public int TimeoutSeconds { get; set; } = 30;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(FromAddress) &&
        (Security == SmtpSecurityMode.None ||
         (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password)));
}

public sealed class SmtpOptionsValidator : IValidateOptions<SmtpOptions>
{
    private readonly IHostEnvironment _environment;

    public SmtpOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, SmtpOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host) && string.IsNullOrWhiteSpace(options.FromAddress))
        {
            // Cho phép để trống ở local/dev khi chưa điền Gmail App Password (Worker sẽ tạm nghỉ an toàn)
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            failures.Add("Notifications:Smtp:Host không được để trống khi đã bật cấu hình SMTP.");
        }

        if (options.Port is <= 0 or > 65535)
        {
            failures.Add("Notifications:Smtp:Port phải nằm trong khoảng 1..65535.");
        }

        if (string.IsNullOrWhiteSpace(options.FromAddress))
        {
            failures.Add("Notifications:Smtp:FromAddress không được để trống khi đã bật cấu hình SMTP.");
        }

        var hasUsername = !string.IsNullOrWhiteSpace(options.Username);
        var hasPassword = !string.IsNullOrWhiteSpace(options.Password);
        if (hasUsername != hasPassword)
        {
            failures.Add("Notifications:Smtp:Username và Password phải được cung cấp đồng thời.");
        }

        if (!_environment.IsDevelopment() && options.Security == SmtpSecurityMode.None)
        {
            failures.Add("Không cho phép SmtpSecurityMode.None trên môi trường Production (bắt buộc StartTls hoặc SslOnConnect).");
        }

        if (options.TimeoutSeconds <= 0)
        {
            failures.Add("Notifications:Smtp:TimeoutSeconds phải lớn hơn 0.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
