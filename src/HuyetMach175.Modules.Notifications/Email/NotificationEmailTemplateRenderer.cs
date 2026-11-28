using System.Text;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Notifications.Options;
using HuyetMach175.SharedKernel.Notifications;
using Microsoft.Extensions.Options;

namespace HuyetMach175.Modules.Notifications.Email;

public interface INotificationEmailTemplateRenderer
{
    EmailMessage Render(string recipientEmail, NotificationEmailPayload payload);
}

public sealed class NotificationEmailTemplateRenderer : INotificationEmailTemplateRenderer
{
    private readonly IOptionsMonitor<NotificationDeliveryOptions> _deliveryOptions;

    public NotificationEmailTemplateRenderer(IOptionsMonitor<NotificationDeliveryOptions> deliveryOptions)
    {
        _deliveryOptions = deliveryOptions;
    }

    public EmailMessage Render(string recipientEmail, NotificationEmailPayload payload)
    {
        var baseUrl = _deliveryOptions.CurrentValue.PublicWebBaseUrl;
        var ctaUrl = BuildSafeUrl(baseUrl, payload.ActionPath);
        var ctaLabel = string.IsNullOrWhiteSpace(payload.CtaLabel)
            ? "Mở trên Hệ thống Huyết Mạch 175"
            : payload.CtaLabel.Trim();

        var subject = string.IsNullOrWhiteSpace(payload.Subject)
            ? $"[Huyết Mạch 175] {payload.Title}"
            : payload.Subject.Trim();

        var htmlBody = payload.DigestSections is { Count: > 0 }
            ? RenderDigestHtml(payload, baseUrl, ctaUrl, ctaLabel)
            : RenderStandardHtml(payload, ctaUrl, ctaLabel);

        var textBody = RenderPlainText(payload, baseUrl, ctaUrl, ctaLabel);

        return new EmailMessage(
            To: recipientEmail.Trim(),
            Subject: subject,
            HtmlBody: htmlBody,
            TextBody: textBody);
    }

    private static string RenderStandardHtml(
        NotificationEmailPayload payload,
        string? ctaUrl,
        string ctaLabel)
    {
        var isCritical = payload.Priority == NotificationPriority.Critical;
        var accentColor = isCritical ? "#b91c1c" : "#991b1b";
        var categoryLabel = string.IsNullOrWhiteSpace(payload.CategoryLabel)
            ? (isCritical ? "Cảnh báo khẩn cấp" : "Thông báo nghiệp vụ")
            : payload.CategoryLabel.Trim();

        var greeting = string.IsNullOrWhiteSpace(payload.RecipientFullName)
            ? "Kính gửi Quý anh/chị,"
            : $"Kính gửi <strong>{Html(payload.RecipientFullName.Trim())}</strong>,";

        var sb = new StringBuilder(2048);
        AppendDocumentStart(sb, payload.Title, accentColor, categoryLabel, isCritical);

        sb.Append("<p style=\"margin:0 0 14px 0;font-size:15px;line-height:1.6;color:#1f2937;\">")
          .Append(greeting)
          .Append("</p>");

        sb.Append("<h2 style=\"margin:0 0 10px 0;font-size:18px;line-height:1.4;color:#111827;font-weight:700;\">")
          .Append(Html(payload.Title))
          .Append("</h2>");

        sb.Append("<p style=\"margin:0 0 18px 0;font-size:15px;line-height:1.65;color:#374151;\">")
          .Append(Html(payload.Content))
          .Append("</p>");

        if (payload.Metadata is { Count: > 0 })
        {
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;margin:0 0 18px 0;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden;\">");
            for (var i = 0; i < payload.Metadata.Count; i++)
            {
                var meta = payload.Metadata[i];
                var rowBg = i % 2 == 0 ? "#f9fafb" : "#ffffff";
                sb.Append("<tr style=\"background-color:").Append(rowBg).Append(";\">")
                  .Append("<td width=\"38%\" style=\"padding:10px 14px;border-bottom:1px solid #e5e7eb;font-size:13px;font-weight:600;color:#4b5563;vertical-align:top;\">")
                  .Append(Html(meta.Label))
                  .Append("</td>")
                  .Append("<td style=\"padding:10px 14px;border-bottom:1px solid #e5e7eb;font-size:14px;color:#111827;vertical-align:top;\">")
                  .Append(Html(meta.Value))
                  .Append("</td>")
                  .Append("</tr>");
            }
            sb.Append("</table>");
        }

