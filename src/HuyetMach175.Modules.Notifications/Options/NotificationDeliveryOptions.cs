namespace HuyetMach175.Modules.Notifications.Options;

public sealed class NotificationDeliveryOptions
{
    public const string SectionName = "Notifications:Delivery";

    public bool Enabled { get; set; } = true;
    public string PublicWebBaseUrl { get; set; } = "http://localhost:5173";
    public int PollIntervalSeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 25;
    public int MaxAttempts { get; set; } = 5;
    public int StaleProcessingMinutes { get; set; } = 10;
    public int MaxDrainIterations { get; set; } = 5;

    /// <summary>
    /// Các tên miền giả lập trong dữ liệu seed sẽ tự động chuyển trạng thái Skipped
    /// để tránh Gmail SMTP bị dội thư lỗi (bounce) khi gửi cho các tài khoản mẫu chưa đổi sang mail thật.
    /// </summary>
    public string[] SkippedEmailDomains { get; set; } =
    [
        "huyetmach175.vn",
        "example.com",
        "localhost",
        "invalid"
    ];

    public bool ShouldSkipDestination(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return true;
        }

        var atIndex = email.LastIndexOf('@');
        if (atIndex < 0 || atIndex == email.Length - 1)
        {
            return false;
        }

        var domain = email[(atIndex + 1)..].Trim();
        return SkippedEmailDomains.Any(skipped =>
            string.Equals(domain, skipped.Trim().TrimStart('@'), StringComparison.OrdinalIgnoreCase));
    }
}