        if (!string.IsNullOrWhiteSpace(payload.Notice))
        {
            var noticeBg = isCritical ? "#fef2f2" : "#fffbeb";
            var noticeBorder = isCritical ? "#dc2626" : "#d97706";
            var noticeText = isCritical ? "#991b1b" : "#92400e";

            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:0 0 20px 0;background-color:")
              .Append(noticeBg)
              .Append(";border-left:4px solid ")
              .Append(noticeBorder)
              .Append(";border-radius:4px;\"><tr><td style=\"padding:12px 14px;font-size:13px;line-height:1.55;color:")
              .Append(noticeText)
              .Append(";\"><strong>Lưu ý:</strong> ")
              .Append(Html(payload.Notice))
              .Append("</td></tr></table>");
        }

        if (!string.IsNullOrWhiteSpace(ctaUrl))
        {
            AppendCtaButton(sb, ctaUrl, ctaLabel, accentColor);
        }

        AppendDocumentEnd(sb, ctaUrl);
        return sb.ToString();
    }

    private static string RenderDigestHtml(
        NotificationEmailPayload payload,
        string baseUrl,
        string? mainCtaUrl,
        string mainCtaLabel)
    {
        const string accentColor = "#991b1b";
        var categoryLabel = string.IsNullOrWhiteSpace(payload.CategoryLabel)
            ? "Tổng hợp công việc trong ngày"
            : payload.CategoryLabel.Trim();

        var greeting = string.IsNullOrWhiteSpace(payload.RecipientFullName)
            ? "Kính gửi Quý anh/chị,"
            : $"Kính gửi <strong>{Html(payload.RecipientFullName.Trim())}</strong>,";

        var sb = new StringBuilder(3072);
        AppendDocumentStart(sb, payload.Title, accentColor, categoryLabel, isCritical: false);

        sb.Append("<p style=\"margin:0 0 14px 0;font-size:15px;line-height:1.6;color:#1f2937;\">")
          .Append(greeting)
          .Append("</p>");

        sb.Append("<h2 style=\"margin:0 0 10px 0;font-size:18px;line-height:1.4;color:#111827;font-weight:700;\">")
          .Append(Html(payload.Title))
          .Append("</h2>");

        sb.Append("<p style=\"margin:0 0 18px 0;font-size:15px;line-height:1.65;color:#374151;\">")
          .Append(Html(payload.Content))
          .Append("</p>");

        foreach (var section in payload.DigestSections!)
        {
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:0 0 16px 0;border:1px solid #e5e7eb;border-radius:8px;\">")
              .Append("<tr><td style=\"padding:12px 16px;background-color:#f9fafb;border-bottom:1px solid #e5e7eb;\">")
              .Append("<strong style=\"font-size:14px;color:#111827;\">")
              .Append(Html(section.Title))
              .Append("</strong>")
              .Append(" <span style=\"display:inline-block;margin-left:6px;padding:2px 8px;font-size:12px;font-weight:700;background-color:#fee2e2;color:#991b1b;border-radius:999px;\">")
              .Append(section.Count)
              .Append("</span></td></tr>")
              .Append("<tr><td style=\"padding:12px 16px;\"><ul style=\"margin:0;padding-left:18px;font-size:14px;line-height:1.6;color:#374151;\">");

            foreach (var bullet in section.Items.Take(10))
            {
                sb.Append("<li style=\"margin-bottom:4px;\">").Append(Html(bullet)).Append("</li>");
            }

            if (section.Items.Count > 10)
            {
                sb.Append("<li style=\"color:#6b7280;font-style:italic;\">... và ")
                  .Append(section.Items.Count - 10)
                  .Append(" mục khác</li>");
            }

            sb.Append("</ul>");

            var sectionUrl = BuildSafeUrl(baseUrl, section.ActionPath);
            if (!string.IsNullOrWhiteSpace(sectionUrl))
            {
                var sectionCta = string.IsNullOrWhiteSpace(section.CtaLabel) ? "Đi tới trang xử lý →" : section.CtaLabel.Trim();
                sb.Append("<p style=\"margin:10px 0 0 0;\"><a href=\"")
                  .Append(Html(sectionUrl))
                  .Append("\" style=\"font-size:13px;font-weight:600;color:#991b1b;text-decoration:none;\">")
                  .Append(Html(sectionCta))
                  .Append("</a></p>");
            }

            sb.Append("</td></tr></table>");
        }

        if (!string.IsNullOrWhiteSpace(mainCtaUrl))
        {
            AppendCtaButton(sb, mainCtaUrl, mainCtaLabel, accentColor);
        }

        AppendDocumentEnd(sb, mainCtaUrl);
        return sb.ToString();
    }

    private static void AppendDocumentStart(
        StringBuilder sb,
        string preheaderTitle,
        string accentColor,
        string categoryLabel,
        bool isCritical)
    {
        sb.Append("<!DOCTYPE html><html lang=\"vi\"><head><meta charset=\"UTF-8\" /><meta name=\"viewport\" content=\"width=device-width,initial-scale=1.0\" /><title>")
          .Append(Html(preheaderTitle))
          .Append("</title></head>")
          .Append("<body style=\"margin:0;padding:0;background-color:#f3f4f6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;\">")
          .Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background-color:#f3f4f6;padding:24px 12px;\">")
          .Append("<tr><td align=\"center\">")
          .Append("<table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:600px;width:100%;background-color:#ffffff;border-radius:10px;overflow:hidden;border:1px solid #e5e7eb;\">")
          .Append("<tr><td style=\"background-color:")
          .Append(accentColor)
          .Append(";padding:18px 24px;\">")
          .Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr>")
          .Append("<td style=\"color:#ffffff;font-size:17px;font-weight:700;letter-spacing:0.2px;\">Huyết Mạch 175 — Bệnh viện Quân y 175</td>")
          .Append("<td align=\"right\"><span style=\"display:inline-block;padding:4px 10px;border-radius:999px;background-color:")
          .Append(isCritical ? "#fef2f2" : "rgba(255,255,255,0.18)")
          .Append(";color:")
          .Append(isCritical ? "#991b1b" : "#ffffff")
          .Append(";font-size:12px;font-weight:700;\">")
          .Append(Html(categoryLabel))
          .Append("</span></td>")
          .Append("</tr></table></td></tr>")
          .Append("<tr><td style=\"padding:24px;\">");
    }

    private static void AppendCtaButton(StringBuilder sb, string ctaUrl, string ctaLabel, string accentColor)
    {
        sb.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:6px 0 18px 0;\">")
          .Append("<tr><td align=\"center\" style=\"border-radius:6px;background-color:")
          .Append(accentColor)
          .Append(";\">")
          .Append("<a href=\"")
          .Append(Html(ctaUrl))
          .Append("\" target=\"_blank\" rel=\"noopener noreferrer\" style=\"display:inline-block;padding:12px 22px;font-size:14px;font-weight:600;color:#ffffff;text-decoration:none;border-radius:6px;\">")
          .Append(Html(ctaLabel))
          .Append("</a></td></tr></table>");
    }

    private static void AppendDocumentEnd(StringBuilder sb, string? ctaUrl)
    {
        if (!string.IsNullOrWhiteSpace(ctaUrl))
        {
            sb.Append("<p style=\"margin:0 0 8px 0;font-size:12px;line-height:1.5;color:#6b7280;\">Nếu nút bấm không hoạt động, bạn có thể mở trực tiếp đường dẫn: <a href=\"")
              .Append(Html(ctaUrl))
              .Append("\" style=\"color:#991b1b;word-break:break-all;\">")
              .Append(Html(ctaUrl))
              .Append("</a></p>");
        }

        sb.Append("</td></tr>")
          .Append("<tr><td style=\"padding:16px 24px;background-color:#f9fafb;border-top:1px solid #e5e7eb;font-size:12px;line-height:1.55;color:#6b7280;\">")
          .Append("Email này được gửi tự động từ <strong>Hệ thống Quản lý Ngân hàng Máu Huyết Mạch 175</strong> (Bệnh viện Quân y 175). Vui lòng không trả lời trực tiếp thư này.")
          .Append("</td></tr></table></td></tr></table></body></html>");
    }

    private static string RenderPlainText(
        NotificationEmailPayload payload,
        string baseUrl,
        string? ctaUrl,
        string ctaLabel)
    {
        var sb = new StringBuilder(1024);
        sb.AppendLine("HUYẾT MẠCH 175 - BỆNH VIỆN QUÂN Y 175");
        sb.AppendLine(new string('=', 44));
        sb.AppendLine();

        sb.AppendLine(string.IsNullOrWhiteSpace(payload.RecipientFullName)
            ? "Kính gửi Quý anh/chị,"
            : $"Kính gửi {payload.RecipientFullName.Trim()},");
        sb.AppendLine();
        sb.AppendLine(payload.Title);
        sb.AppendLine(payload.Content);
        sb.AppendLine();

        if (payload.Metadata is { Count: > 0 })
        {
            foreach (var meta in payload.Metadata)
            {
                sb.Append("- ").Append(meta.Label).Append(": ").AppendLine(meta.Value);
            }
            sb.AppendLine();
        }

        if (payload.DigestSections is { Count: > 0 })
        {
            foreach (var section in payload.DigestSections)
            {
                sb.AppendLine($"* {section.Title} ({section.Count}):");
                foreach (var item in section.Items.Take(10))
                {
                    sb.Append("  - ").AppendLine(item);
                }
                var secUrl = BuildSafeUrl(baseUrl, section.ActionPath);
                if (!string.IsNullOrWhiteSpace(secUrl))
                {
                    sb.Append("  Xem tại: ").AppendLine(secUrl);
                }
                sb.AppendLine();
            }
        }

        if (!string.IsNullOrWhiteSpace(payload.Notice))
        {
            sb.Append("Lưu ý: ").AppendLine(payload.Notice);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(ctaUrl))
        {
            sb.Append(ctaLabel).Append(": ").AppendLine(ctaUrl);
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("Email tự động từ Hệ thống Huyết Mạch 175 - Bệnh viện Quân y 175.");
        return sb.ToString();
    }

    public static string? BuildSafeUrl(string? publicWebBaseUrl, string? actionPath)
    {
        if (string.IsNullOrWhiteSpace(actionPath))
        {
            return null;
        }

        var trimmedPath = actionPath.Trim();

        // Nếu là URL tuyệt đối, chỉ chấp nhận http/https để chặn javascript: injection
        if (Uri.TryCreate(trimmedPath, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.Scheme is "http" or "https"
                ? absoluteUri.ToString()
                : null;
        }

        if (string.IsNullOrWhiteSpace(publicWebBaseUrl))
        {
            return null;
        }

        var normalizedBase = publicWebBaseUrl.Trim().TrimEnd('/');
        if (!Uri.TryCreate(normalizedBase, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme is not ("http" or "https"))
        {
            return null;
        }

        var relative = trimmedPath.StartsWith('/') ? trimmedPath : "/" + trimmedPath;
        return normalizedBase + relative;
    }

    /// <summary>
    /// Escape 5 ký tự HTML nguy hiểm, giữ nguyên chuỗi UTF-8 tiếng Việt sạch sẽ.
    /// </summary>
    private static string Html(string? value)
        => string.IsNullOrEmpty(value)
            ? string.Empty
            : value
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal)
                .Replace("'", "&#39;", StringComparison.Ordinal);
}
